using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Caching.Memory;
using StokSayimApi.Data;
using StokSayimApi.Models;
using StokSayimApi.DTOs;

namespace StokSayimApi.Services;

public class ProductService
{
    private readonly AppDbContext _db;
    private readonly ILogger<ProductService> _logger;
    private readonly IMemoryCache _cache;

    private static readonly TimeSpan _filterCacheTtl = TimeSpan.FromMinutes(10);

    public ProductService(AppDbContext db, ILogger<ProductService> logger, IMemoryCache cache)
    {
        _db = db;
        _logger = logger;
        _cache = cache;
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
            .FirstOrDefaultAsync(p => p.ProductId == productId && p.CompanyId == companyId);

        if (product == null) return null;

        // Her unit_type için tek en güncel fiyatı DB'den çek — tüm geçmişi belleğe alma
        var unitTypes = product.Barcodes.Select(b => b.UnitType).Distinct().ToList();

        var latestPrices = await _db.ProductPrices
            .Where(pp => pp.ProductId == productId)
            .GroupBy(pp => pp.UnitType)
            .Select(g => g.OrderByDescending(pp => pp.GecerlilikTarihi).First())
            .ToListAsync();

        var priceByUnit = latestPrices.ToDictionary(pp => pp.UnitType);

        // Barkodları unit_type'a göre grupla
        var barcodeGroups = product.Barcodes
            .GroupBy(b => b.UnitType)
            .OrderBy(g => g.Key)
            .Select(g =>
            {
                // Birime özgü fiyat yoksa ADT'ye fallback (eski SecMarket sync kayıtları)
                priceByUnit.TryGetValue(g.Key, out var price);
                price ??= priceByUnit.GetValueOrDefault("ADT");

                return new BarcodeGroupDto
                {
                    UnitType = g.Key,
                    UnitQuantity = g.First().UnitQuantity,
                    Barcodes = g.Select(b => b.Barcode).ToList(),
                    AlisFiyati = price?.AlisFiyati,
                    SatisFiyati = price?.SatisFiyati
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
                     && EF.Functions.ILike(p.ProductName, $"%{query}%"));

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
        var key = $"markas_{companyId}";
        if (_cache.TryGetValue(key, out List<string>? cached)) return cached!;

        var result = await _db.Products
            .Where(p => p.CompanyId == companyId && p.Marka != null && p.Marka != "")
            .Select(p => p.Marka!)
            .Distinct()
            .OrderBy(m => m)
            .ToListAsync();

        _cache.Set(key, result, _filterCacheTtl);
        return result;
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

    // ─── Barkod sil ───────────────────────────────────────────────────────────

    public async Task DeleteBarcodeAsync(int productId, string barcode, int companyId)
    {
        var pb = await _db.ProductBarcodes
            .FirstOrDefaultAsync(x =>
                x.Barcode    == barcode    &&
                x.ProductId  == productId  &&
                x.CompanyId  == companyId);

        if (pb == null)
            throw new KeyNotFoundException(
                $"Barkod '{barcode}' bu ürüne ({productId}) ait değil veya bulunamadı.");

        _db.ProductBarcodes.Remove(pb);
        await _db.SaveChangesAsync();
        _logger.LogInformation(
            "Barcode deleted: {Barcode} (UnitType={UnitType}) ← ProductId={ProductId}",
            barcode, pb.UnitType, productId);
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

    // ─── Toplu fiyat güncelleme (bulk upsert) ─────────────────────────────────

    public async Task<BulkPriceResultDto> BulkUpdatePricesAsync(
        List<BulkPriceItemDto> items, int companyId, int chunkSize = 500)
    {
        var result = new BulkPriceResultDto();

        foreach (var chunk in items.Chunk(chunkSize))
        {
            var productIds = chunk.Select(c => c.ProductId).Distinct().ToList();

            // Sahiplik kontrolü — sadece bu chunk'ın ID'leri için
            var validIds = await _db.Products
                .Where(p => p.CompanyId == companyId && productIds.Contains(p.ProductId))
                .Select(p => p.ProductId)
                .ToListAsync();

            var validSet = validIds.ToHashSet();

            var notFound = productIds.Where(id => !validSet.Contains(id)).ToList();
            result.NotFoundIds.AddRange(notFound);
            result.Skipped += notFound.Count;

            // Mevcut en güncel fiyatlar — sadece geçerli ID'ler için
            var existingLookup = (await _db.ProductPrices
                .Where(pp => validSet.Contains(pp.ProductId))
                .ToListAsync())
                .GroupBy(pp => (pp.ProductId, pp.UnitType))
                .ToDictionary(g => g.Key, g => g.OrderByDescending(pp => pp.GecerlilikTarihi).First());

            var toAdd = new List<ProductPrice>();

            foreach (var item in chunk.Where(i => validSet.Contains(i.ProductId)))
            {
                if (existingLookup.TryGetValue((item.ProductId, item.UnitType), out var current)
                    && Math.Abs(current.SatisFiyati - item.SatisFiyati) < 0.001)
                {
                    result.Skipped++;
                    continue;
                }

                toAdd.Add(new ProductPrice
                {
                    ProductId        = item.ProductId,
                    UnitType         = item.UnitType,
                    AlisFiyati       = 0,
                    SatisFiyati      = item.SatisFiyati,
                    GecerlilikTarihi = DateTime.UtcNow,
                    OlusturanUserId  = null,
                });
                result.Updated++;
            }

            if (toAdd.Count > 0)
            {
                await _db.ProductPrices.AddRangeAsync(toAdd);
                await _db.SaveChangesAsync();
            }
        }

        _logger.LogInformation(
            "BulkUpdatePrices — updated={Updated} skipped={Skipped} notFound={NotFound}",
            result.Updated, result.Skipped, result.NotFoundIds.Count);

        return result;
    }

    // ─── Ürün listesi (sayfalı, filtrelenebilir) ──────────────────────────────

    public async Task<ProductListResultDto> GetProductListAsync(
        string? q, string? marka, string? kategori, bool? hasPrice, int page, int pageSize, int companyId)
    {
        var query = _db.Products.Where(p => p.CompanyId == companyId);

        if (!string.IsNullOrWhiteSpace(q))
            query = query.Where(p => EF.Functions.ILike(p.ProductName, $"%{q}%"));
        if (!string.IsNullOrWhiteSpace(marka))
            query = query.Where(p => p.Marka == marka);
        if (!string.IsNullOrWhiteSpace(kategori))
            query = query.Where(p => p.Kategori == kategori);
        // Materialize et — EF Core 10'da nested IQueryable.Contains çevirisi güvenilir değil
        if (hasPrice != null)
        {
            var pricedIds = await _db.ProductPrices
                .Where(pp => pp.SatisFiyati > 0)
                .Select(pp => pp.ProductId)
                .Distinct()
                .ToListAsync();

            query = hasPrice == true
                ? query.Where(p => pricedIds.Contains(p.ProductId))
                : query.Where(p => !pricedIds.Contains(p.ProductId));
        }

        // page 1'de count al; sonraki sayfalarda Flutter zaten biliyor
        var total = page == 1 ? await query.CountAsync() : -1;
        var offset = (page - 1) * pageSize;

        var products = await query
            .AsNoTracking()
            .OrderBy(p => p.ProductName)
            .Skip(offset)
            .Take(pageSize)
            .Select(p => new
            {
                p.ProductId, p.ProductName, p.Marka, p.Kategori, p.AltKategori, p.Unit, p.KdvOrani,
                BarcodeCount = p.Barcodes.Count()
            })
            .ToListAsync();

        var productIds = products.Select(p => p.ProductId).ToList();

        // Tüm fiyatları çek — birime özgü fiyat yoksa ADT'ye fallback yapılacak
        var allPrices = await _db.ProductPrices
            .Where(pp => productIds.Contains(pp.ProductId))
            .OrderByDescending(pp => pp.GecerlilikTarihi)
            .ToListAsync();

        // Her ürün + birim tipi için en güncel fiyat haritası
        var priceMap = allPrices
            .GroupBy(pp => (pp.ProductId, pp.UnitType))
            .ToDictionary(g => g.Key, g => (double?)g.First().SatisFiyati);

        // Ürünün kendi Unit değerine göre fiyat al; yoksa ADT'ye düş
        // (products listesi zaten Unit içeriyor — ayrı sorgu gerekmiyor)
        var productUnits = products.ToDictionary(p => p.ProductId, p => p.Unit);

        var latestPrices = productIds.ToDictionary(
            id => id,
            id =>
            {
                var unit = productUnits.GetValueOrDefault(id, "ADT");
                return priceMap.GetValueOrDefault((id, unit))
                    ?? priceMap.GetValueOrDefault((id, "ADT"));
            });

        var items = products.Select(p => new ProductListItemDto
        {
            ProductId    = p.ProductId,
            ProductName  = p.ProductName,
            Marka        = p.Marka,
            Kategori     = p.Kategori,
            AltKategori  = p.AltKategori,
            Unit         = p.Unit,
            KdvOrani     = p.KdvOrani,
            BarcodeCount = p.BarcodeCount,
            SatisFiyati  = latestPrices.GetValueOrDefault(p.ProductId)
        }).ToList();

        return new ProductListResultDto
        {
            Items   = items,
            Total   = total,
            // total == -1 → page 1'den öğrenilmiş olmalı; bu sayfada pageSize kadar geldiyse devam var
            HasMore = total == -1
                ? items.Count == pageSize
                : offset + items.Count < total
        };
    }

    // ─── Kategori listesi ─────────────────────────────────────────────────────

    public async Task<List<string>> GetKategorilerAsync(int companyId)
    {
        var key = $"kategoriler_{companyId}";
        if (_cache.TryGetValue(key, out List<string>? cached)) return cached!;

        var result = await _db.Products
            .Where(p => p.CompanyId == companyId && p.Kategori != null && p.Kategori != "")
            .Select(p => p.Kategori!)
            .Distinct()
            .OrderBy(k => k)
            .ToListAsync();

        _cache.Set(key, result, _filterCacheTtl);
        return result;
    }

    // ─── POS Export — tüm barkodlar + güncel fiyatlar (tek sorgu) ────────────

    public async Task<PosExportResultDto> GetPosExportAsync(int companyId)
    {
        // Tüm barkodları + ürün bilgilerini tek JOIN ile çek
        var barcodes = await _db.ProductBarcodes
            .Where(pb => pb.CompanyId == companyId)
            .Include(pb => pb.Product)
            .ToListAsync();

        var productIds = barcodes.Select(pb => pb.ProductId).Distinct().ToList();

        // Her ürün + birim tipi için en güncel fiyatı çek
        var prices = await _db.ProductPrices
            .Where(pp => productIds.Contains(pp.ProductId))
            .OrderByDescending(pp => pp.GecerlilikTarihi)
            .ToListAsync();

        // productId + unitType → en güncel SatisFiyati
        var latestPriceMap = prices
            .GroupBy(pp => (pp.ProductId, pp.UnitType))
            .ToDictionary(g => g.Key, g => (double?)g.First().SatisFiyati);

        var entries = barcodes.Select(pb => new PosExportEntryDto
        {
            Barcode      = pb.Barcode,
            ProductId    = pb.ProductId,
            ProductName  = pb.Product.ProductName,
            UnitType     = pb.UnitType,
            UnitQuantity = pb.UnitQuantity,
            // Birime özgü fiyat yoksa ADT'ye fallback (eski sync kayıtları için)
            SatisFiyati  = latestPriceMap.GetValueOrDefault((pb.ProductId, pb.UnitType))
                        ?? latestPriceMap.GetValueOrDefault((pb.ProductId, "ADT")),
            KdvOrani     = pb.Product.KdvOrani,
        }).ToList();

        _logger.LogInformation("POS export: {Count} barkod, CompanyId={CompanyId}",
            entries.Count, companyId);

        return new PosExportResultDto
        {
            ExportedAt = DateTime.UtcNow,
            EntryCount = entries.Count,
            Entries    = entries,
        };
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
