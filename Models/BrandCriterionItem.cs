namespace uis_bachelor_sustainability_webapp.Models;

public class BrandCriterionItem
{
    public int Id { get; set; }
    public int ClothingBrandId { get; set; }
    public required string Category { get; set; }
    public required string Name { get; set; }
    public decimal? NumericValue { get; set; }
    public string? Unit { get; set; }
    public decimal? GoodThreshold { get; set; }
    public decimal? WarningThreshold { get; set; }
    public bool LowerIsBetter { get; set; } = true;
    public decimal Weight { get; set; } = 1m;
    public string? Notes { get; set; }
    public DateTime CreatedAtUtc { get; set; } = DateTime.UtcNow;

    public ClothingBrand? ClothingBrand { get; set; }
}