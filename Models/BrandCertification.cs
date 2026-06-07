using System.Text.Json.Serialization;

namespace uis_bachelor_sustainability_webapp.Models;

public class BrandCertification
{
    public int Id { get; set; }
    public int ClothingBrandId { get; set; }
    public required string Name { get; set; }
    public DateTime CreatedAtUtc { get; set; } = DateTime.UtcNow;

    [JsonIgnore]
    public ClothingBrand? ClothingBrand { get; set; }
}
