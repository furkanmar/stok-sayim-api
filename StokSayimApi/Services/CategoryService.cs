using Microsoft.EntityFrameworkCore;
using StokSayimApi.Data;
using StokSayimApi.Models;

namespace StokSayimApi.Services;

public class CategoryService
{
    private readonly AppDbContext _db;
    private readonly ILogger<CategoryService> _logger;

    public CategoryService(AppDbContext db, ILogger<CategoryService> logger)
    {
        _db = db;
        _logger = logger;
    }

    public async Task<List<Category>> GetCategoriesAsync(int companyId)
    {
        _logger.LogInformation("Fetching categories for company: {CompanyId}", companyId);
        return await _db.Categories
            .Include(c => c.SubCategories)
            .Where(c => c.CompanyId == companyId)
            .ToListAsync();
    }

    public async Task<Category> CreateCategoryAsync(string name, int companyId)
    {
        var category = new Category { Name = name, CompanyId = companyId };
        _db.Categories.Add(category);
        await _db.SaveChangesAsync();
        _logger.LogInformation("Category created: {Name}", name);
        return category;
    }

    public async Task<bool> DeleteCategoryAsync(int id, int companyId)
    {
        var category = await _db.Categories.FirstOrDefaultAsync(c => c.Id == id && c.CompanyId == companyId);
        if (category == null) return false;
        _db.Categories.Remove(category);
        await _db.SaveChangesAsync();
        _logger.LogInformation("Category deleted: {Id}", id);
        return true;
    }

    public async Task<SubCategory> CreateSubCategoryAsync(string name, int categoryId)
    {
        var subCategory = new SubCategory { Name = name, CategoryId = categoryId };
        _db.SubCategories.Add(subCategory);
        await _db.SaveChangesAsync();
        _logger.LogInformation("SubCategory created: {Name}", name);
        return subCategory;
    }

    public async Task<bool> DeleteSubCategoryAsync(int id)
    {
        var subCategory = await _db.SubCategories.FindAsync(id);
        if (subCategory == null) return false;
        _db.SubCategories.Remove(subCategory);
        await _db.SaveChangesAsync();
        _logger.LogInformation("SubCategory deleted: {Id}", id);
        return true;
    }
}