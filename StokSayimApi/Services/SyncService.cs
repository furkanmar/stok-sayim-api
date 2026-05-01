using Microsoft.Data.Sqlite;
using Microsoft.EntityFrameworkCore;
using StokSayimApi.Data;
using StokSayimApi.DTOs;
using StokSayimApi.Models;

namespace StokSayimApi.Services;

public class SyncService
{
    private readonly AppDbContext _db;
    private readonly ILogger<SyncService> _logger;

    public SyncService(AppDbContext db, ILogger<SyncService> logger)
    {
        _db = db;
        _logger = logger;
    }

    /// <summary>
    /// final_secmarket.db dosyasından ürün, barkod ve fiyat verilerini PostgreSQL'e aktarır.
    /// Beklenen SQLite şeması:
    ///   products        (product_id, product_name, unit, content, kdv, marka, kategori, alt_kategori)
    ///   product_barcodes(product_id, barcode, unit_type, unit_quantity)
    ///   product_prices  (product_id, alis_fiyati, satis_fiyati)
    /// İdempotent: zaten varolan kayıtları atlar.
    /// </summary>
    public async Task<SyncResultDto> SyncFromSqliteAsync(string dbFilePath, int companyId)
    {
        if (!File.Exists(dbFilePath))
            throw new FileNotFoundException("SQLite dosyası bulunamadı.", dbFilePath);

        var sourceFile = Path.GetFileName(dbFilePath);
        _logger.LogInformation("Starting sync from {File} for companyId={CompanyId}", sourceFile, companyId);

        // ─── SQLite'tan verileri oku ─────────────────────────────────────────
        var products = ReadProductsFromSqlite(dbFilePath);
        var barcodes = ReadBarcodesFromSqlite(dbFilePath);
        var prices   = ReadPricesFromSqlite(dbFilePath);

        _logger.LogInformation(
            "SQLite read complete — {P} products, {B} barcodes, {Pr} prices",
            products.Count, barcodes.Count, prices.Count);

        // ─── Mevcut ProductId'leri önbelleğe al ─────────────────────────────
        var existingProductIds = await _db.Products
            .Where(p => p.CompanyId == companyId)
            .Select(p => p.ProductId)
            .ToHashSetAsync();

        // ─── Mevcut (Barcode, UnitType) çiftlerini önbelleğe al ─────────────
        var existingBarcodes = await _db.ProductBarcodes
            .Where(pb => pb.CompanyId == companyId)
            .Select(pb => new { pb.Barcode, pb.UnitType })
            .ToHashSetAsync();

        int productsAdded  = 0;
        int barcodesAdded  = 0;
        int barcodesSkipped = 0;

        // ─── Ürünleri ekle ──────────────────────────────────────────────────
        var newProducts = new List<Product>();
        foreach (var p in products)
        {
            if (existingProductIds.Contains(p.ProductId)) continue;

            newProducts.Add(new Product
            {
                ProductId   = p.ProductId,
                ProductName = p.ProductName,
                Unit        = p.Unit,
                Content     = p.Content,
                KdvOrani    = (int)(p.KdvOrani ?? 20),
                Kategori    = p.Kategori,
                AltKategori = p.AltKategori,
                Marka       = p.Marka,
                CompanyId   = companyId,
                CreatedAt   = DateTime.UtcNow,
                UpdatedAt   = DateTime.UtcNow
            });
        }

        if (newProducts.Count > 0)
        {
            await _db.Products.AddRangeAsync(newProducts);
            await _db.SaveChangesAsync();
            productsAdded = newProducts.Count;
            _logger.LogInformation("Added {Count} new products", productsAdded);
        }

        // ─── Barkodları toplu ekle ───────────────────────────────────────────
        var allProductIds = await _db.Products
            .Where(p => p.CompanyId == companyId)
            .Select(p => p.ProductId)
            .ToHashSetAsync();

        const int batchSize = 500;
        var newBarcodes = new List<ProductBarcode>();

        foreach (var b in barcodes)
        {
            if (!allProductIds.Contains(b.ProductId)) continue;

            var key = new { b.Barcode, b.UnitType };
            if (existingBarcodes.Contains(key))
            {
                barcodesSkipped++;
                continue;
            }

            newBarcodes.Add(new ProductBarcode
            {
                ProductId    = b.ProductId,
                Barcode      = b.Barcode,
                UnitType     = b.UnitType,
                UnitQuantity = b.UnitQuantity,
                CompanyId    = companyId,
                CreatedAt    = DateTime.UtcNow
            });

            existingBarcodes.Add(key);

            if (newBarcodes.Count >= batchSize)
            {
                await _db.ProductBarcodes.AddRangeAsync(newBarcodes);
                await _db.SaveChangesAsync();
                barcodesAdded += newBarcodes.Count;
                newBarcodes.Clear();
            }
        }

        if (newBarcodes.Count > 0)
        {
            await _db.ProductBarcodes.AddRangeAsync(newBarcodes);
            await _db.SaveChangesAsync();
            barcodesAdded += newBarcodes.Count;
        }

        // ─── Fiyatları ekle ─────────────────────────────────────────────────
        // Ürün birim tipi haritası — KG/GR/L/ML ürünlerde fiyat doğru birimde kaydedilsin
        var productUnitMap = products.ToDictionary(p => p.ProductId, p => p.Unit);

        // Zaten fiyatı olan (productId, unitType) çiftlerini atla (idempotent)
        var existingPriceSet = (await _db.ProductPrices
            .Where(pp => allProductIds.Contains(pp.ProductId))
            .Select(pp => new { pp.ProductId, pp.UnitType })
            .ToListAsync())
            .Select(x => (x.ProductId, x.UnitType))
            .ToHashSet();

        var newPrices = new List<ProductPrice>();
        foreach (var sp in prices)
        {
            if (!allProductIds.Contains(sp.ProductId)) continue;

            var unitType = productUnitMap.GetValueOrDefault(sp.ProductId, "ADT");
            if (existingPriceSet.Contains((sp.ProductId, unitType))) continue;

            newPrices.Add(new ProductPrice
            {
                ProductId        = sp.ProductId,
                UnitType         = unitType,
                AlisFiyati       = sp.AlisFiyati ?? 0,
                SatisFiyati      = sp.SatisFiyati ?? 0,
                GecerlilikTarihi = DateTime.UtcNow,
                OlusturanUserId  = null
            });
        }

        int pricesAdded = 0;
        const int priceBatch = 500;
        for (int i = 0; i < newPrices.Count; i += priceBatch)
        {
            var batch = newPrices.Skip(i).Take(priceBatch).ToList();
            await _db.ProductPrices.AddRangeAsync(batch);
            await _db.SaveChangesAsync();
            pricesAdded += batch.Count;
        }

        _logger.LogInformation(
            "Sync complete — products: {PA}, barcodes: {BA} (+{BS} skipped), prices: {PrA}",
            productsAdded, barcodesAdded, barcodesSkipped, pricesAdded);

        // ─── Sync kaydı oluştur ───────────────────────────────────────────────
        var log = new SecMarketSyncLog
        {
            SyncedAt        = DateTime.UtcNow,
            SourceFile      = sourceFile,
            ProductsAdded   = productsAdded,
            BarcodesAdded   = barcodesAdded,
            BarcodesSkipped = barcodesSkipped,
            PricesAdded     = pricesAdded,
            Status          = "success"
        };
        _db.SecMarketSyncLogs.Add(log);
        await _db.SaveChangesAsync();

        return new SyncResultDto
        {
            SyncedAt        = log.SyncedAt,
            SourceFile      = log.SourceFile,
            ProductsAdded   = productsAdded,
            BarcodesAdded   = barcodesAdded,
            BarcodesSkipped = barcodesSkipped,
            PricesAdded     = pricesAdded,
            Status          = "success"
        };
    }

