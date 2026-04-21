namespace StokSayimApi.Models;

public class Company
{
    public int Id { get; set; }
    public string Name { get; set; } = string.Empty;

    public ICollection<Branch> Branches { get; set; } = new List<Branch>();
    public ICollection<User> Users { get; set; } = new List<User>();
    public ICollection<Product> Products { get; set; } = new List<Product>();
}