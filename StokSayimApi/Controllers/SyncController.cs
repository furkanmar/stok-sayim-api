using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using StokSayimApi.Services;
using System.Security.Claims;

namespace StokSayimApi.Controllers;

[ApiController]
[Route("api/sync")]
[Authorize(Roles = "admin,superadmin")]
public class SyncController : ControllerBase
{
    private readonly SyncService _syncService;
    private readonly ILogger<SyncController> _logger;
    private readonly IWebHostEnvironment _env;

    public SyncController(SyncService syncService, ILogger<SyncController> logger, IWebHostEnvironment env)
    {
        _syncService = syncService;
        _logger = logger;
        _env = env;
    }

    private int GetCompanyId() => int.Parse(User.FindFirstValue("companyId")!);

    /// <summary>
    /// secmarket.db dosyasını yükle ve senkronizasyonu başlat.
    /// Multipart form-data: dosya "file" alanında gönderilmeli.
    /// </summary>
    [HttpPost("secmarket")]
    [RequestSizeLimit(50 * 1024 * 1024)] // 50 MB
    public async Task<IActionResult> SyncFromUpload(IFormFile file)
    {
        if (file == null || file.Length == 0)
            return BadRequest(new { error = "Dosya yüklenmedi." });

        if (!file.FileName.EndsWith(".db", StringComparison.OrdinalIgnoreCase))
            return BadRequest(new { error = "Sadece .db uzantılı SQLite dosyaları kabul edilir." });

        // Geçici dosyaya kaydet
        var tempPath = Path.Combine(Path.GetTempPath(), $"secmarket_sync_{Guid.NewGuid()}.db");
        try
        {
            await using (var stream = new FileStream(tempPath, FileMode.Create))
            {
                await file.CopyToAsync(stream);
            }

            _logger.LogInformation("Sync file uploaded: {FileName} ({Size} bytes)",
                file.FileName, file.Length);

            var result = await _syncService.SyncFromSqliteAsync(tempPath, GetCompanyId());
            return Ok(result);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Sync failed for file {FileName}", file.FileName);
            return StatusCode(500, new { error = $"Sync başarısız: {ex.Message}" });
        }
        finally
        {
            // Geçici dosyayı temizle
            if (System.IO.File.Exists(tempPath))
                System.IO.File.Delete(tempPath);
        }
    }

    /// <summary>Son 20 sync işleminin geçmişi</summary>
    [HttpGet("history")]
    public async Task<IActionResult> GetHistory()
    {
        var history = await _syncService.GetSyncHistoryAsync();
        return Ok(history.Select(h => new
        {
            h.Id,
            h.SyncedAt,
            h.SourceFile,
            h.ProductsAdded,
            h.BarcodesAdded,
            h.BarcodesSkipped,
            h.Status,
            h.Notes
        }));
    }
}
