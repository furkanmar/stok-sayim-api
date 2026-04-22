using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using StokSayimApi.DTOs;
using StokSayimApi.Services;
using System.Security.Claims;

namespace StokSayimApi.Controllers;

[ApiController]
[Route("api/sales")]
[Authorize]
public class SaleController : ControllerBase
{
    private readonly SaleService _saleService;
    private readonly ILogger<SaleController> _logger;

    public SaleController(SaleService saleService, ILogger<SaleController> logger)
    {
        _saleService = saleService;
        _logger = logger;
    }

    private int GetUserId() => int.Parse(User.FindFirstValue(ClaimTypes.NameIdentifier)!);
    private int GetCompanyId() => int.Parse(User.FindFirstValue("companyId")!);

    // POST /api/sales — satış oluştur (tüm roller)
    [HttpPost]
    public async Task<IActionResult> CreateSale([FromBody] CreateSaleDto dto)
    {
        try
        {
            var sale = await _saleService.CreateSaleAsync(dto, GetUserId(), GetCompanyId());
            return Ok(MapToResponse(sale));
        }
        catch (InvalidOperationException ex)
        {
            return BadRequest(new { message = ex.Message });
        }
    }

    // GET /api/sales/{id} — tekil satış
    [HttpGet("{id:int}")]
    public async Task<IActionResult> GetById(int id)
    {
        var sale = await _saleService.GetByIdAsync(id, GetCompanyId());
        if (sale == null) return NotFound(new { message = "Satış bulunamadı." });
        return Ok(MapToResponse(sale));
    }

    // GET /api/sales/daily/{branchId}?date=2026-04-22 — günlük satışlar (tüm roller)
    [HttpGet("daily/{branchId:int}")]
    public async Task<IActionResult> GetDaily(int branchId, [FromQuery] string? date = null)
    {
        DateOnly? parsedDate = null;
        if (!string.IsNullOrWhiteSpace(date) && DateOnly.TryParse(date, out var d))
            parsedDate = d;

        var sales = await _saleService.GetDailyAsync(branchId, GetCompanyId(), parsedDate);
        return Ok(sales.Select(MapToResponse));
    }

    // POST /api/sales/{id}/refund — iade
    [HttpPost("{id:int}/refund")]
    public async Task<IActionResult> Refund(int id, [FromBody] RefundSaleDto dto)
    {
        dto.SaleId = id;
        try
        {
            var sale = await _saleService.RefundAsync(dto, GetCompanyId());
            return Ok(MapToResponse(sale));
        }
        catch (KeyNotFoundException ex)
        {
            return NotFound(new { message = ex.Message });
        }
        catch (InvalidOperationException ex)
        {
            return BadRequest(new { message = ex.Message });
        }
    }

    // ─── Mapper ──────────────────────────────────────────────────────────────

    private static SaleResponseDto MapToResponse(Models.Sale sale) => new()
    {
        Id = sale.Id,
        ReceiptNo = sale.ReceiptNo,
        BranchId = sale.BranchId,
        BranchName = sale.Branch?.Name ?? string.Empty,
        UserId = sale.UserId,
        Username = sale.User?.Username ?? string.Empty,
        CreatedAt = sale.CreatedAt,
        TotalAmount = sale.TotalAmount,
        DiscountAmount = sale.DiscountAmount,
        GrandTotal = sale.GrandTotal,
        IsRefunded = sale.IsRefunded,
        Items = sale.Items.Select(i => new SaleItemResponseDto
        {
            Id = i.Id,
            ProductId = i.ProductId,
            ProductName = i.ProductName,
            Barcode = i.Barcode,
            UnitType = i.UnitType,
            Quantity = i.Quantity,
            SatisFiyati = i.SatisFiyati,
            KdvOrani = i.KdvOrani,
            LineTotal = i.LineTotal,
        }).ToList(),
        Payments = sale.Payments.Select(p => new SalePaymentResponseDto
        {
            Type = p.PaymentType,
            Amount = p.Amount,
        }).ToList(),
    };
}
