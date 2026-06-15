using System.Text.Json.Serialization;

namespace uis_bachelor_sustainability_webapp.Models;

public class BrandEvidenceSource
{
    public int Id { get; set; }
    public int ClothingBrandId { get; set; }
    public required string SourceTitle { get; set; }
    public required string SourceUrl { get; set; }
    public DateTime CreatedAtUtc { get; set; } = DateTime.UtcNow;

    [JsonIgnore]
    public ClothingBrand? ClothingBrand { get; set; }
}