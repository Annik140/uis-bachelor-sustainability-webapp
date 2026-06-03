namespace uis_bachelor_sustainability_webapp.Models;

public class BrandEvidenceSource
{
    public int Id { get; set; }
    public int ClothingBrandId { get; set; }
    public required string SourceTitle { get; set; }
    public required string SourceUrl { get; set; }
    public string? SourceType { get; set; }
    public DateTime? PublishedAtUtc { get; set; }
    public string? Notes { get; set; }
    public DateTime CreatedAtUtc { get; set; } = DateTime.UtcNow;

    public ClothingBrand? ClothingBrand { get; set; }
}