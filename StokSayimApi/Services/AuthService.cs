using StokSayimApi.Data;
using StokSayimApi.Models;
using Microsoft.EntityFrameworkCore;
using StokSayimApi.DTOs;

namespace StokSayimApi.Services;

public class AuthService
{
    private readonly AppDbContext _db;
    private readonly JwtService _jwtService;
    private readonly ILogger<AuthService> _logger;

    public AuthService(AppDbContext db, JwtService jwtService, ILogger<AuthService> logger)
    {
        _db = db;
        _jwtService = jwtService;
        _logger = logger;
    }

    public async Task<LoginResponseDto?> LoginAsync(string username, string password)
    {
        _logger.LogInformation("Login attempt: {Username}", username);

        var user = await _db.Users
            .Include(u => u.Company)
            .FirstOrDefaultAsync(u => u.Username == username);

        if (user == null)
        {
            _logger.LogWarning("User not found: {Username}", username);
            return null;
        }

        if (!BCrypt.Net.BCrypt.Verify(password, user.PasswordHash))
        {
            _logger.LogWarning("Invalid password for user: {Username}", username);
            return null;
        }

        var token = _jwtService.GenerateToken(user);
        _logger.LogInformation("Login successful: {Username}", username);

        return new LoginResponseDto
        {
            Token = token,
            Role = user.Role,
            CompanyId = user.CompanyId.ToString(),
            Username = user.Username
        };
    }
}