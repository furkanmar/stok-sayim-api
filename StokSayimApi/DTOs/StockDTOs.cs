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
    /// <summary>Barcode ile ara (eski yol — geriye dönük uyumluluk)</summary>
    public string? Barcode { get; set; }
    /// <summary>Doğrudan ProductId ile kaydet (yeni yol)</summary>
    public int? ProductId { get; set; }
    public double Stock { get; set; }
}

public class StockCountItemDto
{
    public int Id { get; set; }
    public int ProductId { get; set; }
    public string ProductName { get; set; } = string.Empty;
    public string? Kategori { get; set; }
    public string? Barkod { get; set; }
    public double Stock { get; set; }
    public double? SatisFiyati { get; set; }
    public DateTime UpdatedAt { get; set; }
}

public class UpdateStockItemDto
{
    public double Stock { get; set; }
}