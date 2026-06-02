namespace uis_bachelor_sustainability_webapp.Models;

public class ClothingBrand
{
    public int Id { get; set; }
    public required string BrandName { get; set; }
    public string? Category { get; set; }
    public decimal? MaterialSustainabilityScore { get; set; }
    public decimal? LaborPracticesScore { get; set; }
    public decimal? CarbonFootprintScore { get; set; }
    public decimal? ProductLongevityScore { get; set; }
    public int EvidenceSourceCount { get; set; }
    public decimal? SustainabilityScore { get; set; }
    public decimal? TransparencyScore { get; set; }
    public DateTime CreatedAtUtc { get; set; } = DateTime.UtcNow;
    public DateTime UpdatedAtUtc { get; set; } = DateTime.UtcNow;
}
