using Microsoft.EntityFrameworkCore;
using StokSayimApi.Data;
using StokSayimApi.DTOs;
using StokSayimApi.Models;

namespace StokSayimApi.Services;

public class SaleService
{
    private readonly AppDbContext _db;
    private readonly ILogger<SaleService> _logger;

    public SaleService(AppDbContext db, ILogger<SaleService> logger)
    {
        _db = db;
        _logger = logger;
    }

    // ─── Satış oluştur ───────────────────────────────────────────────────────

    public async Task<Sale> CreateSaleAsync(CreateSaleDto dto, int userId, int companyId)
    {
        // Temel doğrulama
        if (dto.Items.Count == 0)
            throw new InvalidOperationException("Sepet boş olamaz.");

        if (dto.Payments.Sum(p => p.Amount) < dto.GrandTotal - 0.01m)
            throw new InvalidOperationException("Ödeme tutarı yetersiz.");

        var receiptNo = GenerateReceiptNo();

        var sale = new Sale
        {
            ReceiptNo = receiptNo,
            BranchId = dto.BranchId,
            UserId = userId,
            CompanyId = companyId,
            TotalAmount = dto.TotalAmount,
            DiscountAmount = dto.DiscountAmount,
            GrandTotal = dto.GrandTotal,
            CreatedAt = DateTime.UtcNow,
            Items = dto.Items.Select(i => new SaleItem
            {
                ProductId = i.ProductId,
                ProductName = i.ProductName,
                Barcode = i.Barcode,
                UnitType = i.UnitType,
                Quantity = i.Quantity,
                SatisFiyati = i.SatisFiyati,
                KdvOrani = i.KdvOrani,
                LineTotal = i.LineTotal,
            }).ToList(),
            Payments = dto.Payments.Select(p => new SalePayment
            {
                PaymentType = p.Type,
                Amount = p.Amount,
            }).ToList(),
        };

        _db.Sales.Add(sale);
        await _db.SaveChangesAsync();

        _logger.LogInformation("Sale created: {ReceiptNo}, branch: {BranchId}, total: {GrandTotal}",
            receiptNo, dto.BranchId, dto.GrandTotal);

        return await LoadSaleWithDetailsAsync(sale.Id);
    }

    // ─── Tekil satış getir ───────────────────────────────────────────────────

    public async Task<Sale?> GetByIdAsync(int id, int companyId)
    {
        var sale = await LoadSaleWithDetailsAsync(id);
        if (sale == null || sale.CompanyId != companyId) return null;
        return sale;
    }

    // ─── Günlük satışlar ─────────────────────────────────────────────────────

    public async Task<List<Sale>> GetDailyAsync(int branchId, int companyId, DateOnly? date = null)
    {
        var targetDate = date ?? DateOnly.FromDateTime(DateTime.UtcNow);
        var startUtc = targetDate.ToDateTime(TimeOnly.MinValue, DateTimeKind.Utc);
        var endUtc = startUtc.AddDays(1);

        return await _db.Sales
            .Include(s => s.Items)
            .Include(s => s.Payments)
            .Include(s => s.Branch)
            .Include(s => s.User)
            .Where(s => s.BranchId == branchId
                     && s.CompanyId == companyId
                     && s.CreatedAt >= startUtc
                     && s.CreatedAt < endUtc)
            .OrderByDescending(s => s.CreatedAt)
            .ToListAsync();
    }

    // ─── İade Modu (barkod taramalı geri alım) ──────────────────────────────

