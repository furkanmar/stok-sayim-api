using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using StokSayimApi.DTOs;
using StokSayimApi.Services;
using System.Security.Claims;

namespace StokSayimApi.Controllers;

[ApiController]
[Route("api/products")]
[Authorize]
public class ProductController : ControllerBase
{
    private readonly ProductService _productService;
    private readonly ILogger<ProductController> _logger;

    public ProductController(ProductService productService, ILogger<ProductController> logger)
    {
        _productService = productService;
        _logger = logger;
    }

    private int GetCompanyId() => int.Parse(User.FindFirstValue("companyId")!);
    private int GetUserId() => int.Parse(User.FindFirstValue(ClaimTypes.NameIdentifier)!);

    // ─── Barkod ile ürün getir ────────────────────────────────────────────────

    [HttpGet("{barcode}")]
    public async Task<IActionResult> GetByBarcode(string barcode)
    {
        var dto = await _productService.GetByBarcodeAsync(barcode, GetCompanyId());
        if (dto == null) return NotFound();
        return Ok(dto);
    }

    // ─── ProductId ile ürün getir ─────────────────────────────────────────────

    [HttpGet("id/{productId:int}")]
    public async Task<IActionResult> GetByProductId(int productId)
    {
        var dto = await _productService.GetByProductIdAsync(productId, GetCompanyId());
        if (dto == null) return NotFound();
        return Ok(dto);
    }

    // ─── Ürün arama (ad ile) ──────────────────────────────────────────────────

    [HttpGet("search")]
    public async Task<IActionResult> Search([FromQuery] string? marka, [FromQuery] string q)
    {
        if (string.IsNullOrWhiteSpace(q))
            return BadRequest(new { error = "q parametresi zorunludur." });

        var results = await _productService.SearchByNameAsync(marka, q, GetCompanyId());
        return Ok(results);
    }

    // ─── Marka listesi ────────────────────────────────────────────────────────

    [HttpGet("markas")]
    public async Task<IActionResult> GetMarkas()
    {
        var markas = await _productService.GetMarkasAsync(GetCompanyId());
        return Ok(markas);
    }

    // ─── Manuel ürün oluştur ──────────────────────────────────────────────────

    [HttpPost]
    public async Task<IActionResult> CreateProduct([FromBody] SaveProductDto dto)
    {
        var product = await _productService.SaveProductAsync(null, dto, GetCompanyId());
        return Ok(new { productId = product.ProductId, productName = product.ProductName });
    }

    // ─── Manuel ürün güncelle ─────────────────────────────────────────────────

    [HttpPut("{id:int}")]
    public async Task<IActionResult> UpdateProduct(int id, [FromBody] SaveProductDto dto)
    {
        try
        {
            var product = await _productService.SaveProductAsync(id, dto, GetCompanyId());
            return Ok(new { productId = product.ProductId, productName = product.ProductName });
        }
        catch (KeyNotFoundException ex)
        {
            return NotFound(new { error = ex.Message });
        }
    }

    // ─── Ürüne barkod ekle ────────────────────────────────────────────────────

    [HttpPost("{id:int}/barcodes")]
    public async Task<IActionResult> AddBarcode(int id, [FromBody] AddBarcodeDto dto)
    {
        try
        {
            var pb = await _productService.AddBarcodeAsync(
                id, dto.Barcode, dto.UnitType, dto.UnitQuantity, GetCompanyId());

            return Ok(new
            {
                id = pb.Id,
                productId = pb.ProductId,
                barcode = pb.Barcode,
                unitType = pb.UnitType,
                unitQuantity = pb.UnitQuantity
            });
        }
        catch (KeyNotFoundException ex)  { return NotFound(new { error = ex.Message }); }
        catch (InvalidOperationException ex) { return Conflict(new { error = ex.Message }); }
    }

    // ─── Barkod sil ──────────────────────────────────────────────────────────

    [HttpDelete("{id:int}/barcodes/{barcode}")]
    public async Task<IActionResult> DeleteBarcode(int id, string barcode)
    {
        try
        {
            await _productService.DeleteBarcodeAsync(id, barcode, GetCompanyId());
            return NoContent();
        }
        catch (KeyNotFoundException ex) { return NotFound(new { error = ex.Message }); }
    }

    // ─── Toplu fiyat kaydet ───────────────────────────────────────────────────

    [HttpPost("prices")]
    public async Task<IActionResult> SavePrices([FromBody] SavePricesDto dto)
    {
        try
        {
            await _productService.SavePricesAsync(dto, GetUserId(), GetCompanyId());
            return Ok();
        }
        catch (KeyNotFoundException ex) { return NotFound(new { error = ex.Message }); }
    }

    // ─── Toplu fiyat güncelleme (bulk) ───────────────────────────────────────

    [HttpPost("prices/bulk")]
    public async Task<IActionResult> BulkUpdatePrices([FromBody] List<BulkPriceItemDto> items)
    {
        if (items == null || items.Count == 0)
            return BadRequest(new { error = "Boş liste." });

        var result = await _productService.BulkUpdatePricesAsync(items, GetCompanyId());
        return Ok(result);
    }

    // ─── Ürün listesi (sayfalı) ───────────────────────────────────────────────

    [HttpGet("list")]
    public async Task<IActionResult> GetList(
        [FromQuery] string? q,
        [FromQuery] string? marka,
        [FromQuery] string? kategori,
        [FromQuery] bool? hasPrice,
        [FromQuery] int page = 1,
        [FromQuery] int pageSize = 30)
    {
        if (pageSize > 100) pageSize = 100;
        var result = await _productService.GetProductListAsync(q, marka, kategori, hasPrice, page, pageSize, GetCompanyId());
        return Ok(result);
    }

    // ─── Kategori listesi ─────────────────────────────────────────────────────

    [HttpGet("kategoriler")]
    public async Task<IActionResult> GetKategoriler()
    {
        var kategoriler = await _productService.GetKategorilerAsync(GetCompanyId());
        return Ok(kategoriler);
    }

    // ─── POS Export — tüm barkodlar + güncel fiyatlar ────────────────────────

    /// <summary>
    /// POS cihazı için tam ürün dump'ı — offline çalışma için kullanılır.
    /// Her barkod için: productId, productName, unitType, unitQuantity, satisFiyati, kdvOrani.
    /// </summary>
    [HttpGet("export")]
    public async Task<IActionResult> GetPosExport()
    {
        var result = await _productService.GetPosExportAsync(GetCompanyId());
        return Ok(result);
    }

    // ─── Fiyat geçmişi ────────────────────────────────────────────────────────

    [HttpGet("{id:int}/prices")]
    public async Task<IActionResult> GetPriceHistory(int id)
    {
        var prices = await _productService.GetPriceHistoryAsync(id);
        return Ok(prices.Select(p => new ProductPriceDto
        {
            Id = p.Id,
            UnitType = p.UnitType,
            AlisFiyati = p.AlisFiyati,
            SatisFiyati = p.SatisFiyati,
            GecerlilikTarihi = p.GecerlilikTarihi
        }));
    }
}
