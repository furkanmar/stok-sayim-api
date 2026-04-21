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
    /// secmarket.db dosyasından ürün ve barkod verilerini PostgreSQL'e aktarır.
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

        _logger.LogInformation("SQLite read complete — {ProductCount} products, {BarcodeCount} barcodes",
            products.Count, barcodes.Count);

        // ─── Mevcut ProductId'leri önbelleğe al ─────────────────────────────
        var existingProductIds = await _db.Products
            .Where(p => p.CompanyId == companyId)
            .Select(p => p.ProductId)
            .ToHashSetAsync();

        // ─── Mevcut (CompanyId, Barcode, UnitType) üçlülerini önbelleğe al ──
        var existingBarcodes = await _db.ProductBarcodes
            .Where(pb => pb.CompanyId == companyId)
            .Select(pb => new { pb.Barcode, pb.UnitType })
            .ToHashSetAsync();

        int productsAdded = 0;
        int barcodesAdded = 0;
        int barcodesSkipped = 0;

        // ─── Ürünleri ekle ──────────────────────────────────────────────────
        var newProducts = new List<Product>();
        foreach (var p in products)
        {
            if (existingProductIds.Contains(p.ProductId)) continue;

            newProducts.Add(new Product
            {
                ProductId = p.ProductId,
                ProductName = p.ProductName,
                Unit = p.Unit,
                Content = p.Content,
                KdvOrani = (int)(p.KdvOrani ?? 20),
                Kategori = p.Kategori,
                AltKategori = p.AltKategori,
                Marka = p.Marka,
                CompanyId = companyId,
                CreatedAt = DateTime.UtcNow,
                UpdatedAt = DateTime.UtcNow
            });
        }

        if (newProducts.Count > 0)
        {
            await _db.Products.AddRangeAsync(newProducts);
            await _db.SaveChangesAsync();
            productsAdded = newProducts.Count;
            _logger.LogInformation("Added {Count} new products", productsAdded);
        }

        // ─── Barkodları toplu ekle (batch) ───────────────────────────────────
        // Tüm productId'leri yeniden çek (yeni eklenenleri de kapsasın)
        var allProductIds = await _db.Products
            .Where(p => p.CompanyId == companyId)
            .Select(p => p.ProductId)
            .ToHashSetAsync();

        const int batchSize = 500;
        var newBarcodes = new List<ProductBarcode>();

        foreach (var b in barcodes)
        {
            // Ürün bu şirkette yoksa atla (daha önce sync edilmemiş ürünler)
            if (!allProductIds.Contains(b.ProductId)) continue;

            var key = new { b.Barcode, b.UnitType };
            if (existingBarcodes.Contains(key))
            {
                barcodesSkipped++;
                continue;
            }

            newBarcodes.Add(new ProductBarcode
            {
                ProductId = b.ProductId,
                Barcode = b.Barcode,
                UnitType = b.UnitType,
                UnitQuantity = b.UnitQuantity,
                CompanyId = companyId,
                CreatedAt = DateTime.UtcNow
            });

            existingBarcodes.Add(key); // çift eklemeyi önle

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

        _logger.LogInformation(
            "Sync complete — products added: {PA}, barcodes added: {BA}, barcodes skipped: {BS}",
            productsAdded, barcodesAdded, barcodesSkipped);

        // ─── Sync kaydı oluştur ───────────────────────────────────────────────
        var log = new SecMarketSyncLog
        {
            SyncedAt = DateTime.UtcNow,
            SourceFile = sourceFile,
            ProductsAdded = productsAdded,
            BarcodesAdded = barcodesAdded,
            BarcodesSkipped = barcodesSkipped,
            Status = "success"
        };
        _db.SecMarketSyncLogs.Add(log);
        await _db.SaveChangesAsync();

        return new SyncResultDto
        {
            SyncedAt = log.SyncedAt,
            SourceFile = log.SourceFile,
            ProductsAdded = productsAdded,
            BarcodesAdded = barcodesAdded,
            BarcodesSkipped = barcodesSkipped,
            Status = "success"
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

    private List<SqliteProduct> ReadProductsFromSqlite(string dbPath)
    {
        var results = new List<SqliteProduct>();
        var seen = new HashSet<int>();

        using var conn = new SqliteConnection($"Data Source={dbPath};Mode=ReadOnly");
        conn.Open();

        // csv_products ile barcode_lookup'ı join ederek kategorik verileri al
        var sql = @"
            SELECT
                bl.product_id,
                bl.product_name,
                cp.unit,
                cp.content,
                cp.kdv,
                cp.kategori,
                cp.alt_kategori,
                cp.marka
            FROM barcode_lookup bl
            LEFT JOIN csv_products cp ON cp.barcode = bl.source_barcode
            WHERE bl.status = 'found'
            GROUP BY bl.product_id";

        using var cmd = new SqliteCommand(sql, conn);
        using var reader = cmd.ExecuteReader();

        while (reader.Read())
        {
            var productId = reader.GetInt32(0);
            if (seen.Contains(productId)) continue;
            seen.Add(productId);

            results.Add(new SqliteProduct
            {
                ProductId = productId,
                ProductName = reader.IsDBNull(1) ? string.Empty : reader.GetString(1),
                Unit = reader.IsDBNull(2) ? "ADT" : reader.GetString(2),
                Content = reader.IsDBNull(3) ? null : reader.GetString(3),
                KdvOrani = reader.IsDBNull(4) ? 20 : (double?)reader.GetDouble(4),
                Kategori = reader.IsDBNull(5) ? null : reader.GetString(5),
                AltKategori = reader.IsDBNull(6) ? null : reader.GetString(6),
                Marka = reader.IsDBNull(7) ? null : reader.GetString(7)
            });
        }

        return results;
    }

    // ─── SQLite okuma: Barkodlar ──────────────────────────────────────────────

    private List<SqliteBarcode> ReadBarcodesFromSqlite(string dbPath)
    {
        var results = new List<SqliteBarcode>();

        using var conn = new SqliteConnection($"Data Source={dbPath};Mode=ReadOnly");
        conn.Open();

        // DISTINCT (product_id, barcode, unit_type) — barcode_id tekrarlarını ele
        var sql = @"
            SELECT DISTINCT
                pb.product_id,
                pb.barcode,
                pb.unit_type,
                pb.unit_quantity
            FROM product_barcodes pb
            INNER JOIN phase2_done pd ON pd.product_id = pb.product_id
            WHERE pb.barcode IS NOT NULL AND pb.barcode != ''";

        using var cmd = new SqliteCommand(sql, conn);
        using var reader = cmd.ExecuteReader();

        while (reader.Read())
        {
            results.Add(new SqliteBarcode
            {
                ProductId = reader.GetInt32(0),
                Barcode = reader.GetString(1),
                UnitType = reader.IsDBNull(2) ? "ADT" : reader.GetString(2),
                UnitQuantity = reader.IsDBNull(3) ? null : (double?)reader.GetDouble(3)
            });
        }

        return results;
    }

    // ─── Yardımcı iç sınıflar ─────────────────────────────────────────────────

    private class SqliteProduct
    {
        public int ProductId { get; set; }
        public string ProductName { get; set; } = string.Empty;
        public string Unit { get; set; } = "ADT";
        public string? Content { get; set; }
        public double? KdvOrani { get; set; }
        public string? Kategori { get; set; }
        public string? AltKategori { get; set; }
        public string? Marka { get; set; }
    }

    private class SqliteBarcode
    {
        public int ProductId { get; set; }
        public string Barcode { get; set; } = string.Empty;
        public string UnitType { get; set; } = "ADT";
        public double? UnitQuantity { get; set; }
    }
}
