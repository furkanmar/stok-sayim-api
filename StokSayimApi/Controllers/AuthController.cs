using Microsoft.AspNetCore.Mvc;
using StokSayimApi.DTOs;
using StokSayimApi.Services;

namespace StokSayimApi.Controllers;

[ApiController]
[Route("api/auth")]
public class AuthController : ControllerBase
{
    private readonly AuthService _authService;
    private readonly ILogger<AuthController> _logger;

    public AuthController(AuthService authService, ILogger<AuthController> logger)
    {
        _authService = authService;
        _logger = logger;
    }

    [HttpPost("login")]
    public async Task<IActionResult> Login([FromBody] LoginRequestDto dto)
    {
        var result = await _authService.LoginAsync(dto.Username, dto.Password);
        if (result == null)
            return Unauthorized(new { message = "Kullanıcı adı veya şifre hatalı." });

        return Ok(result);
    }
}