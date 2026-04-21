using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using StokSayimApi.DTOs;
using StokSayimApi.Services;
using System.Security.Claims;

namespace StokSayimApi.Controllers;

[ApiController]
[Route("api/users")]
[Authorize]
public class UserController : ControllerBase
{
    private readonly UserService _userService;
    private readonly ILogger<UserController> _logger;

    public UserController(UserService userService, ILogger<UserController> logger)
    {
        _userService = userService;
        _logger = logger;
    }

    private int GetCompanyId() => int.Parse(User.FindFirstValue("companyId")!);
    private int GetUserId() => int.Parse(User.FindFirstValue(ClaimTypes.NameIdentifier)!);
    private string GetRole() => User.FindFirstValue(ClaimTypes.Role)!;

    [HttpGet]
    [Authorize(Roles = "admin,superadmin")]
    public async Task<IActionResult> GetUsers()
    {
        var users = await _userService.GetUsersByCompanyAsync(GetCompanyId());
        return Ok(users.Select(u => new UserDto
        {
            Id = u.Id,
            Username = u.Username,
            Role = u.Role,
            CompanyId = u.CompanyId.ToString(),
            BranchIds = u.UserBranches.Select(ub => ub.BranchId).ToList()
        }));
    }

    [HttpPost]
    [Authorize(Roles = "admin,superadmin")]
    public async Task<IActionResult> CreateUser([FromBody] CreateUserDto dto)
    {
        var role = GetRole();

        // Admin sadece counter ekleyebilir
        if (role == "admin" && dto.Role != "counter")
            return Forbid();

        var user = await _userService.CreateUserAsync(
            dto.Username, dto.Password, dto.Role, GetCompanyId(), dto.BranchIds);

        return Ok(new UserDto
        {
            Id = user.Id,
            Username = user.Username,
            Role = user.Role,
            CompanyId = user.CompanyId.ToString(),
            BranchIds = dto.BranchIds
        });
    }

    [HttpPut("{id}")]
    [Authorize(Roles = "admin,superadmin")]
    public async Task<IActionResult> UpdateUser(int id, [FromBody] UpdateUserDto dto)
    {
        var role = GetRole();

        // Admin sadece counter güncelleyebilir
        if (role == "admin" && dto.Role != "counter")
            return Forbid();

        var user = await _userService.UpdateUserAsync(
            id, dto.Username, dto.Password, dto.Role, GetCompanyId(), dto.BranchIds);

        if (user == null) return NotFound();

        return Ok(new UserDto
        {
            Id = user.Id,
            Username = user.Username,
            Role = user.Role,
            CompanyId = user.CompanyId.ToString(),
            BranchIds = dto.BranchIds
        });
    }

    [HttpDelete("{id}")]
    [Authorize(Roles = "superadmin")]
    public async Task<IActionResult> DeleteUser(int id)
    {
        var result = await _userService.DeleteUserAsync(id, GetCompanyId());
        if (!result) return NotFound();
        return NoContent();
    }
}