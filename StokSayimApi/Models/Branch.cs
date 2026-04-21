namespace StokSayimApi.Models;

public class Branch
{
    public int Id { get; set; }
    public string Name { get; set; } = string.Empty;
    public int CompanyId { get; set; }

    public Company Company { get; set; } = null!;
    public ICollection<StockCount> StockCounts { get; set; } = new List<StockCount>();
}