namespace StokSayimApi.Models;

public class User
{
    public int Id { get; set; }
    public string Username { get; set; } = string.Empty;
    public string PasswordHash { get; set; } = string.Empty;
    public string Role { get; set; } = "counter";
    public int CompanyId { get; set; }

    public Company Company { get; set; } = null!;
    public ICollection<UserBranch> UserBranches { get; set; } = new List<UserBranch>();
}