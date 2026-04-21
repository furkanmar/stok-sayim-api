namespace StokSayimApi.DTOs;

public class UserDto
{
    public int Id { get; set; }
    public string Username { get; set; } = string.Empty;
    public string Role { get; set; } = string.Empty;
    public string CompanyId { get; set; } = string.Empty;
    public List<int> BranchIds { get; set; } = new();
}

public class CreateUserDto
{
    public string Username { get; set; } = string.Empty;
    public string Password { get; set; } = string.Empty;
    public string Role { get; set; } = string.Empty;
    public List<int> BranchIds { get; set; } = new();
}

public class UpdateUserDto
{
    public string Username { get; set; } = string.Empty;
    public string? Password { get; set; }
    public string Role { get; set; } = string.Empty;
    public List<int> BranchIds { get; set; } = new();
}