using Microsoft.EntityFrameworkCore;
using StokSayimApi.Data;
using StokSayimApi.Models;
using StokSayimApi.DTOs;

namespace StokSayimApi.Services;

public class ProductService
{
    private readonly AppDbContext _db;
    private readonly ILogger<ProductService> _logger;

    public ProductService(AppDbContext db, ILogger<ProductService> logger)
    {
        _db = db;
        _logger = logger;
    }

    // ─── Barkod ile ürün getir ─────────────────────────────────────────────────

    public async Task<ProductDto?> GetByBarcodeAsync(string barcode, int companyId)
    {
        _logger.LogInformation("Fetching product by barcode: {Barcode}", barcode);

        var pb = await _db.ProductBarcodes
            .FirstOrDefaultAsync(x => x.Barcode == barcode && x.CompanyId == companyId);

        if (pb == null)
        {
            _logger.LogInformation("Barcode not found: {Barcode}", barcode);
            return null;
        }

        return await BuildProductDtoAsync(pb.ProductId, companyId);
    }

    // ─── ProductId ile ürün getir ──────────────────────────────────────────────

    public async Task<ProductDto?> GetByProductIdAsync(int productId, int companyId)
    {
        return await BuildProductDtoAsync(productId, companyId);
    }

    // ─── Ürün DTO'su oluştur (barkodlar + güncel fiyatlar dahil) ──────────────

    private async Task<ProductDto?> BuildProductDtoAsync(int productId, int companyId)
    {
        var product = await _db.Products
            .Include(p => p.Barcodes)
            .Include(p => p.Prices)
            .FirstOrDefaultAsync(p => p.ProductId == productId && p.CompanyId == companyId);

        if (product == null) return null;

        // Barkodları unit_type'a göre grupla
        var barcodeGroups = product.Barcodes
            .GroupBy(b => b.UnitType)
            .OrderBy(g => g.Key)
            .Select(g =>
            {
                // Her unit_type için en güncel fiyatı bul
                var latestPrice = product.Prices
                    .Where(pr => pr.UnitType == g.Key)
                    .OrderByDescending(pr => pr.GecerlilikTarihi)
                    .FirstOrDefault();

                return new BarcodeGroupDto
                {
                    UnitType = g.Key,
                    UnitQuantity = g.First().UnitQuantity,
                    Barcodes = g.Select(b => b.Barcode).ToList(),
                    AlisFiyati = latestPrice?.AlisFiyati,
                    SatisFiyati = latestPrice?.SatisFiyati
                };
            })
            .ToList();

        return new ProductDto
        {
            ProductId = product.ProductId,
            ProductName = product.ProductName,
            Unit = product.Unit,
            Content = product.Content,
            KdvOrani = product.KdvOrani,
            Kategori = product.Kategori,
            AltKategori = product.AltKategori,
            Marka = product.Marka,
            BarcodeGroups = barcodeGroups
        };
    }

    // ─── Ad ile ürün arama ────────────────────────────────────────────────────

    public async Task<List<SearchProductDto>> SearchByNameAsync(string? marka, string query, int companyId)
    {
        var q = _db.Products
            .Where(p => p.CompanyId == companyId
                     && p.ProductName.ToLower().Contains(query.ToLower()));

        if (!string.IsNullOrWhiteSpace(marka))
            q = q.Where(p => p.Marka == marka);

        return await q
            .OrderBy(p => p.ProductName)
            .Take(30)
            .Select(p => new SearchProductDto
            {
                ProductId = p.ProductId,
                ProductName = p.ProductName,
                Marka = p.Marka,
                Unit = p.Unit
            })
            .ToListAsync();
    }

    // ─── Marka listesi ────────────────────────────────────────────────────────

    public async Task<List<string>> GetMarkasAsync(int companyId)
    {
        return await _db.Products
            .Where(p => p.CompanyId == companyId && p.Marka != null && p.Marka != "")
            .Select(p => p.Marka!)
            .Distinct()
            .OrderBy(m => m)
            .ToListAsync();
    }

    // ─── Manuel ürün oluştur / güncelle ──────────────────────────────────────

