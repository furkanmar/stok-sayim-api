namespace StokSayimApi.Models;

public class StockCountItem
{
    public int Id { get; set; }
    public int StockCountId { get; set; }
    public int ProductId { get; set; }
    public double Stock { get; set; }
    public DateTime CreatedAt { get; set; } = DateTime.UtcNow;
    public DateTime UpdatedAt { get; set; } = DateTime.UtcNow;

    public StockCount StockCount { get; set; } = null!;
    public Product Product { get; set; } = null!;
}