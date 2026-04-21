namespace StokSayimApi.Models;

public class Category
{
    public int Id { get; set; }
    public string Name { get; set; } = string.Empty;
    public int CompanyId { get; set; }

    public Company Company { get; set; } = null!;
    public ICollection<SubCategory> SubCategories { get; set; } = new List<SubCategory>();
}