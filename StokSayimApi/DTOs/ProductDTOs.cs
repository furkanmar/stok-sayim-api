namespace StokSayimApi.DTOs;

/// <summary>Tek bir unit_type'a ait barkodlar + güncel fiyat</summary>
public class BarcodeGroupDto
{
    public string UnitType { get; set; } = string.Empty;
    public double? UnitQuantity { get; set; }
    public List<string> Barcodes { get; set; } = new();
    public double? AlisFiyati { get; set; }
    public double? SatisFiyati { get; set; }
}

/// <summary>Barkod aramasının tam cevabı — barkod grupları dahil</summary>
public class ProductDto
{
    public int ProductId { get; set; }
    public string ProductName { get; set; } = string.Empty;
    public string Unit { get; set; } = string.Empty;
    public string? Content { get; set; }
    public int KdvOrani { get; set; }
    public string? Kategori { get; set; }
    public string? AltKategori { get; set; }
    public string? Marka { get; set; }
    public List<BarcodeGroupDto> BarcodeGroups { get; set; } = new();
}

/// <summary>Arama sonuçlarında dönen sade ürün bilgisi</summary>
public class SearchProductDto
{
    public int ProductId { get; set; }
    public string ProductName { get; set; } = string.Empty;
    public string? Marka { get; set; }
    public string Unit { get; set; } = string.Empty;
}

/// <summary>Manuel ürün oluşturma / güncelleme</summary>
public class SaveProductDto
{
    public string ProductName { get; set; } = string.Empty;
    public string Unit { get; set; } = "ADT";
    public string? Content { get; set; }
    public int KdvOrani { get; set; } = 20;
    public string? Kategori { get; set; }
    public string? AltKategori { get; set; }
    public string? Marka { get; set; }
}

/// <summary>Ürüne barkod ekleme (unit_type zorunlu)</summary>
public class AddBarcodeDto
{
    public string Barcode { get; set; } = string.Empty;
    public string UnitType { get; set; } = "ADT";
    public double? UnitQuantity { get; set; }
}

/// <summary>Tek bir unit_type için fiyat girişi</summary>
public class PriceEntryDto
{
    public string UnitType { get; set; } = string.Empty;
    public double AlisFiyati { get; set; }
    public double SatisFiyati { get; set; }
}

/// <summary>Bir ürünün tüm unit_type'ları için toplu fiyat kaydetme</summary>
public class SavePricesDto
{
    public int ProductId { get; set; }
    public List<PriceEntryDto> Prices { get; set; } = new();
}

/// <summary>Fiyat geçmişi listesi için</summary>
public class ProductPriceDto
{
    public int Id { get; set; }
    public string UnitType { get; set; } = string.Empty;
    public double AlisFiyati { get; set; }
    public double SatisFiyati { get; set; }
    public DateTime GecerlilikTarihi { get; set; }
}

/// <summary>Ürün listesi tek satır</summary>
public class ProductListItemDto
{
    public int ProductId { get; set; }
    public string ProductName { get; set; } = string.Empty;
    public string? Marka { get; set; }
    public string? Kategori { get; set; }
    public string? AltKategori { get; set; }
    public string Unit { get; set; } = string.Empty;
    public int KdvOrani { get; set; }
    public int BarcodeCount { get; set; }
    /// <summary>En güncel ADT satış fiyatı (varsa)</summary>
    public double? SatisFiyati { get; set; }
}

/// <summary>Sayfalı ürün listesi cevabı</summary>
public class ProductListResultDto
{
    public List<ProductListItemDto> Items { get; set; } = new();
    public int Total { get; set; }
    public bool HasMore { get; set; }
}

/// <summary>Sync sonuç özeti</summary>
public class SyncResultDto
{
    public DateTime SyncedAt { get; set; }
    public string SourceFile { get; set; } = string.Empty;
    public int ProductsAdded { get; set; }
    public int BarcodesAdded { get; set; }
    public int BarcodesSkipped { get; set; }
    public string Status { get; set; } = "success";
    public string? Notes { get; set; }
}
