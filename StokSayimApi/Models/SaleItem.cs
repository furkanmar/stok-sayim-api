namespace StokSayimApi.Models;

public class SaleItem
{
    public int Id { get; set; }
    public int SaleId { get; set; }
    public int ProductId { get; set; }

    // Anlık snapshot — ürün adı/fiyat ilerleyen günlerde değişse fiş doğru kalır
    public string ProductName { get; set; } = string.Empty;
    public string Barcode { get; set; } = string.Empty;
    public string UnitType { get; set; } = "ADT";
    public decimal Quantity { get; set; }
    public decimal SatisFiyati { get; set; }    // KDV dahil birim fiyat
    public int KdvOrani { get; set; }
    public decimal LineTotal { get; set; }

    public Sale Sale { get; set; } = null!;
}
