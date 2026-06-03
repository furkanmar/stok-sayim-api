using Microsoft.EntityFrameworkCore;
using StokSayimApi.Data;

namespace StokSayimApi.Services;

public class ReportService
{
    private readonly AppDbContext _db;
    private readonly ILogger<ReportService> _logger;

    public ReportService(AppDbContext db, ILogger<ReportService> logger)
    {
        _db = db;
        _logger = logger;
    }

    public async Task<object> GetBranchReportAsync(int branchId, int companyId)
    {
        _logger.LogInformation("Generating report for branch: {BranchId}", branchId);

        var branch = await _db.Branches
            .FirstOrDefaultAsync(b => b.Id == branchId && b.CompanyId == companyId);

        if (branch == null) return null!;

        var stockCounts = await _db.StockCounts
            .Include(sc => sc.Items)
                .ThenInclude(i => i.Product)
                    .ThenInclude(p => p.Prices)  // Tüm fiyatları yükle — ADT filtresi memory'de
            .Where(sc => sc.BranchId == branchId)
            .OrderByDescending(sc => sc.CreatedAt)
            .ToListAsync();

        var result = stockCounts.Select(sc => new
        {
            id = sc.Id,
            name = sc.Name,
            status = sc.Status,
            createdAt = sc.CreatedAt,
            completedAt = sc.CompletedAt,
            categories = sc.Items
                .GroupBy(i => i.Product.Kategori ?? "Kategorisiz")
                .Select(g => new
                {
                    category = g.Key,
                    totalAlis = g.Sum(i => i.Stock * (i.Product.Prices
                        .Where(p => p.UnitType == "ADT")
                        .OrderByDescending(p => p.GecerlilikTarihi)
                        .FirstOrDefault()?.AlisFiyati ?? 0)),
                    totalSatis = g.Sum(i => i.Stock * (i.Product.Prices
                        .Where(p => p.UnitType == "ADT")
                        .OrderByDescending(p => p.GecerlilikTarihi)
                        .FirstOrDefault()?.SatisFiyati ?? 0)),
                    subCategories = g
                        .GroupBy(i => i.Product.AltKategori ?? "Kategorisiz")
                        .Select(sg => new
                        {
                            subCategory = sg.Key,
                            totalAlis = sg.Sum(i => i.Stock * (i.Product.Prices
                                .Where(p => p.UnitType == "ADT")
                                .OrderByDescending(p => p.GecerlilikTarihi)
                                .FirstOrDefault()?.AlisFiyati ?? 0)),
                            totalSatis = sg.Sum(i => i.Stock * (i.Product.Prices
                                .Where(p => p.UnitType == "ADT")
                                .OrderByDescending(p => p.GecerlilikTarihi)
                                .FirstOrDefault()?.SatisFiyati ?? 0))
                        }).ToList()
                }).ToList(),
            grandTotalAlis = sc.Items.Sum(i => i.Stock * (i.Product.Prices
                .Where(p => p.UnitType == "ADT")
                .OrderByDescending(p => p.GecerlilikTarihi)
                .FirstOrDefault()?.AlisFiyati ?? 0)),
            grandTotalSatis = sc.Items.Sum(i => i.Stock * (i.Product.Prices
                .Where(p => p.UnitType == "ADT")
                .OrderByDescending(p => p.GecerlilikTarihi)
                .FirstOrDefault()?.SatisFiyati ?? 0))
        }).ToList();

        _logger.LogInformation("Report generated for branch: {BranchId}", branchId);

        return new
        {
            branchId = branch.Id,
            branchName = branch.Name,
            stockCounts = result
        };
    }
}