    /// <summary>Sync geçmişini döndür (en yeni 20 kayıt)</summary>
    public async Task<List<SecMarketSyncLog>> GetSyncHistoryAsync()
    {
        return await _db.SecMarketSyncLogs
            .OrderByDescending(s => s.SyncedAt)
            .Take(20)
            .ToListAsync();
    }

    // ─── SQLite okuma: Ürünler ────────────────────────────────────────────────
    // Yeni şema: products tablosu — doğrudan tüm metadata burada
    private List<SqliteProduct> ReadProductsFromSqlite(string dbPath)
    {
        var results = new List<SqliteProduct>();

        using var conn = new SqliteConnection($"Data Source={dbPath};Mode=ReadOnly");
        conn.Open();

        const string sql = @"
            SELECT product_id, product_name, unit, content, kdv,
                   kategori, alt_kategori, marka
            FROM products";

        using var cmd    = new SqliteCommand(sql, conn);
        using var reader = cmd.ExecuteReader();

        while (reader.Read())
        {
            results.Add(new SqliteProduct
            {
                ProductId   = reader.GetInt32(0),
                ProductName = reader.IsDBNull(1) ? string.Empty : reader.GetString(1),
                Unit        = reader.IsDBNull(2) ? "ADT"         : reader.GetString(2),
                Content     = reader.IsDBNull(3) ? null          : reader.GetString(3),
                KdvOrani    = reader.IsDBNull(4) ? 20            : (double?)reader.GetDouble(4),
                Kategori    = reader.IsDBNull(5) ? null          : reader.GetString(5),
                AltKategori = reader.IsDBNull(6) ? null          : reader.GetString(6),
                Marka       = reader.IsDBNull(7) ? null          : reader.GetString(7)
            });
        }

        return results;
    }

