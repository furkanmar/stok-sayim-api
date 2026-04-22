namespace StokSayimApi.Models;

public class Sale
{
    public int Id { get; set; }
    public string ReceiptNo { get; set; } = string.Empty;   // Fiş numarası (F + timestamp)
    public int BranchId { get; set; }
    public int UserId { get; set; }
    public int CompanyId { get; set; }
    public decimal TotalAmount { get; set; }                 // İskonto öncesi toplam
    public decimal DiscountAmount { get; set; }              // İskonto tutarı
    public decimal GrandTotal { get; set; }                  // Ödenen tutar
    public bool IsRefunded { get; set; } = false;
    public bool IsReturn { get; set; } = false;           // true → iade/geri alım fişi
    public DateTime CreatedAt { get; set; } = DateTime.UtcNow;

    public Branch Branch { get; set; } = null!;
    public User User { get; set; } = null!;
    public ICollection<SaleItem> Items { get; set; } = new List<SaleItem>();
    public ICollection<SalePayment> Payments { get; set; } = new List<SalePayment>();
}
