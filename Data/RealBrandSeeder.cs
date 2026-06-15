using uis_bachelor_sustainability_webapp.Models;
using uis_bachelor_sustainability_webapp.Services;

namespace uis_bachelor_sustainability_webapp.Data;

public static class RealBrandSeeder
{
    public static void Seed(AppDbContext db, ILogger logger)
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
            logger.LogInformation("Real brand seeding skipped. All configured real brands already exist.");
            return;
        }

        db.ClothingBrands.AddRange(missingBrands);
        db.SaveChanges();
        logger.LogInformation("Real brand seeding inserted {Count} brand(s).", missingBrands.Count);
    }

    private static List<ClothingBrand> BuildSeedBrands()
    {
        var template = GetDefaultCriteriaTemplate();

        return
        [
            BuildBrand(
                brandName: "H&M Group",
                description: "H&M (Hennes & Mauritz AB), is a Swedish multinational clothing company headquartered in Stockholm. The retailer sells apparel, accessories, and homeware.",
                logoPath: "/brand-logos/dd5489280b0c4c0ba435ce98f5946a98.png",
                criteria: FillCriteria(template, new Dictionary<string, decimal>
                {
                    ["Material:Fiber traceability"] = 100,
                    ["Material:Chemical management"] = 75,
                    ["Material:Recycled content / Preferred material content"] = 32,
                    ["Material:Certifications"] = 70,
                    ["Labor:Living wage commitment & coverage"] = 25,
                    ["Labor:Worker safety & working hours"] = 50,
                    ["Labor:Freedom of association / grievance mechanisms"] = 75,
                    ["Labor:Supplier audit transparency"] = 50,
                    ["Carbon:Scope 1-3 measurement"] = 100,
                    ["Carbon:Reduction targets & progress"] = 100,
                    ["Carbon:Renewable energy"] = 48,
                    ["Carbon:Transport & logistics"] = 50,
                    ["Longevity:Durability Testing / Expected Lifetime"] = 50,
                    ["Longevity:Repairability & Repair Services"] = 25,
                    ["Longevity:Circularity Programs"] = 100,
                }),
                sources:
                [
                    ("H&M Group Annual Sustainability Report 2025", "https://hmgroup.com/wp-content/uploads/2026/03/HM-Group-Annual-and-sustainability-report-2025.pdf"),
                    ("H&M Wikipedia", "https://en.wikipedia.org/wiki/H%26M"),
                ],
                certifications: ["GOTS", "SBTi", "GRS", "RE100", "FSC", "RWS"]
            ),
            BuildBrand(
                brandName: "Nike Inc",
                description: "Nike, Inc. is an American athletic footwear and apparel corporation with headquarter near Oregon. It is the world's largest supplier of athletic shoes and apparel, and a major manufacturer of sports equipment.",
                logoPath: "/brand-logos/6b574513ff22448ebb8b43c2e1416425.webp",
                criteria: FillCriteria(template, new Dictionary<string, decimal>
                {
                    ["Material:Fiber traceability"] = 100,
                    ["Material:Chemical management"] = 100,
                    ["Material:Certifications"] = 100,
                    ["Labor:Living wage commitment & coverage"] = 25,
                    ["Labor:Worker safety & working hours"] = 75,
                    ["Labor:Freedom of association / grievance mechanisms"] = 75,
                    ["Labor:Supplier audit transparency"] = 75,
                    ["Carbon:Scope 1-3 measurement"] = 100,
                    ["Carbon:Reduction targets & progress"] = 100,
                    ["Carbon:Renewable energy"] = 96,
                    ["Longevity:Repairability & Repair Services"] = 75,
                    ["Longevity:Circularity Programs"] = 75,
                }),
                sources:
                [
                    ("FY24 Sustainability Data", "https://media.about.nike.com/files/f37dfe60-0341-4db1-8ab9-6156da717313/FY24-NIKE%2C-Inc.-Sustainability-Data.pdf"),
                    ("NIKE, INC. CHEMISTRY PLAYBOOK", "https://irp.cdn-website.com/32615094/files/uploaded/ChemistryPlaybook_2026_0521.pdf"),
                    ("NIKE CODE LEADERSHIP STANDARDS", "https://media.about.nike.com/files/c0ab46f7-fafb-4fd3-b755-486349c3051d/Nike-Inc.-Code-Leadership-Standards-2025---English.pdf"),
                    ("Nike Wikipedia", "https://en.wikipedia.org/wiki/Nike,_Inc."),
                    ("Sustainability / Purpose Commitments", "https://about.nike.com/en/resources/sustainability-commitments"),
                    ("Circular Solutions", "https://www.nike.com/no/en/sustainability/services"),
                ],
                certifications: ["RE100"]
            ),
            BuildBrand(
                brandName: "Patagonia, Inc",
                description: "Patagonia, Inc. is an American retailer of outdoor recreation clothing, equipment, and food. It was founded 1973 and is based in California.",
                logoPath: "/brand-logos/a0e21dd96e714d2d8894db1ee5a0d61f.png",
                criteria: FillCriteria(template, new Dictionary<string, decimal>
                {
                    ["Material:Fiber traceability"] = 100,
                    ["Material:Chemical management"] = 75,
                    ["Material:Recycled content / Preferred material content"] = 84,
                    ["Material:Certifications"] = 100,
                    ["Labor:Living wage commitment & coverage"] = 75,
                    ["Labor:Worker safety & working hours"] = 75,
                    ["Labor:Freedom of association / grievance mechanisms"] = 75,
                    ["Labor:Supplier audit transparency"] = 50,
                    ["Carbon:Scope 1-3 measurement"] = 75,
                    ["Carbon:Reduction targets & progress"] = 75,
                    ["Carbon:Renewable energy"] = 98,
                    ["Carbon:Transport & logistics"] = 75,
                    ["Longevity:Durability Testing / Expected Lifetime"] = 75,
                    ["Longevity:Repairability & Repair Services"] = 100,
                    ["Longevity:Circularity Programs"] = 25,
                }),
                sources:
                [
                    ("Patagonia Progress Report 2025", "https://www.patagonia.com/media/pdf/patagonia-progress-report-2025.pdf"),
                    ("Workplace Compliance Benchmarks for Suppliers (2022)", "https://www.patagonia.com/on/demandware.static/-/Library-Sites-PatagoniaShared/default/dw118d994a/PDF-US/Patagonia-Benchmarks-2022.pdf"),
                    ("B Impact Score - Patagonia Inc", "https://www.bcorporation.net/en-us/find-a-b-corp/company/patagonia-inc/"),
                    ("Patagnia Inc Wikipedia", "https://en.wikipedia.org/wiki/Patagonia,_Inc."),
                ],
                certifications: ["SBTi", "GRS", "bluesign", "RWS", "GOTS", "RDS", "B Corp", "FSC"]
            ),
            BuildBrand(
                brandName: "Fj\u00E4llr\u00E4ven",
                description: "Fj\u00E4llr\u00E4ven is a Swedish brand specialising in outdoor equipment, mostly clothing and luggage.",
                logoPath: "/brand-logos/d47b7003adfb4287ab8996051ae1f2d1.png",
                criteria: FillCriteria(template, new Dictionary<string, decimal>
                {
                    ["Material:Fiber traceability"] = 100,
                    ["Material:Chemical management"] = 100,
                    ["Material:Certifications"] = 70,
                    ["Labor:Living wage commitment & coverage"] = 50,
                    ["Labor:Worker safety & working hours"] = 50,
                    ["Labor:Freedom of association / grievance mechanisms"] = 50,
                    ["Labor:Supplier audit transparency"] = 25,
                    ["Carbon:Scope 1-3 measurement"] = 50,
                    ["Carbon:Reduction targets & progress"] = 50,
                    ["Carbon:Renewable energy"] = 100,
                    ["Carbon:Transport & logistics"] = 75,
                    ["Longevity:Durability Testing / Expected Lifetime"] = 25,
                    ["Longevity:Repairability & Repair Services"] = 100,
                }),
                sources:
                [
                    ("Sustainability and CSR 2025", "https://www.fjallraven.com/49cc25/globalassets/fjallraven/eu/csr/fjr-csr-summary-2025.pdf"),
                    ("FENIX OUTDOOR Chemical Guideline and Restricted Substances List (RSL)", "https://www.fenixoutdoor.com/wp-content/uploads/2024/07/Guideline_Chemicals_EN_CLEAN-Rev-7.0-Fenix-Outdoor.pdf"),
                    ("Fj\u00E4llr\u00E4ven Wikipedia", "https://en.wikipedia.org/wiki/Fj%C3%A4llr%C3%A4ven"),
                ],
                certifications: []
            )
        ];
    }

    private static ClothingBrand BuildBrand(
        string brandName,
        string description,
        string? logoPath,
        List<BrandCriterionItem> criteria,
        IReadOnlyList<(string Title, string Url)> sources,
        IReadOnlyList<string> certifications)
    {
        var brand = new ClothingBrand
        {
            BrandName = brandName,
            Description = description,
            LogoPath = logoPath,
            CreatedAtUtc = DateTime.UtcNow,
            UpdatedAtUtc = DateTime.UtcNow,
        };

        foreach (var criterion in criteria)
        {
            brand.CriteriaItems.Add(criterion);
        }

        foreach (var source in sources)
        {
            brand.EvidenceSources.Add(new BrandEvidenceSource
            {
                SourceTitle = source.Title,
                SourceUrl = source.Url,
                CreatedAtUtc = DateTime.UtcNow,
            });
        }

        foreach (var certification in certifications.Distinct(StringComparer.OrdinalIgnoreCase))
        {
            brand.Certifications.Add(new BrandCertification
            {
                Name = certification,
                CreatedAtUtc = DateTime.UtcNow,
            });
        }

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
            new() { Category = "Carbon", Name = "Scope 1-3 measurement", Weight = 1m },
            new() { Category = "Carbon", Name = "Reduction targets & progress", Weight = 1m },
            new() { Category = "Carbon", Name = "Renewable energy", Unit = "%", Weight = 1m },
            new() { Category = "Carbon", Name = "Transport & logistics", Weight = 1m },
            new() { Category = "Longevity", Name = "Durability Testing / Expected Lifetime", Weight = 1m },
            new() { Category = "Longevity", Name = "Repairability & Repair Services", Weight = 1m },
            new() { Category = "Longevity", Name = "Circularity Programs", Weight = 1m },
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
