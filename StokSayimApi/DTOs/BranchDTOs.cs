namespace StokSayimApi.DTOs;

public class BranchDto
{
    public int Id { get; set; }
    public string Name { get; set; } = string.Empty;
}

public class CreateBranchDto
{
    public string Name { get; set; } = string.Empty;
}