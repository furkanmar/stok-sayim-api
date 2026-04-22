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
}
