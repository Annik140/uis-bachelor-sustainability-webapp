namespace uis_bachelor_sustainability_webapp.Models;

public class ClothingBrand
{
    public int Id { get; set; }
    public required string BrandName { get; set; }
    public string? LogoPath { get; set; }
    public string? Description { get; set; }
    public string? Category { get; set; }
    public string? ProsSummary { get; set; }
    public string? ConsSummary { get; set; }
    public decimal? MaterialSustainabilityScore { get; set; }
    public decimal? LaborPracticesScore { get; set; }
    public decimal? CarbonFootprintScore { get; set; }
    public decimal? ProductLongevityScore { get; set; }
    public int EvidenceSourceCount { get; set; }
    public decimal? SustainabilityScore { get; set; }
    public decimal? TransparencyScore { get; set; }
    public DateTime CreatedAtUtc { get; set; } = DateTime.UtcNow;
    public DateTime UpdatedAtUtc { get; set; } = DateTime.UtcNow;

    public ICollection<BrandEvidenceSource> EvidenceSources { get; set; } = new List<BrandEvidenceSource>();
    public ICollection<BrandCriterionItem> CriteriaItems { get; set; } = new List<BrandCriterionItem>();
    public ICollection<BrandCertification> Certifications { get; set; } = new List<BrandCertification>();
}
