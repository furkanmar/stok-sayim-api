namespace StokSayimApi.DTOs;

public class CategoryDto
{
    public int Id { get; set; }
    public string Name { get; set; } = string.Empty;
    public List<SubCategoryDto> SubCategories { get; set; } = new();
}

public class SubCategoryDto
{
    public int Id { get; set; }
    public string Name { get; set; } = string.Empty;
    public int CategoryId { get; set; }
}

public class CreateCategoryDto
{
    public string Name { get; set; } = string.Empty;
}

public class CreateSubCategoryDto
{
    public string Name { get; set; } = string.Empty;
    public int CategoryId { get; set; }
}