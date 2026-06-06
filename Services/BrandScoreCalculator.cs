using uis_bachelor_sustainability_webapp.Models;

namespace uis_bachelor_sustainability_webapp.Services;

public static class BrandScoreCalculator
{
    private const decimal MaterialWeight = 0.25m;
    private const decimal LaborWeight = 0.30m;
    private const decimal CarbonWeight = 0.25m;
    private const decimal LongevityWeight = 0.20m;

    private static readonly IReadOnlyDictionary<string, CriterionDefinition> CriterionDefinitions =
        new Dictionary<string, CriterionDefinition>(StringComparer.OrdinalIgnoreCase)
        {
            // Material
            [Key("Material", "Fiber traceability")] = new("Material", "Fiber traceability", 0.30m, "%"),
            [Key("Material", "Chemical management")] = new("Material", "Chemical management", 0.20m, null),
            [Key("Material", "Recycled content / Preferred material content")] = new("Material", "Recycled content / Preferred material content", 0.20m, "%"),
            [Key("Material", "Certifications")] = new("Material", "Certifications", 0.15m, null),
            [Key("Material", "Packaging sustainability")] = new("Material", "Packaging sustainability", 0.15m, "%"),

            // Labor
            [Key("Labor", "Living wage commitment & coverage")] = new("Labor", "Living wage commitment & coverage", 0.35m, null),
            [Key("Labor", "Worker safety & working hours")] = new("Labor", "Worker safety & working hours", 0.25m, null),
            [Key("Labor", "Freedom of association / grievance mechanisms")] = new("Labor", "Freedom of association / grievance mechanisms", 0.20m, null),
            [Key("Labor", "Supplier audit transparency")] = new("Labor", "Supplier audit transparency", 0.20m, null),

            // Carbon
            [Key("Carbon", "Reduction targets & progress")] = new("Carbon", "Reduction targets & progress", 0.35m, null),
            [Key("Carbon", "Renewable energy")] = new("Carbon", "Renewable energy", 0.30m, "%"),
            [Key("Carbon", "Transport & logistics")] = new("Carbon", "Transport & logistics", 0.20m, null),
            [Key("Carbon", "Scope 1-3 measurement")] = new("Carbon", "Scope 1-3 measurement", 0.15m, null),

            // Longevity
            [Key("Longevity", "Durability Testing / Expected Lifetime")] = new("Longevity", "Durability Testing / Expected Lifetime", 0.40m, null),
            [Key("Longevity", "Repairability & Repair Services")] = new("Longevity", "Repairability & Repair Services", 0.30m, null),
            [Key("Longevity", "Circularity Programs")] = new("Longevity", "Circularity Programs", 0.20m, null),
            [Key("Longevity", "Care Instructions & User Guidance")] = new("Longevity", "Care Instructions & User Guidance", 0.10m, null)
        };

    public static void ApplyScores(ClothingBrand brand)
    {
        var criteriaItems = brand.CriteriaItems?.ToList() ?? new List<BrandCriterionItem>();
        var pros = new List<string>();
        var cons = new List<string>();

        var material = CalculateCategoryScore(criteriaItems, "Material", pros, cons);
        var labor = CalculateCategoryScore(criteriaItems, "Labor", pros, cons);
        var carbon = CalculateCategoryScore(criteriaItems, "Carbon", pros, cons);
        var longevity = CalculateCategoryScore(criteriaItems, "Longevity", pros, cons);

        brand.MaterialSustainabilityScore = material.Score;
        brand.LaborPracticesScore = labor.Score;
        brand.CarbonFootprintScore = carbon.Score;
        brand.ProductLongevityScore = longevity.Score;

        var weightedTotal = 0m;
        var weightSum = 0m;

        AddWeightedScore(material.Score, MaterialWeight, ref weightedTotal, ref weightSum);
        AddWeightedScore(labor.Score, LaborWeight, ref weightedTotal, ref weightSum);
        AddWeightedScore(carbon.Score, CarbonWeight, ref weightedTotal, ref weightSum);
        AddWeightedScore(longevity.Score, LongevityWeight, ref weightedTotal, ref weightSum);

        var coverageCount = CountProvidedCriteria(criteriaItems);
        var totalCriteria = Math.Max(criteriaItems.Count, 1);
        var coverageRatio = coverageCount / (decimal)totalCriteria;

        if (weightSum > 0m)
        {
            // Category scores are computed on a 0-100 scale; convert final score to 1-10.
            var weightedAverage100 = weightedTotal / weightSum;
            brand.SustainabilityScore = RoundToOneDecimal(Clamp(weightedAverage100 / 10m, 1m, 10m));
        }
        else
        {
            brand.SustainabilityScore = null;
        }

        var transparency = 1m + (coverageRatio * 4m);
        brand.TransparencyScore = RoundToOneDecimal(Clamp(transparency, 1m, 5m));
        brand.ProsSummary = BuildSummary(pros);
        brand.ConsSummary = BuildSummary(cons);
        brand.UpdatedAtUtc = DateTime.UtcNow;
    }

