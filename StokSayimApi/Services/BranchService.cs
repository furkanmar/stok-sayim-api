using Microsoft.EntityFrameworkCore;
using StokSayimApi.Data;
using StokSayimApi.Models;

namespace StokSayimApi.Services;

public class BranchService
{
    private readonly AppDbContext _db;
    private readonly ILogger<BranchService> _logger;

    public BranchService(AppDbContext db, ILogger<BranchService> logger)
    {
        _db = db;
        _logger = logger;
    }

    public async Task<List<Branch>> GetBranchesByCompanyAsync(int companyId)
    {
        _logger.LogInformation("Fetching branches for company: {CompanyId}", companyId);
        return await _db.Branches
            .Where(b => b.CompanyId == companyId)
            .OrderBy(b => b.Name)
            .ToListAsync();
    }

    /// <summary>
    /// Belirli bir kullanıcıya atanmış şubeleri döndürür (user rolü için).
    /// </summary>
    public async Task<List<Branch>> GetBranchesByUserAsync(int userId, int companyId)
    {
        _logger.LogInformation("Fetching branches for user: {UserId}", userId);
        return await _db.UserBranches
            .Where(ub => ub.UserId == userId)
            .Include(ub => ub.Branch)
            .Where(ub => ub.Branch.CompanyId == companyId)
            .Select(ub => ub.Branch)
            .OrderBy(b => b.Name)
            .ToListAsync();
    }

    public async Task<Branch> CreateBranchAsync(string name, int companyId)
    {
        var branch = new Branch { Name = name, CompanyId = companyId };
        _db.Branches.Add(branch);
        await _db.SaveChangesAsync();
        _logger.LogInformation("Branch created: {Name}", name);
        return branch;
    }

    public async Task<Branch?> UpdateBranchAsync(int id, string name, int companyId)
    {
        var branch = await _db.Branches.FirstOrDefaultAsync(b => b.Id == id && b.CompanyId == companyId);
        if (branch == null) return null;
        branch.Name = name;
        await _db.SaveChangesAsync();
        _logger.LogInformation("Branch updated: {Name}", name);
        return branch;
    }

    public async Task<bool> DeleteBranchAsync(int id, int companyId)
    {
        var branch = await _db.Branches.FirstOrDefaultAsync(b => b.Id == id && b.CompanyId == companyId);
        if (branch == null) return false;
        _db.Branches.Remove(branch);
        await _db.SaveChangesAsync();
        _logger.LogInformation("Branch deleted: {Id}", id);
        return true;
    }
}