namespace StokSayimApi.Models;

public class SalePayment
{
    public int Id { get; set; }
    public int SaleId { get; set; }
    public string PaymentType { get; set; } = "cash";  // "cash" | "card"
    public decimal Amount { get; set; }

    public Sale Sale { get; set; } = null!;
}
