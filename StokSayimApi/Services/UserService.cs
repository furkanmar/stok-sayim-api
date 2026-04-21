using Microsoft.EntityFrameworkCore;
using StokSayimApi.Data;
using StokSayimApi.Models;

namespace StokSayimApi.Services;

public class UserService
{
    private readonly AppDbContext _db;
    private readonly ILogger<UserService> _logger;

    public UserService(AppDbContext db, ILogger<UserService> logger)
    {
        _db = db;
        _logger = logger;
    }

    public async Task<List<User>> GetUsersByCompanyAsync(int companyId)
    {
        _logger.LogInformation("Fetching users for company: {CompanyId}", companyId);
        return await _db.Users
            .Include(u => u.UserBranches)
            .Where(u => u.CompanyId == companyId)
            .ToListAsync();
    }

    public async Task<User> CreateUserAsync(string username, string password, string role, int companyId, List<int> branchIds)
    {
        var user = new User
        {
            Username = username,
            PasswordHash = BCrypt.Net.BCrypt.HashPassword(password),
            Role = role,
            CompanyId = companyId,
            UserBranches = branchIds.Select(b => new UserBranch { BranchId = b }).ToList()
        };
        _db.Users.Add(user);
        await _db.SaveChangesAsync();
        _logger.LogInformation("User created: {Username}", username);
        return user;
    }

    public async Task<User?> UpdateUserAsync(int id, string username, string? password, string role, int companyId, List<int> branchIds)
    {
        var user = await _db.Users
            .Include(u => u.UserBranches)
            .FirstOrDefaultAsync(u => u.Id == id && u.CompanyId == companyId);

        if (user == null) return null;

        user.Username = username;
        user.Role = role;

        if (!string.IsNullOrEmpty(password))
            user.PasswordHash = BCrypt.Net.BCrypt.HashPassword(password);

        _db.UserBranches.RemoveRange(user.UserBranches);
        user.UserBranches = branchIds.Select(b => new UserBranch { BranchId = b, UserId = id }).ToList();

        await _db.SaveChangesAsync();
        _logger.LogInformation("User updated: {Username}", username);
        return user;
    }

    public async Task<bool> DeleteUserAsync(int id, int companyId)
    {
        var user = await _db.Users.FirstOrDefaultAsync(u => u.Id == id && u.CompanyId == companyId);
        if (user == null) return false;
        _db.Users.Remove(user);
        await _db.SaveChangesAsync();
        _logger.LogInformation("User deleted: {Id}", id);
        return true;
    }
}