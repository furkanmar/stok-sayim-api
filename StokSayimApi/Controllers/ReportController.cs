using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using StokSayimApi.Services;
using System.Security.Claims;

namespace StokSayimApi.Controllers;

[ApiController]
[Route("api/reports")]
[Authorize(Roles = "admin,superadmin")]
public class ReportController : ControllerBase
{
    private readonly ReportService _reportService;
    private readonly SaleService _saleService;
    private readonly ILogger<ReportController> _logger;

    public ReportController(ReportService reportService, SaleService saleService, ILogger<ReportController> logger)
    {
        _reportService = reportService;
        _saleService = saleService;
        _logger = logger;
    }

    private int GetCompanyId() => int.Parse(User.FindFirstValue("companyId")!);

    // GET /api/reports/branch/{branchId} — stok sayım raporu
    [HttpGet("branch/{branchId}")]
    public async Task<IActionResult> GetBranchReport(int branchId)
    {
        var report = await _reportService.GetBranchReportAsync(branchId, GetCompanyId());
        if (report == null) return NotFound();
        return Ok(report);
    }

    // GET /api/reports/zreport/{branchId}?date=2026-04-22 — Z raporu
    [HttpGet("zreport/{branchId}")]
    public async Task<IActionResult> GetZReport(int branchId, [FromQuery] string? date = null)
    {
        DateOnly? parsedDate = null;
        if (!string.IsNullOrWhiteSpace(date) && DateOnly.TryParse(date, out var d))
            parsedDate = d;

        var report = await _saleService.GetZReportAsync(branchId, GetCompanyId(), parsedDate);
        if (report == null) return NotFound(new { message = "Şube bulunamadı." });
        return Ok(report);
    }

    // GET /api/reports/analytics/trend?branchId=1&startDate=2026-04-01&endDate=2026-04-30
    [HttpGet("analytics/trend")]
    public async Task<IActionResult> GetSalesTrend(
        [FromQuery] int branchId,
        [FromQuery] string? startDate = null,
        [FromQuery] string? endDate = null)
    {
        var end = DateOnly.FromDateTime(DateTime.UtcNow);
        var start = end.AddDays(-29); // varsayılan: son 30 gün

        if (!string.IsNullOrWhiteSpace(startDate) && DateOnly.TryParse(startDate, out var s))
            start = s;
        if (!string.IsNullOrWhiteSpace(endDate) && DateOnly.TryParse(endDate, out var e))
            end = e;

        var data = await _saleService.GetSalesTrendAsync(branchId, GetCompanyId(), start, end);
        return Ok(data);
    }

    // GET /api/reports/analytics/bestsellers?branchId=1&startDate=&endDate=&limit=10
    [HttpGet("analytics/bestsellers")]
    public async Task<IActionResult> GetBestsellers(
        [FromQuery] int branchId,
        [FromQuery] string? startDate = null,
        [FromQuery] string? endDate = null,
        [FromQuery] int limit = 10)
    {
        var end = DateOnly.FromDateTime(DateTime.UtcNow);
        var start = end.AddDays(-29);

        if (!string.IsNullOrWhiteSpace(startDate) && DateOnly.TryParse(startDate, out var s))
            start = s;
        if (!string.IsNullOrWhiteSpace(endDate) && DateOnly.TryParse(endDate, out var e))
            end = e;

        limit = Math.Clamp(limit, 1, 50);
        var data = await _saleService.GetBestsellersAsync(branchId, GetCompanyId(), start, end, limit);
        return Ok(data);
    }

    // GET /api/reports/analytics/categories?branchId=1&startDate=&endDate=
    [HttpGet("analytics/categories")]
    public async Task<IActionResult> GetCategorySales(
        [FromQuery] int branchId,
        [FromQuery] string? startDate = null,
        [FromQuery] string? endDate = null)
    {
        var end = DateOnly.FromDateTime(DateTime.UtcNow);
        var start = end.AddDays(-29);

        if (!string.IsNullOrWhiteSpace(startDate) && DateOnly.TryParse(startDate, out var s))
            start = s;
        if (!string.IsNullOrWhiteSpace(endDate) && DateOnly.TryParse(endDate, out var e))
            end = e;

        var data = await _saleService.GetCategorySalesAsync(branchId, GetCompanyId(), start, end);
        return Ok(data);
    }
}
