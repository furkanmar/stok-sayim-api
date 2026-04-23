using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using StokSayimApi.DTOs;
using StokSayimApi.Services;
using System.Security.Claims;

namespace StokSayimApi.Controllers;

[ApiController]
[Route("api/branches")]
[Authorize]
public class BranchController : ControllerBase
{
    private readonly BranchService _branchService;
    private readonly ILogger<BranchController> _logger;

    public BranchController(BranchService branchService, ILogger<BranchController> logger)
    {
        _branchService = branchService;
        _logger = logger;
    }

    private int GetCompanyId() => int.Parse(User.FindFirstValue("companyId")!);
    private int GetUserId() => int.Parse(User.FindFirstValue(ClaimTypes.NameIdentifier)!);
    private string GetRole() => User.FindFirstValue(ClaimTypes.Role) ?? "user";

    [HttpGet]
    public async Task<IActionResult> GetBranches()
    {
        var role = GetRole();
        List<Models.Branch> branches;

        // user (kasiyer) rolü: sadece kendine atanmış şubeler
        if (role == "user")
        {
            branches = await _branchService.GetBranchesByUserAsync(GetUserId(), GetCompanyId());
        }
        else
        {
            // admin / superadmin: şirketin tüm şubeleri
            branches = await _branchService.GetBranchesByCompanyAsync(GetCompanyId());
        }

        return Ok(branches.Select(b => new BranchDto { Id = b.Id, Name = b.Name }));
    }

    [HttpPost]
    [Authorize(Roles = "superadmin")]
    public async Task<IActionResult> CreateBranch([FromBody] CreateBranchDto dto)
    {
        var branch = await _branchService.CreateBranchAsync(dto.Name, GetCompanyId());
        return Ok(new BranchDto { Id = branch.Id, Name = branch.Name });
    }

    [HttpPut("{id}")]
    [Authorize(Roles = "superadmin")]
    public async Task<IActionResult> UpdateBranch(int id, [FromBody] CreateBranchDto dto)
    {
        var branch = await _branchService.UpdateBranchAsync(id, dto.Name, GetCompanyId());
        if (branch == null) return NotFound();
        return Ok(new BranchDto { Id = branch.Id, Name = branch.Name });
    }

    [HttpDelete("{id}")]
    [Authorize(Roles = "superadmin")]
    public async Task<IActionResult> DeleteBranch(int id)
    {
        var result = await _branchService.DeleteBranchAsync(id, GetCompanyId());
        if (!result) return NotFound();
        return NoContent();
    }
}