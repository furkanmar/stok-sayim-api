namespace StokSayimApi.Models;

public class StockCount
{
    public int Id { get; set; }
    public int BranchId { get; set; }
    public int CreatedByUserId { get; set; }
    public string Status { get; set; } = "active";
    public string Name { get; set; } = string.Empty;
    public DateTime CreatedAt { get; set; } = DateTime.UtcNow;
    public DateTime? CompletedAt { get; set; }

    public Branch Branch { get; set; } = null!;
    public User CreatedByUser { get; set; } = null!;
    public ICollection<StockCountItem> Items { get; set; } = new List<StockCountItem>();
}