    public async Task<Product> SaveProductAsync(int? productId, SaveProductDto dto, int companyId)
    {
        if (productId.HasValue)
        {
            // Güncelle
            var existing = await _db.Products
                .FirstOrDefaultAsync(p => p.ProductId == productId.Value && p.CompanyId == companyId)
                ?? throw new KeyNotFoundException($"Ürün bulunamadı: ProductId={productId}");

            existing.ProductName = dto.ProductName;
            existing.Unit = dto.Unit;
            existing.Content = dto.Content;
            existing.KdvOrani = dto.KdvOrani;
            existing.Kategori = dto.Kategori;
            existing.AltKategori = dto.AltKategori;
            existing.Marka = dto.Marka;
            existing.UpdatedAt = DateTime.UtcNow;

            await _db.SaveChangesAsync();
            _logger.LogInformation("Product updated: ProductId={ProductId}", productId);
            return existing;
        }
        else
        {
            // Yeni ürün — manuel ID ata (1xxxxx)
            var newId = await GetNextManualProductIdAsync();
            var product = new Product
            {
                ProductId = newId,
                ProductName = dto.ProductName,
                Unit = dto.Unit,
                Content = dto.Content,
                KdvOrani = dto.KdvOrani,
                Kategori = dto.Kategori,
                AltKategori = dto.AltKategori,
                Marka = dto.Marka,
                CompanyId = companyId,
                CreatedAt = DateTime.UtcNow,
                UpdatedAt = DateTime.UtcNow
            };

            _db.Products.Add(product);
            await _db.SaveChangesAsync();
            _logger.LogInformation("Product created manually: ProductId={ProductId}, Name={Name}", newId, dto.ProductName);
            return product;
        }
    }

    /// <summary>Manuel ürünler için sıradaki ID'yi üretir (100000–199999)</summary>
    private async Task<int> GetNextManualProductIdAsync()
    {
        var maxId = await _db.Products
            .Where(p => p.ProductId >= 100000 && p.ProductId <= 199999)
            .MaxAsync(p => (int?)p.ProductId) ?? 99999;

        var next = maxId + 1;
        if (next > 199999)
            throw new InvalidOperationException("Manuel ürün ID aralığı (100000–199999) doldu.");

        return next;
    }

    // ─── Ürüne barkod ekle ────────────────────────────────────────────────────

    public async Task<ProductBarcode> AddBarcodeAsync(
        int productId, string barcode, string unitType, double? unitQuantity, int companyId)
    {
        // Ürün bu şirkete ait mi?
        var productExists = await _db.Products
            .AnyAsync(p => p.ProductId == productId && p.CompanyId == companyId);

        if (!productExists)
            throw new KeyNotFoundException($"Ürün bulunamadı: ProductId={productId}");

        // Aynı (company, barcode, unitType) zaten var mı?
        var exists = await _db.ProductBarcodes
            .AnyAsync(pb => pb.CompanyId == companyId
                         && pb.Barcode == barcode
                         && pb.UnitType == unitType);

        if (exists)
            throw new InvalidOperationException(
                $"Barkod '{barcode}' zaten bu birim tipiyle ({unitType}) kayıtlı.");

        var pb = new ProductBarcode
        {
            ProductId = productId,
            Barcode = barcode,
            UnitType = unitType,
            UnitQuantity = unitQuantity,
            CompanyId = companyId,
            CreatedAt = DateTime.UtcNow
        };

        _db.ProductBarcodes.Add(pb);
        await _db.SaveChangesAsync();
        _logger.LogInformation("Barcode added: {Barcode} ({UnitType}) → ProductId={ProductId}",
            barcode, unitType, productId);
        return pb;
    }

    // ─── Fiyat kaydet (toplu, tüm unit_type'lar için) ─────────────────────────

    public async Task SavePricesAsync(SavePricesDto dto, int userId, int companyId)
    {
        var productExists = await _db.Products
            .AnyAsync(p => p.ProductId == dto.ProductId && p.CompanyId == companyId);

        if (!productExists)
            throw new KeyNotFoundException($"Ürün bulunamadı: ProductId={dto.ProductId}");

        foreach (var entry in dto.Prices)
        {
            _db.ProductPrices.Add(new ProductPrice
            {
                ProductId = dto.ProductId,
                UnitType = entry.UnitType,
                AlisFiyati = entry.AlisFiyati,
                SatisFiyati = entry.SatisFiyati,
                GecerlilikTarihi = DateTime.UtcNow,
                OlusturanUserId = userId
            });
        }

        await _db.SaveChangesAsync();
        _logger.LogInformation("Prices saved for ProductId={ProductId}, {Count} unit types",
            dto.ProductId, dto.Prices.Count);
    }

    // ─── Fiyat geçmişi ────────────────────────────────────────────────────────

    public async Task<List<ProductPrice>> GetPriceHistoryAsync(int productId)
    {
        return await _db.ProductPrices
            .Where(p => p.ProductId == productId)
            .OrderByDescending(p => p.GecerlilikTarihi)
            .ToListAsync();
    }
}
