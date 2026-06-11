using uis_bachelor_sustainability_webapp.Models;
using uis_bachelor_sustainability_webapp.Services;

namespace uis_bachelor_sustainability_webapp.Data;

public static class DemoBrandSeeder
{
    public static void Seed(AppDbContext db)
    {
        var seedBrands = BuildSeedBrands();

        var existingNames = db.ClothingBrands
            .Select(brand => brand.BrandName)
            .ToHashSet(StringComparer.OrdinalIgnoreCase);

        var missingBrands = seedBrands
            .Where(brand => !existingNames.Contains(brand.BrandName))
            .ToList();

        if (missingBrands.Count == 0)
        {
            return;
        }

        db.ClothingBrands.AddRange(missingBrands);
        db.SaveChanges();
    }

    private static List<ClothingBrand> BuildSeedBrands()
    {
        var template = GetDefaultCriteriaTemplate();

        return
        [
            BuildBrand(
                brandName: "Pinnacle Proof",
                description: "Synthetic benchmark profile. Not a real brand. Maxed options across all categories.",
                criteria: FillCriteria(template, BuildAllCriteriaOverride(100m)),
                certifications: ["GOTS", "SBTi", "B Corp"],
                sourceTitle: "Pinnacle benchmark disclosure"
            ),
            BuildBrand(
                brandName: "Nadir Null",
                description: "Synthetic benchmark profile. Not a real brand. Lowest options selected for every criterion.",
                criteria: FillCriteria(template, BuildAllCriteriaOverride(0m)),
                certifications: [],
                sourceTitle: "Nadir benchmark disclosure"
            ),
            BuildBrand(
                brandName: "No Info Void",
                description: "Synthetic benchmark profile. Not a real brand. All criteria set to Information not found.",
                criteria: FillCriteria(template, new Dictionary<string, decimal>()),
                certifications: [],
                sourceTitle: "No-info benchmark stub"
            ),
            BuildBrand(
                brandName: "Circular Standard",
                description: "Synthetic profile. Broadly strong practices with mostly high option selections.",
                criteria: FillCriteria(template, new Dictionary<string, decimal>
                {
                    ["Material:Fiber traceability"] = 75,
                    ["Material:Chemical management"] = 75,
                    ["Material:Recycled content / Preferred material content"] = 72,
                    ["Material:Certifications"] = 70,
                    ["Labor:Living wage commitment & coverage"] = 75,
                    ["Labor:Worker safety & working hours"] = 75,
                    ["Labor:Freedom of association / grievance mechanisms"] = 75,
                    ["Labor:Supplier audit transparency"] = 75,
                    ["Carbon:Reduction targets & progress"] = 75,
                    ["Carbon:Renewable energy"] = 68,
                    ["Carbon:Transport & logistics"] = 75,
                    ["Carbon:Scope 1-3 measurement"] = 75,
                    ["Longevity:Durability Testing / Expected Lifetime"] = 75,
                    ["Longevity:Circularity Programs"] = 75,
                }),
                certifications: ["Fair Trade", "OEKO-TEX"],
                sourceTitle: "Circular Standard impact report"
            ),
            BuildBrand(
                brandName: "Progress Thread",
                description: "Synthetic profile. Mid-to-high performance with clear improvement trajectory.",
                criteria: FillCriteria(template, new Dictionary<string, decimal>
                {
                    ["Material:Fiber traceability"] = 50,
                    ["Material:Chemical management"] = 75,
                    ["Material:Recycled content / Preferred material content"] = 58,
                    ["Material:Certifications"] = 35,
                    ["Labor:Living wage commitment & coverage"] = 50,
                    ["Labor:Worker safety & working hours"] = 75,
                    ["Labor:Freedom of association / grievance mechanisms"] = 50,
                    ["Labor:Supplier audit transparency"] = 50,
                    ["Carbon:Reduction targets & progress"] = 75,
                    ["Carbon:Renewable energy"] = 54,
                    ["Carbon:Transport & logistics"] = 50,
                    ["Longevity:Durability Testing / Expected Lifetime"] = 75,
                    ["Longevity:Circularity Programs"] = 50,
                }),
                certifications: ["GRS"],
                sourceTitle: "Progress Thread annual sustainability update"
            ),
            BuildBrand(
                brandName: "Materials First",
                description: "Synthetic profile. Strong material choices, weaker disclosure in labor and carbon areas.",
                criteria: FillCriteria(template, new Dictionary<string, decimal>
                {
                    ["Material:Fiber traceability"] = 100,
                    ["Material:Chemical management"] = 75,
                    ["Material:Recycled content / Preferred material content"] = 84,
                    ["Material:Certifications"] = 70,
                    ["Labor:Living wage commitment & coverage"] = 50,
                    ["Labor:Worker safety & working hours"] = 50,
                    ["Labor:Supplier audit transparency"] = 25,
                    ["Carbon:Reduction targets & progress"] = 50,
                    ["Carbon:Transport & logistics"] = 25,
                    ["Longevity:Durability Testing / Expected Lifetime"] = 75,
                    ["Longevity:Circularity Programs"] = 50,
                    ["Longevity:Care Instructions & User Guidance"] = 50,
                }),
                certifications: ["GOTS", "FSC"],
                sourceTitle: "Materials First transparency brief"
            ),
            BuildBrand(
                brandName: "Labor Lift",
                description: "Synthetic profile. Labor-focused progress with moderate product and climate performance.",
                criteria: FillCriteria(template, new Dictionary<string, decimal>
                {
                    ["Material:Fiber traceability"] = 50,
                    ["Material:Chemical management"] = 50,
                    ["Material:Recycled content / Preferred material content"] = 40,
                    ["Material:Certifications"] = 35,
                    ["Labor:Living wage commitment & coverage"] = 100,
                    ["Labor:Worker safety & working hours"] = 100,
                    ["Labor:Freedom of association / grievance mechanisms"] = 75,
                    ["Labor:Supplier audit transparency"] = 75,
                    ["Carbon:Reduction targets & progress"] = 50,
                    ["Longevity:Durability Testing / Expected Lifetime"] = 50,
                    ["Longevity:Repairability & Repair Services"] = 50,
                }),
                certifications: ["SA8000"],
                sourceTitle: "Labor Lift social impact review"
            ),
            BuildBrand(
                brandName: "Carbon Climber",
                description: "Synthetic profile. Carbon strategy is strong while materials and longevity lag behind.",
                criteria: FillCriteria(template, new Dictionary<string, decimal>
                {
                    ["Material:Fiber traceability"] = 25,
                    ["Material:Chemical management"] = 50,
                    ["Material:Recycled content / Preferred material content"] = 32,
                    ["Labor:Living wage commitment & coverage"] = 50,
                    ["Labor:Supplier audit transparency"] = 50,
                    ["Carbon:Reduction targets & progress"] = 100,
                    ["Carbon:Renewable energy"] = 82,
                    ["Carbon:Transport & logistics"] = 75,
                    ["Carbon:Scope 1-3 measurement"] = 100,
                    ["Longevity:Durability Testing / Expected Lifetime"] = 25,
                }),
                certifications: ["SBTi"],
                sourceTitle: "Carbon Climber decarbonization pathway"
            ),
            BuildBrand(
                brandName: "Patch and Repair Co",
                description: "Synthetic profile. Longevity-centered model with modest transparency elsewhere.",
                criteria: FillCriteria(template, new Dictionary<string, decimal>
                {
                    ["Material:Fiber traceability"] = 50,
                    ["Material:Chemical management"] = 50,
                    ["Material:Recycled content / Preferred material content"] = 48,
                    ["Material:Certifications"] = 35,
                    ["Labor:Living wage commitment & coverage"] = 50,
                    ["Labor:Worker safety & working hours"] = 50,
                    ["Labor:Supplier audit transparency"] = 25,
                    ["Carbon:Reduction targets & progress"] = 25,
                    ["Carbon:Transport & logistics"] = 25,
                    ["Carbon:Scope 1-3 measurement"] = 25,
                    ["Longevity:Durability Testing / Expected Lifetime"] = 100,
                    ["Longevity:Repairability & Repair Services"] = 100,
                }),
                certifications: ["Cradle to Cradle"],
                sourceTitle: "Patch and Repair product lifespan dossier"
            ),
            BuildBrand(
                brandName: "Opaque Fastwear",
                description: "Synthetic profile. Low scores with partial disclosure to test weak concern states.",
                criteria: FillCriteria(template, new Dictionary<string, decimal>
                {
                    ["Material:Fiber traceability"] = 0,
                    ["Material:Chemical management"] = 25,
                    ["Labor:Worker safety & working hours"] = 25,
                    ["Labor:Supplier audit transparency"] = 0,
                    ["Carbon:Reduction targets & progress"] = 0,
                    ["Carbon:Transport & logistics"] = 25,
                    ["Longevity:Durability Testing / Expected Lifetime"] = 25,
                    ["Longevity:Repairability & Repair Services"] = 0,
                }),
                certifications: [],
                sourceTitle: "Opaque Fastwear limited disclosure note"
            )
        ];
    }