    public static void NormalizeCriteria(ClothingBrand brand)
    {
        if (brand.CriteriaItems is null)
        {
            return;
        }

        foreach (var item in brand.CriteriaItems)
        {
            item.Category = NormalizeCategory(item.Category);
            item.Name = item.Name.Trim();
            item.Unit = item.Unit?.Trim();
            item.Notes = item.Notes?.Trim();

            var definition = ResolveDefinition(item.Category, item.Name);
            if (definition is not null)
            {
                item.Weight = definition.Value.Weight;
                item.Unit = definition.Value.Unit ?? item.Unit;
            }
            else
            {
                item.Weight = Clamp(item.Weight, 0.1m, 10m);
            }

            if (item.NumericValue.HasValue)
            {
                item.NumericValue = RoundToOneDecimal(item.NumericValue.Value);
            }
        }
    }

    private static void AddWeightedScore(decimal? score, decimal weight, ref decimal weightedTotal, ref decimal weightSum)
    {
        if (score is null)
        {
            return;
        }

        weightedTotal += score.Value * weight;
        weightSum += weight;
    }

    private static int CountProvidedCriteria(IEnumerable<BrandCriterionItem> criteriaItems)
    {
        return criteriaItems.Count(item => item.NumericValue.HasValue);
    }

    private static CategoryScoreResult CalculateCategoryScore(List<BrandCriterionItem> criteriaItems, string category, List<string> pros, List<string> cons)
    {
        var categoryItems = criteriaItems.Where(item => string.Equals(item.Category, category, StringComparison.OrdinalIgnoreCase)).ToList();
        if (categoryItems.Count == 0)
        {
            return new CategoryScoreResult(null, 0m);
        }

        var weightedTotal = 0m;
        var weightSum = 0m;

        foreach (var item in categoryItems)
        {
            var itemScore = ScoreCriterion(item);
            if (itemScore is null)
            {
                cons.Add($"{item.Name}: data is missing.");
                continue;
            }

            weightedTotal += itemScore.Value * item.Weight;
            weightSum += item.Weight;

            var reason = BuildReason(item);
            if (itemScore >= 75m)
            {
                pros.Add($"{item.Name}: {reason}");
            }
            else if (itemScore <= 40m)
            {
                cons.Add($"{item.Name}: {reason}");
            }
        }

        if (weightSum == 0m)
        {
            return new CategoryScoreResult(null, 0m);
        }

        return new CategoryScoreResult(RoundToOneDecimal(weightedTotal / weightSum), weightSum);
    }

    private static decimal? ScoreCriterion(BrandCriterionItem item)
    {
        if (!item.NumericValue.HasValue)
        {
            return null;
        }

        return Clamp(item.NumericValue.Value, 0m, 100m);
    }

    private static string BuildReason(BrandCriterionItem item)
    {
        if (!item.NumericValue.HasValue)
        {
            return "no numeric data was provided";
        }

        var unit = string.IsNullOrWhiteSpace(item.Unit) ? string.Empty : $" {item.Unit}";
        return $"scored {item.NumericValue.Value:0.#}/100{unit}.";
    }

    private static string NormalizeCategory(string category)
    {
        var normalized = category.Trim().ToLowerInvariant();
        return normalized switch
        {
            "material" => "Material",
            "labor" => "Labor",
            "labour" => "Labor",
            "carbon" => "Carbon",
            "longevity" => "Longevity",
            _ => category.Trim()
        };
    }

    private static string? BuildSummary(List<string> items)
    {
        if (items.Count == 0)
        {
            return null;
        }

        return string.Join("\n", items.Take(8));
    }

    private static decimal Clamp(decimal value, decimal min, decimal max)
    {
        return Math.Min(Math.Max(value, min), max);
    }

    private static string Key(string category, string name) => $"{category}:{name}";

    private static CriterionDefinition? ResolveDefinition(string category, string name)
    {
        CriterionDefinitions.TryGetValue(Key(category, name), out var definition);
        return definition;
    }

    private static decimal RoundToOneDecimal(decimal value)
    {
        return Math.Round(value, 1, MidpointRounding.AwayFromZero);
    }

    private readonly record struct CategoryScoreResult(decimal? Score, decimal WeightSum);

    private readonly record struct CriterionDefinition(
        string Category,
        string Name,
        decimal Weight,
        string? Unit);
}