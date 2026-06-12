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
        return
        [
            BuildBrand(
                brandName: "H&M Group",
                description: "H&M (Hennes & Mauritz AB), is a Swedish multinational clothing company headquartered in Stockholm. The retailer sells apparel, accessories, and homeware.",
                logoPath: "/brand-logos/dd5489280b0c4c0ba435ce98f5946a98.png",
                criteria: BuildCriteria([
                    ("Material", "Fiber traceability", 100m, null),
                    ("Material", "Chemical management", 75m, null),
                    ("Material", "Recycled content / Preferred material content", 32m, "%"),
                    ("Material", "Certifications", 70m, null),
                    ("Labor", "Living wage commitment & coverage", 25m, null),
                    ("Labor", "Worker safety & working hours", 50m, null),
                    ("Labor", "Freedom of association / grievance mechanisms", 75m, null),
                    ("Labor", "Supplier audit transparency", 50m, null),
                    ("Carbon", "Scope 1-3 measurement", 100m, null),
                    ("Carbon", "Reduction targets & progress", 100m, null),
                    ("Carbon", "Renewable energy", 48m, "%"),
                    ("Carbon", "Transport & logistics", 50m, null),
                    ("Longevity", "Durability Testing / Expected Lifetime", 50m, null),
                    ("Longevity", "Repairability & Repair Services", 25m, null),
                    ("Longevity", "Circularity Programs", 100m, null),
                ]),
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
                criteria: BuildCriteria([
                    ("Material", "Fiber traceability", 100m, null),
                    ("Material", "Chemical management", 100m, null),
                    ("Material", "Recycled content / Preferred material content", null, "%"),
                    ("Material", "Certifications", 100m, null),
                    ("Labor", "Living wage commitment & coverage", 25m, null),
                    ("Labor", "Worker safety & working hours", 75m, null),
                    ("Labor", "Freedom of association / grievance mechanisms", 75m, null),
                    ("Labor", "Supplier audit transparency", 75m, null),
                    ("Carbon", "Scope 1-3 measurement", 100m, null),
                    ("Carbon", "Reduction targets & progress", 100m, null),
                    ("Carbon", "Renewable energy", 96m, "%"),
                    ("Carbon", "Transport & logistics", null, null),
                    ("Longevity", "Durability Testing / Expected Lifetime", null, null),
                    ("Longevity", "Repairability & Repair Services", 75m, null),
                    ("Longevity", "Circularity Programs", 75m, null),
                ]),
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
                criteria: BuildCriteria([
                    ("Material", "Fiber traceability", 100m, null),
                    ("Material", "Chemical management", 75m, null),
                    ("Material", "Recycled content / Preferred material content", 84m, "%"),
                    ("Material", "Certifications", 100m, null),
                    ("Labor", "Living wage commitment & coverage", 75m, null),
                    ("Labor", "Worker safety & working hours", 75m, null),
                    ("Labor", "Freedom of association / grievance mechanisms", 75m, null),
                    ("Labor", "Supplier audit transparency", 50m, null),
                    ("Carbon", "Scope 1-3 measurement", 75m, null),
                    ("Carbon", "Reduction targets & progress", 75m, null),
                    ("Carbon", "Renewable energy", 98m, "%"),
                    ("Carbon", "Transport & logistics", 75m, null),
                    ("Longevity", "Durability Testing / Expected Lifetime", 75m, null),
                    ("Longevity", "Repairability & Repair Services", 100m, null),
                    ("Longevity", "Circularity Programs", 25m, null),
                ]),
                sources:
                [
                    ("Patagonia Progress Report 2025", "https://www.patagonia.com/media/pdf/patagonia-progress-report-2025.pdf"),
                    ("Workplace Compliance Benchmarks for Suppliers (2022)", "https://www.patagonia.com/on/demandware.static/-/Library-Sites-PatagoniaShared/default/dw118d994a/PDF-US/Patagonia-Benchmarks-2022.pdf"),
                    ("B Impact Score - Patagonia Inc", "https://www.bcorporation.net/en-us/find-a-b-corp/company/patagonia-inc/"),
                    ("Patagnia Inc Wikipedia", "https://en.wikipedia.org/wiki/Patagonia,_Inc."),
                ],
                certifications: ["SBTi", "GRS", "bluesign", "RWS", "GOTS", "RDS", "B Corp", "FSC"]
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
                SourceType = string.Empty,
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

        brand.EvidenceSourceCount = brand.EvidenceSources.Count;
        BrandScoreCalculator.NormalizeCriteria(brand);
        BrandScoreCalculator.ApplyScores(brand);
        return brand;
    }

    private static List<BrandCriterionItem> BuildCriteria(
        IReadOnlyList<(string Category, string Name, decimal? NumericValue, string? Unit)> rows)
    {
        return rows.Select(row => new BrandCriterionItem
        {
            Category = row.Category,
            Name = row.Name,
            NumericValue = row.NumericValue,
            Unit = row.Unit ?? string.Empty,
            Weight = 1m,
            Notes = string.Empty,
            CreatedAtUtc = DateTime.UtcNow,
        }).ToList();
    }
}