    private static Dictionary<string, decimal> BuildAllCriteriaOverride(decimal value)
    {
        return GetDefaultCriteriaTemplate()
            .ToDictionary(item => $"{item.Category}:{item.Name}", _ => value, StringComparer.OrdinalIgnoreCase);
    }

    private static ClothingBrand BuildBrand(
        string brandName,
        string description,
        List<BrandCriterionItem> criteria,
        IReadOnlyList<string> certifications,
        string sourceTitle,
        string? logoPath = null)
    {
        var brand = new ClothingBrand
        {
            BrandName = brandName,
            LogoPath = logoPath,
            Description = description,
            CreatedAtUtc = DateTime.UtcNow,
            UpdatedAtUtc = DateTime.UtcNow,
        };

        foreach (var criterion in criteria)
        {
            brand.CriteriaItems.Add(criterion);
        }

        brand.EvidenceSources.Add(new BrandEvidenceSource
        {
            SourceTitle = sourceTitle,
            SourceUrl = "https://example.com/report",
            SourceType = "Report",
            CreatedAtUtc = DateTime.UtcNow,
        });

        foreach (var certification in certifications.Distinct(StringComparer.OrdinalIgnoreCase))
        {
            brand.Certifications.Add(new BrandCertification
            {
                Name = certification,
                CreatedAtUtc = DateTime.UtcNow,
            });
        }

        brand.EvidenceSourceCount = brand.EvidenceSources.Count;
        BrandScoreCalculator.NormalizeCriteria(brand);
        BrandScoreCalculator.ApplyScores(brand);
        return brand;
    }

