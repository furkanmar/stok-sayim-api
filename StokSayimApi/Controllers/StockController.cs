using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using StokSayimApi.Data;
using StokSayimApi.DTOs;
using StokSayimApi.Services;
using System.Security.Claims;
using Microsoft.EntityFrameworkCore;

namespace StokSayimApi.Controllers;

[ApiController]
[Route("api/stock")]
[Authorize]
public class StockController : ControllerBase
{
    private readonly StockService _stockService;
    private readonly AppDbContext _db;
    private readonly ILogger<StockController> _logger;

    public StockController(StockService stockService, AppDbContext db, ILogger<StockController> logger)
    {
        _stockService = stockService;
        _db = db;
        _logger = logger;
    }

    private int GetUserId() => int.Parse(User.FindFirstValue(ClaimTypes.NameIdentifier)!);
    private int GetCompanyId() => int.Parse(User.FindFirstValue("companyId")!);

    [HttpGet("active/{branchId}")]
    public async Task<IActionResult> GetActiveCounts(int branchId)
    {
        var counts = await _stockService.GetActiveCountsAsync(branchId);
        return Ok(counts.Select(c => new StockCountDto
        {
            Id = c.Id,
            Name = c.Name,
            BranchId = c.BranchId,
            Status = c.Status,
            CreatedAt = c.CreatedAt
        }));
    }

    [HttpPost("create")]
    [Authorize(Roles = "admin,superadmin")]
    public async Task<IActionResult> CreateStockCount([FromBody] CreateStockCountDto dto)
    {
        var count = await _stockService.CreateStockCountAsync(dto.Name, dto.BranchId, GetUserId());
        return Ok(new StockCountDto
        {
            Id = count.Id,
            Name = count.Name,
            BranchId = count.BranchId,
            Status = count.Status,
            CreatedAt = count.CreatedAt
        });
    }

    [HttpPost("add-item")]
    public async Task<IActionResult> AddItem([FromBody] AddStockItemDto dto)
    {
        // Barkod artık ProductBarcodes tablosunda
        var pb = await _db.ProductBarcodes
            .FirstOrDefaultAsync(x => x.Barcode == dto.Barcode && x.CompanyId == GetCompanyId());

        if (pb == null) return NotFound(new { message = "Ürün bulunamadı." });

        var item = await _stockService.AddOrUpdateItemAsync(dto.StockCountId, pb.ProductId, dto.Stock);
        return Ok(item);
    }

    [HttpPost("complete/{stockCountId}")]
    [Authorize(Roles = "admin,superadmin")]
    public async Task<IActionResult> CompleteStockCount(int stockCountId)
    {
        await _stockService.CompleteStockCountAsync(stockCountId);
        return Ok();
    }
}