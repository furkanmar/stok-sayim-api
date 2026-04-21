using Microsoft.EntityFrameworkCore;
using StokSayimApi.Data;
using StokSayimApi.Models;

namespace StokSayimApi.Services;

public class StockService
{
    private readonly AppDbContext _db;
    private readonly ILogger<StockService> _logger;

    public StockService(AppDbContext db, ILogger<StockService> logger)
    {
        _db = db;
        _logger = logger;
    }

    public async Task<List<StockCount>> GetActiveCountsAsync(int branchId)
    {
        _logger.LogInformation("Fetching active stock counts for branch: {BranchId}", branchId);
        return await _db.StockCounts
            .Where(sc => sc.BranchId == branchId && sc.Status == "active")
            .ToListAsync();
    }

    public async Task<StockCount> CreateStockCountAsync(string name, int branchId, int userId)
    {
        var stockCount = new StockCount
        {
            Name = name,
            BranchId = branchId,
            CreatedByUserId = userId,
            Status = "active"
        };
        _db.StockCounts.Add(stockCount);
        await _db.SaveChangesAsync();
        _logger.LogInformation("Stock count created: {Name}", name);
        return stockCount;
    }

    public async Task<StockCountItem> AddOrUpdateItemAsync(int stockCountId, int productId, double stock)
    {
        var existing = await _db.StockCountItems
            .FirstOrDefaultAsync(i => i.StockCountId == stockCountId && i.ProductId == productId);

        if (existing != null)
        {
            existing.Stock += stock;
            existing.UpdatedAt = DateTime.UtcNow;
            _logger.LogInformation("Stock count item updated — product: {ProductId}, new stock: {Stock}", productId, existing.Stock);
        }
        else
        {
            existing = new StockCountItem
            {
                StockCountId = stockCountId,
                ProductId = productId,
                Stock = stock
            };
            _db.StockCountItems.Add(existing);
            _logger.LogInformation("Stock count item added — product: {ProductId}", productId);
        }

        await _db.SaveChangesAsync();
        return existing;
    }

    public async Task CompleteStockCountAsync(int stockCountId)
    {
        var stockCount = await _db.StockCounts.FindAsync(stockCountId);
        if (stockCount == null) return;
        stockCount.Status = "completed";
        stockCount.CompletedAt = DateTime.UtcNow;
        await _db.SaveChangesAsync();
        _logger.LogInformation("Stock count completed: {Id}", stockCountId);
    }
}