    public async Task<Sale> CreateReturnAsync(CreateReturnDto dto, int userId, int companyId)
    {
        if (dto.Items.Count == 0)
            throw new InvalidOperationException("İade listesi boş olamaz.");

        var receiptNo = "I" + DateTimeOffset.UtcNow.ToUnixTimeMilliseconds().ToString().Substring(5);

        var sale = new Sale
        {
            ReceiptNo = receiptNo,
            BranchId = dto.BranchId,
            UserId = userId,
            CompanyId = companyId,
            TotalAmount = dto.TotalAmount,
            DiscountAmount = 0,
            GrandTotal = dto.GrandTotal,
            IsReturn = true,
            CreatedAt = DateTime.UtcNow,
            Items = dto.Items.Select(i => new SaleItem
            {
                ProductId = i.ProductId,
                ProductName = i.ProductName,
                Barcode = i.Barcode,
                UnitType = i.UnitType,
                Quantity = i.Quantity,
                SatisFiyati = i.SatisFiyati,
                KdvOrani = i.KdvOrani,
                LineTotal = i.LineTotal,
            }).ToList(),
            Payments = new List<SalePayment>
            {
                new SalePayment { PaymentType = "cash", Amount = dto.GrandTotal },
            },
        };

        _db.Sales.Add(sale);
        await _db.SaveChangesAsync();

        _logger.LogInformation("Return created: {ReceiptNo}, branch: {BranchId}, total: {GrandTotal}",
            receiptNo, dto.BranchId, dto.GrandTotal);

        return await LoadSaleWithDetailsAsync(sale.Id);
    }

    // ─── İade (eski satış üzerinden işaretleme) ──────────────────────────────

    public async Task<Sale> RefundAsync(RefundSaleDto dto, int companyId)
    {
        var original = await LoadSaleWithDetailsAsync(dto.SaleId);

        if (original == null || original.CompanyId != companyId)
            throw new KeyNotFoundException("Satış bulunamadı.");

        if (original.IsRefunded)
            throw new InvalidOperationException("Bu satış zaten iade edilmiş.");

        // İade edilecek satır tutarını hesapla
        var refundTotal = 0m;
        foreach (var ri in dto.Items)
        {
            var item = original.Items.FirstOrDefault(i => i.Id == ri.SaleItemId)
                ?? throw new InvalidOperationException($"SaleItem {ri.SaleItemId} bu satışa ait değil.");

            if (ri.Quantity > item.Quantity)
                throw new InvalidOperationException($"İade miktarı satış miktarını aşamaz.");

            refundTotal += ri.Quantity * item.SatisFiyati;
        }

        // Basit tam iadeyi işaretleyelim (kısmi iade için ayrı tablo eklenebilir, şimdilik tüm satış)
        original.IsRefunded = true;
        await _db.SaveChangesAsync();

        _logger.LogInformation("Sale refunded: {SaleId}, refundTotal: {RefundTotal}", dto.SaleId, refundTotal);

        return original;
    }

    // ─── Z Raporu ────────────────────────────────────────────────────────────

    public async Task<ZReportDto?> GetZReportAsync(int branchId, int companyId, DateOnly? date = null)
    {
        var branch = await _db.Branches
            .FirstOrDefaultAsync(b => b.Id == branchId && b.CompanyId == companyId);

        if (branch == null) return null;

        var targetDate = date ?? DateOnly.FromDateTime(DateTime.UtcNow);
        var startUtc = targetDate.ToDateTime(TimeOnly.MinValue, DateTimeKind.Utc);
        var endUtc = startUtc.AddDays(1);

        var sales = await _db.Sales
            .Include(s => s.Items)
            .Include(s => s.Payments)
            .Where(s => s.BranchId == branchId
                     && s.CompanyId == companyId
                     && s.CreatedAt >= startUtc
                     && s.CreatedAt < endUtc)
            .ToListAsync();

        // Aktif satışlar: iade edilmemiş VE geri alım fişi olmayan
        var activeSales = sales.Where(s => !s.IsRefunded && !s.IsReturn).ToList();
        // İade edilmiş satışlar (refund işaretli)
        var refundedSales = sales.Where(s => s.IsRefunded && !s.IsReturn).ToList();
        // Barkod taramalı geri alım fişleri (IsReturn = true)
        var returnSales = sales.Where(s => s.IsReturn).ToList();

        var grandTotal = activeSales.Sum(s => s.GrandTotal);
        var discountTotal = activeSales.Sum(s => s.DiscountAmount);
        // Hem refund hem de geri alım fişlerinin toplamı
        var refundTotal = refundedSales.Sum(s => s.GrandTotal) + returnSales.Sum(s => s.GrandTotal);

        var cashTotal = activeSales
            .SelectMany(s => s.Payments)
            .Where(p => p.PaymentType == "cash")
            .Sum(p => p.Amount);

        var cardTotal = activeSales
            .SelectMany(s => s.Payments)
            .Where(p => p.PaymentType == "card")
            .Sum(p => p.Amount);

        // KDV dökümü — sadece aktif satışlar (geri alım ve refund hariç)
        var kdvLines = activeSales
            .SelectMany(s => s.Items)
            .GroupBy(i => i.KdvOrani)
            .Select(g =>
            {
                var grossTotal = g.Sum(i => i.LineTotal);
                var netTotal = grossTotal / (1 + g.Key / 100m);
                var kdvTotal = grossTotal - netTotal;
                return new KdvLineDto
                {
                    KdvOrani = g.Key,
                    GrossTotal = Math.Round(grossTotal, 2),
                    NetTotal = Math.Round(netTotal, 2),
                    KdvTotal = Math.Round(kdvTotal, 2),
                };
            })
            .OrderBy(k => k.KdvOrani)
            .ToList();

        return new ZReportDto
        {
            BranchId = branchId,
            BranchName = branch.Name,
            Date = targetDate.ToString("yyyy-MM-dd"),
            SaleCount = activeSales.Count,
            TotalAmount = Math.Round(activeSales.Sum(s => s.TotalAmount), 2),
            DiscountAmount = Math.Round(discountTotal, 2),
            GrandTotal = Math.Round(grandTotal, 2),
            CashTotal = Math.Round(cashTotal, 2),
            CardTotal = Math.Round(cardTotal, 2),
            RefundTotal = Math.Round(refundTotal, 2),
            NetRevenue = Math.Round(grandTotal - refundTotal, 2),
            KdvLines = kdvLines,
        };
    }

