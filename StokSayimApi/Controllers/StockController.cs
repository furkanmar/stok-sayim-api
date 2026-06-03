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
        int productId;

        if (dto.ProductId.HasValue)
        {
            // Yeni yol: doğrudan productId
            var exists = await _db.Products
                .AnyAsync(p => p.ProductId == dto.ProductId.Value && p.CompanyId == GetCompanyId());
            if (!exists) return NotFound(new { message = "Ürün bulunamadı." });
            productId = dto.ProductId.Value;
        }
        else if (!string.IsNullOrWhiteSpace(dto.Barcode))
        {
            // Eski yol: barkod üzerinden ara (geriye dönük uyumluluk)
            var pb = await _db.ProductBarcodes
                .FirstOrDefaultAsync(x => x.Barcode == dto.Barcode && x.CompanyId == GetCompanyId());
            if (pb == null) return NotFound(new { message = "Ürün bulunamadı." });
            productId = pb.ProductId;
        }
        else
        {
            return BadRequest(new { message = "Barcode veya ProductId gereklidir." });
        }

        var item = await _stockService.AddOrUpdateItemAsync(dto.StockCountId, productId, dto.Stock);
        return Ok(item);
    }

    [HttpPost("complete/{stockCountId}")]
    [Authorize(Roles = "superadmin")]
    public async Task<IActionResult> CompleteStockCount(int stockCountId)
    {
        await _stockService.CompleteStockCountAsync(stockCountId);
        return Ok();
    }

    // ─── Kalem listesi + düzeltme (superadmin) ───────────────────────────────

    [HttpGet("{stockCountId}/items")]
    [Authorize(Roles = "superadmin")]
    public async Task<IActionResult> GetItems(int stockCountId)
    {
        // Sayımın bu şirkete ait olduğunu doğrula
        var count = await _db.StockCounts
            .Include(sc => sc.Branch)
            .FirstOrDefaultAsync(sc => sc.Id == stockCountId && sc.Branch.CompanyId == GetCompanyId());
        if (count == null) return NotFound();

        var items = await _stockService.GetItemsAsync(stockCountId);
        var result = items.Select(i => new StockCountItemDto
        {
            Id          = i.Id,
            ProductId   = i.ProductId,
            ProductName = i.Product.ProductName,
            Kategori    = i.Product.Kategori,
            Barkod      = i.Product.Barcodes
                            .FirstOrDefault(pb => pb.UnitType == "ADT")?.Barcode,
            Stock       = i.Stock,
            SatisFiyati = i.Product.Prices
                            .Where(p => p.UnitType == "ADT")
                            .OrderByDescending(p => p.GecerlilikTarihi)
                            .FirstOrDefault()?.SatisFiyati,
            UpdatedAt   = i.UpdatedAt
        });
        return Ok(result);
    }

    [HttpPut("items/{itemId}")]
    [Authorize(Roles = "superadmin")]
    public async Task<IActionResult> UpdateItem(int itemId, [FromBody] UpdateStockItemDto dto)
    {
        if (dto.Stock < 0) return BadRequest(new { message = "Stok negatif olamaz." });
        var item = await _stockService.UpdateItemAsync(itemId, dto.Stock);
        if (item == null) return NotFound();
        return Ok(new { item.Id, item.Stock, item.UpdatedAt });
    }

    [HttpDelete("items/{itemId}")]
    [Authorize(Roles = "superadmin")]
    public async Task<IActionResult> DeleteItem(int itemId)
    {
        var ok = await _stockService.DeleteItemAsync(itemId);
        if (!ok) return NotFound();
        return Ok();
    }
}