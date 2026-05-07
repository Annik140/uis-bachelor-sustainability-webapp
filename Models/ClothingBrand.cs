namespace uis_bachelor_sustainability_webapp.Models;

public class ClothingBrand
{
    public int Id { get; set; }
    public required string BrandName { get; set; }
    public string? Category { get; set; }
    public decimal? SustainabilityScore { get; set; }
    public DateTime CreatedAtUtc { get; set; } = DateTime.UtcNow;
}
