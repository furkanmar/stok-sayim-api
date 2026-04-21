using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using StokSayimApi.DTOs;
using StokSayimApi.Services;
using System.Security.Claims;

namespace StokSayimApi.Controllers;

[ApiController]
[Route("api/categories")]
[Authorize]
public class CategoryController : ControllerBase
{
    private readonly CategoryService _categoryService;
    private readonly ILogger<CategoryController> _logger;

    public CategoryController(CategoryService categoryService, ILogger<CategoryController> logger)
    {
        _categoryService = categoryService;
        _logger = logger;
    }

    private int GetCompanyId() => int.Parse(User.FindFirstValue("companyId")!);

    [HttpGet]
    public async Task<IActionResult> GetCategories()
    {
        var categories = await _categoryService.GetCategoriesAsync(GetCompanyId());
        return Ok(categories.Select(c => new CategoryDto
        {
            Id = c.Id,
            Name = c.Name,
            SubCategories = c.SubCategories.Select(sc => new SubCategoryDto
            {
                Id = sc.Id,
                Name = sc.Name,
                CategoryId = sc.CategoryId
            }).ToList()
        }));
    }

    [HttpPost]
    [Authorize(Roles = "superadmin")]
    public async Task<IActionResult> CreateCategory([FromBody] CreateCategoryDto dto)
    {
        var category = await _categoryService.CreateCategoryAsync(dto.Name, GetCompanyId());
        return Ok(new CategoryDto { Id = category.Id, Name = category.Name });
    }

    [HttpDelete("{id}")]
    [Authorize(Roles = "superadmin")]
    public async Task<IActionResult> DeleteCategory(int id)
    {
        var result = await _categoryService.DeleteCategoryAsync(id, GetCompanyId());
        if (!result) return NotFound();
        return NoContent();
    }

    [HttpPost("subcategory")]
    [Authorize(Roles = "superadmin")]
    public async Task<IActionResult> CreateSubCategory([FromBody] CreateSubCategoryDto dto)
    {
        var subCategory = await _categoryService.CreateSubCategoryAsync(dto.Name, dto.CategoryId);
        return Ok(new SubCategoryDto
        {
            Id = subCategory.Id,
            Name = subCategory.Name,
            CategoryId = subCategory.CategoryId
        });
    }

    [HttpDelete("subcategory/{id}")]
    [Authorize(Roles = "superadmin")]
    public async Task<IActionResult> DeleteSubCategory(int id)
    {
        var result = await _categoryService.DeleteSubCategoryAsync(id);
        if (!result) return NotFound();
        return NoContent();
    }
}