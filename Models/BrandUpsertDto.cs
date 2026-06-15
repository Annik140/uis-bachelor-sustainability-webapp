namespace uis_bachelor_sustainability_webapp.Models;

public class BrandUpsertDto
{
    public required string BrandName { get; set; }
    public string? LogoPath { get; set; }
    public string? Description { get; set; }
    public List<BrandEvidenceSourceInputDto>? EvidenceSources { get; set; }
    public List<BrandCriterionItemInputDto>? CriteriaItems { get; set; }
    public List<BrandCertificationInputDto>? Certifications { get; set; }
}

public class BrandEvidenceSourceInputDto
{
    public required string SourceTitle { get; set; }
    public required string SourceUrl { get; set; }
}

public class BrandCriterionItemInputDto
{
    public required string Category { get; set; }
    public required string Name { get; set; }
    public decimal? NumericValue { get; set; }
    public string? Unit { get; set; }
    public decimal? Weight { get; set; }
    public string? Notes { get; set; }
}

public class BrandCertificationInputDto
{
    public required string Name { get; set; }
}