    // ─── Satış Trendi ────────────────────────────────────────────────────────────

    public async Task<List<SalesTrendItemDto>> GetSalesTrendAsync(
        int branchId, int companyId, DateOnly startDate, DateOnly endDate)
    {
        var startUtc = startDate.ToDateTime(TimeOnly.MinValue, DateTimeKind.Utc);
        var endUtc = endDate.ToDateTime(TimeOnly.MinValue, DateTimeKind.Utc).AddDays(1);

        var sales = await _db.Sales
            .Include(s => s.Payments)
            .Where(s => s.BranchId == branchId
                     && s.CompanyId == companyId
                     && s.CreatedAt >= startUtc
                     && s.CreatedAt < endUtc
                     && !s.IsRefunded
                     && !s.IsReturn)
            .ToListAsync();

        var grouped = sales
            .GroupBy(s => DateOnly.FromDateTime(s.CreatedAt))
            .ToDictionary(
                g => g.Key,
                g => new SalesTrendItemDto
                {
                    Date = g.Key.ToString("yyyy-MM-dd"),
                    SaleCount = g.Count(),
                    GrandTotal = Math.Round(g.Sum(s => s.GrandTotal), 2),
                    CashTotal = Math.Round(g.SelectMany(s => s.Payments)
                        .Where(p => p.PaymentType == "cash").Sum(p => p.Amount), 2),
                    CardTotal = Math.Round(g.SelectMany(s => s.Payments)
                        .Where(p => p.PaymentType == "card").Sum(p => p.Amount), 2),
                });

        // Aralıktaki tüm günleri doldur (satışsız günler sıfır)
        var result = new List<SalesTrendItemDto>();
        for (var d = startDate; d <= endDate; d = d.AddDays(1))
        {
            result.Add(grouped.TryGetValue(d, out var item) ? item : new SalesTrendItemDto
            {
                Date = d.ToString("yyyy-MM-dd"),
                SaleCount = 0,
                GrandTotal = 0,
                CashTotal = 0,
                CardTotal = 0,
            });
        }

        return result;
    }

    // ─── En Çok Satılanlar ───────────────────────────────────────────────────────

