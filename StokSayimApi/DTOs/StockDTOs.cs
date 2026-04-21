namespace StokSayimApi.DTOs;

public class CreateStockCountDto
{
    public string Name { get; set; } = string.Empty;
    public int BranchId { get; set; }
}

public class StockCountDto
{
    public int Id { get; set; }
    public string Name { get; set; } = string.Empty;
    public int BranchId { get; set; }
    public string Status { get; set; } = string.Empty;
    public DateTime CreatedAt { get; set; }
}

public class AddStockItemDto
{
    public int StockCountId { get; set; }
    public string Barcode { get; set; } = string.Empty;
    public double Stock { get; set; }
}