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

    // ─── İade ────────────────────────────────────────────────────────────────

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

        var activeSales = sales.Where(s => !s.IsRefunded).ToList();
        var refundedSales = sales.Where(s => s.IsRefunded).ToList();

        var grandTotal = activeSales.Sum(s => s.GrandTotal);
        var discountTotal = activeSales.Sum(s => s.DiscountAmount);
        var refundTotal = refundedSales.Sum(s => s.GrandTotal);

        var cashTotal = activeSales
            .SelectMany(s => s.Payments)
            .Where(p => p.PaymentType == "cash")
            .Sum(p => p.Amount);

        var cardTotal = activeSales
            .SelectMany(s => s.Payments)
            .Where(p => p.PaymentType == "card")
            .Sum(p => p.Amount);

        // KDV dökümü — her farklı oran için ayrı satır
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