    public async Task<List<BestsellerItemDto>> GetBestsellersAsync(
        int branchId, int companyId, DateOnly startDate, DateOnly endDate, int limit = 10)
    {
        var startUtc = startDate.ToDateTime(TimeOnly.MinValue, DateTimeKind.Utc);
        var endUtc = endDate.ToDateTime(TimeOnly.MinValue, DateTimeKind.Utc).AddDays(1);

        var items = await _db.SaleItems
            .Where(i => i.Sale.BranchId == branchId
                     && i.Sale.CompanyId == companyId
                     && i.Sale.CreatedAt >= startUtc
                     && i.Sale.CreatedAt < endUtc
                     && !i.Sale.IsRefunded
                     && !i.Sale.IsReturn)
            .GroupBy(i => new { i.ProductId, i.ProductName })
            .Select(g => new BestsellerItemDto
            {
                ProductId = g.Key.ProductId,
                ProductName = g.Key.ProductName,
                Marka = string.Empty,
                Kategori = string.Empty,
                TotalQuantity = g.Sum(i => i.Quantity),
                TotalRevenue = Math.Round(g.Sum(i => i.LineTotal), 2),
            })
            .OrderByDescending(x => x.TotalRevenue)
            .Take(limit)
            .ToListAsync();

        var productIds = items.Select(i => i.ProductId).ToList();
        var products = await _db.Products
            .Where(p => productIds.Contains(p.ProductId) && p.CompanyId == companyId)
            .Select(p => new { p.ProductId, p.Marka, p.Kategori })
            .ToListAsync();

        foreach (var item in items)
        {
            var prod = products.FirstOrDefault(p => p.ProductId == item.ProductId);
            if (prod != null)
            {
                item.Marka = prod.Marka ?? string.Empty;
                item.Kategori = prod.Kategori ?? string.Empty;
            }
        }

        return items;
    }

    // ─── Kategori Bazlı Satış Dağılımı ──────────────────────────────────────────

    public async Task<List<CategorySalesItemDto>> GetCategorySalesAsync(
        int branchId, int companyId, DateOnly startDate, DateOnly endDate)
    {
        var startUtc = startDate.ToDateTime(TimeOnly.MinValue, DateTimeKind.Utc);
        var endUtc = endDate.ToDateTime(TimeOnly.MinValue, DateTimeKind.Utc).AddDays(1);

        var saleItems = await _db.SaleItems
            .Where(i => i.Sale.BranchId == branchId
                     && i.Sale.CompanyId == companyId
                     && i.Sale.CreatedAt >= startUtc
                     && i.Sale.CreatedAt < endUtc
                     && !i.Sale.IsRefunded
                     && !i.Sale.IsReturn)
            .Select(i => new { i.ProductId, i.LineTotal })
            .ToListAsync();

        var productIds = saleItems.Select(i => i.ProductId).Distinct().ToList();
        var productCategories = await _db.Products
            .Where(p => productIds.Contains(p.ProductId) && p.CompanyId == companyId)
            .Select(p => new { p.ProductId, Kategori = p.Kategori ?? "Diğer" })
            .ToListAsync();

        var catMap = productCategories.ToDictionary(p => p.ProductId, p => p.Kategori);

        var grouped = saleItems
            .GroupBy(i => catMap.TryGetValue(i.ProductId, out var k) ? k : "Diğer")
            .Select(g => new { Kategori = g.Key, Total = g.Sum(i => i.LineTotal) })
            .OrderByDescending(g => g.Total)
            .ToList();

        var totalRevenue = grouped.Sum(g => g.Total);

        return grouped.Select(g => new CategorySalesItemDto
        {
            Kategori = g.Kategori,
            TotalRevenue = Math.Round(g.Total, 2),
            Percentage = totalRevenue > 0 ? Math.Round(g.Total / totalRevenue * 100, 1) : 0,
        }).ToList();
    }

    // ─── Yardımcı ────────────────────────────────────────────────────────────

    private async Task<Sale> LoadSaleWithDetailsAsync(int saleId)
    {
        return await _db.Sales
            .Include(s => s.Items)
            .Include(s => s.Payments)
            .Include(s => s.Branch)
            .Include(s => s.User)
            .FirstOrDefaultAsync(s => s.Id == saleId)
            ?? throw new KeyNotFoundException($"Sale {saleId} bulunamadı.");
    }

    private static string GenerateReceiptNo()
        => "F" + DateTimeOffset.UtcNow.ToUnixTimeMilliseconds().ToString().Substring(5);
}
