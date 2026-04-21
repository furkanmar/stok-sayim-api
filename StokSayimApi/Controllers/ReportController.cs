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
    private readonly ILogger<ReportController> _logger;

    public ReportController(ReportService reportService, ILogger<ReportController> logger)
    {
        _reportService = reportService;
        _logger = logger;
    }

    private int GetCompanyId() => int.Parse(User.FindFirstValue("companyId")!);

    [HttpGet("branch/{branchId}")]
    public async Task<IActionResult> GetBranchReport(int branchId)
    {
        var report = await _reportService.GetBranchReportAsync(branchId, GetCompanyId());
        if (report == null) return NotFound();
        return Ok(report);
    }
}