    private static List<BrandCriterionItem> GetDefaultCriteriaTemplate()
    {
        return
        [
            new() { Category = "Material", Name = "Fiber traceability", Unit = "%", Weight = 1m },
            new() { Category = "Material", Name = "Chemical management", Weight = 1m },
            new() { Category = "Material", Name = "Recycled content / Preferred material content", Unit = "%", Weight = 1m },
            new() { Category = "Material", Name = "Certifications", Weight = 1m },
            new() { Category = "Labor", Name = "Living wage commitment & coverage", Weight = 1m },
            new() { Category = "Labor", Name = "Worker safety & working hours", Weight = 1m },
            new() { Category = "Labor", Name = "Freedom of association / grievance mechanisms", Weight = 1m },
            new() { Category = "Labor", Name = "Supplier audit transparency", Weight = 1m },
            new() { Category = "Carbon", Name = "Reduction targets & progress", Weight = 1m },
            new() { Category = "Carbon", Name = "Renewable energy", Unit = "%", Weight = 1m },
            new() { Category = "Carbon", Name = "Transport & logistics", Weight = 1m },
            new() { Category = "Carbon", Name = "Scope 1-3 measurement", Weight = 1m },
            new() { Category = "Longevity", Name = "Durability Testing / Expected Lifetime", Weight = 1m },
            new() { Category = "Longevity", Name = "Repairability & Repair Services", Weight = 1m },
            new() { Category = "Longevity", Name = "Circularity Programs", Weight = 1m },
            new() { Category = "Longevity", Name = "Care Instructions & User Guidance", Weight = 1m },
        ];
    }

    private static List<BrandCriterionItem> FillCriteria(List<BrandCriterionItem> template, IReadOnlyDictionary<string, decimal> overrides)
    {
        return template.Select(item =>
        {
            var key = $"{item.Category}:{item.Name}";
            overrides.TryGetValue(key, out var value);

            return new BrandCriterionItem
            {
                Category = item.Category,
                Name = item.Name,
                NumericValue = overrides.ContainsKey(key) ? value : null,
                Unit = item.Unit,
                Weight = item.Weight,
                Notes = null,
                CreatedAtUtc = DateTime.UtcNow,
            };
        }).ToList();
    }
}