    // ─── SQLite okuma: Barkodlar ──────────────────────────────────────────────
    // Yeni şema: product_barcodes — phase2_done JOIN'e gerek yok
    private List<SqliteBarcode> ReadBarcodesFromSqlite(string dbPath)
    {
        var results = new List<SqliteBarcode>();

        using var conn = new SqliteConnection($"Data Source={dbPath};Mode=ReadOnly");
        conn.Open();

        const string sql = @"
            SELECT DISTINCT product_id, barcode, unit_type, unit_quantity
            FROM product_barcodes
            WHERE barcode IS NOT NULL AND barcode != ''";

        using var cmd    = new SqliteCommand(sql, conn);
        using var reader = cmd.ExecuteReader();

        while (reader.Read())
        {
            results.Add(new SqliteBarcode
            {
                ProductId    = reader.GetInt32(0),
                Barcode      = reader.GetString(1),
                UnitType     = reader.IsDBNull(2) ? "ADT" : reader.GetString(2),
                UnitQuantity = reader.IsDBNull(3) ? null  : (double?)reader.GetDouble(3)
            });
        }

        return results;
    }

    // ─── SQLite okuma: Fiyatlar ───────────────────────────────────────────────
    // Yeni şema: product_prices — gerçek alış ve satış fiyatları
    private List<SqlitePrice> ReadPricesFromSqlite(string dbPath)
    {
        var results = new List<SqlitePrice>();

        using var conn = new SqliteConnection($"Data Source={dbPath};Mode=ReadOnly");
        conn.Open();

        const string sql = @"
            SELECT product_id, alis_fiyati, satis_fiyati
            FROM product_prices
            WHERE satis_fiyati IS NOT NULL AND satis_fiyati > 0";

        using var cmd    = new SqliteCommand(sql, conn);
        using var reader = cmd.ExecuteReader();

        while (reader.Read())
        {
            results.Add(new SqlitePrice
            {
                ProductId    = reader.GetInt32(0),
                AlisFiyati   = reader.IsDBNull(1) ? null : (double?)reader.GetDouble(1),
                SatisFiyati  = reader.IsDBNull(2) ? null : (double?)reader.GetDouble(2)
            });
        }

        return results;
    }

    // ─── Yardımcı iç sınıflar ────────────────────────────────────────────────

    private class SqliteProduct
    {
        public int     ProductId   { get; set; }
        public string  ProductName { get; set; } = string.Empty;
        public string  Unit        { get; set; } = "ADT";
        public string? Content     { get; set; }
        public double? KdvOrani    { get; set; }
        public string? Kategori    { get; set; }
        public string? AltKategori { get; set; }
        public string? Marka       { get; set; }
    }

    private class SqliteBarcode
    {
        public int     ProductId    { get; set; }
        public string  Barcode      { get; set; } = string.Empty;
        public string  UnitType     { get; set; } = "ADT";
        public double? UnitQuantity { get; set; }
    }

    private class SqlitePrice
    {
        public int     ProductId   { get; set; }
        public double? AlisFiyati  { get; set; }
        public double? SatisFiyati { get; set; }
    }
}
