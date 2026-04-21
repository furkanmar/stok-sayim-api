using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;

namespace StokSayimApi.Models;

public class Product
{
    /// <summary>
    /// Seç Market ürünleri: 2xxxxxx (7 hane, 2 ile başlar)
    /// Manuel girilen ürünler: 1xxxxx (6 hane, 1 ile başlar, 100000'den başlar)
    /// </summary>
    [Key]
    [DatabaseGenerated(DatabaseGeneratedOption.None)]
    public int ProductId { get; set; }

    public string ProductName { get; set; } = string.Empty;
    public string Unit { get; set; } = string.Empty;
    public string? Content { get; set; }
    public int KdvOrani { get; set; } = 20;
    public string? Kategori { get; set; }
    public string? AltKategori { get; set; }
    public string? Marka { get; set; }
    public int CompanyId { get; set; }
    public DateTime CreatedAt { get; set; } = DateTime.UtcNow;
    public DateTime UpdatedAt { get; set; } = DateTime.UtcNow;

    public Company Company { get; set; } = null!;
    public ICollection<StockCountItem> StockCountItems { get; set; } = new List<StockCountItem>();
    public ICollection<ProductPrice> Prices { get; set; } = new List<ProductPrice>();
    public ICollection<ProductBarcode> Barcodes { get; set; } = new List<ProductBarcode>();
}
