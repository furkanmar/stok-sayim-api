namespace StokSayimApi.Models;

public class ProductBarcode
{
    public int Id { get; set; }
    public int ProductId { get; set; }
    public string Barcode { get; set; } = string.Empty;
    public string UnitType { get; set; } = "ADT";
    public double? UnitQuantity { get; set; }
    public int CompanyId { get; set; }
    public DateTime CreatedAt { get; set; } = DateTime.UtcNow;

    public Product Product { get; set; } = null!;
}
