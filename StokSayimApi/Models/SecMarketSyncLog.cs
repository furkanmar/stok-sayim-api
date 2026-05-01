namespace StokSayimApi.Models;

public class SecMarketSyncLog
{
    public int Id { get; set; }
    public DateTime SyncedAt { get; set; } = DateTime.UtcNow;
    public string SourceFile { get; set; } = string.Empty;
    public int ProductsAdded { get; set; }
    public int BarcodesAdded { get; set; }
    public int BarcodesSkipped { get; set; }
    public int PricesAdded { get; set; }

    /// <summary>success / partial / failed</summary>
    public string Status { get; set; } = "success";

    public string? Notes { get; set; }
}
