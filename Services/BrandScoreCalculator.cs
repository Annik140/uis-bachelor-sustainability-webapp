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
            [Key("Material", "Fiber traceability")] = new("Material", "Fiber traceability", 0.30m, null),
            [Key("Material", "Chemical management")] = new("Material", "Chemical management", 0.25m, null),
            [Key("Material", "Recycled content / Preferred material content")] = new("Material", "Recycled content / Preferred material content", 0.25m, "%"),
            [Key("Material", "Certifications")] = new("Material", "Certifications", 0.20m, null),

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
            [Key("Longevity", "Circularity Programs")] = new("Longevity", "Circularity Programs", 0.20m, null)
        };

    public static void ApplyScores(ClothingBrand brand)
    {
        var criteriaItems = (brand.CriteriaItems ?? [])
            .Where(item => !IsRetiredCriterion(item))
            .ToList();
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

        // Only scoreable criteria contribute to sustainability. Empty/default selections are treated as missing data.
        var coverageCount = CountScoreableCriteria(criteriaItems);
        var totalCriteria = Math.Max(criteriaItems.Count, 1);
        var coverageRatio = coverageCount / (decimal)totalCriteria;

        if (weightSum > 0m)
        {
            // Category scores are computed on a 0-100 scale.
            var weightedAverage100 = weightedTotal / weightSum;
            brand.SustainabilityScore = RoundToOneDecimal(Clamp(weightedAverage100, 0m, 100m));
        }
        else
        {
            brand.SustainabilityScore = null;
        }

        var transparency = coverageRatio * 5m;
        brand.TransparencyScore = RoundToOneDecimal(Clamp(transparency, 0m, 5m));
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

    private static int CountScoreableCriteria(IEnumerable<BrandCriterionItem> criteriaItems)
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
                continue;
            }

            weightedTotal += itemScore.Value * item.Weight;
            weightSum += item.Weight;

            var displayInfo = BuildDisplayInfo(item);
            if (displayInfo is null)
            {
                continue;
            }

            if (displayInfo.Value.Tier is CriterionDisplayTier.Strength or CriterionDisplayTier.StrongStrength)
            {
                pros.Add(displayInfo.Value.Text);
            }
            else if (displayInfo.Value.Tier is CriterionDisplayTier.Concern or CriterionDisplayTier.WeakConcern)
            {
                cons.Add(displayInfo.Value.Text);
            }
        }

        if (weightSum == 0m)
        {
            return new CategoryScoreResult(null, 0m);
        }

        var categoryAverage = weightedTotal / weightSum;
        var categoryCoverageRatio = CountScoreableCriteria(categoryItems) / (decimal)Math.Max(categoryItems.Count, 1);
        var coveragePenaltyMultiplier = 0.6m + (0.4m * categoryCoverageRatio);
        var coverageAdjustedCategoryScore = categoryAverage * coveragePenaltyMultiplier;

        return new CategoryScoreResult(RoundToOneDecimal(Clamp(coverageAdjustedCategoryScore, 0m, 100m)), weightSum);
    }

    private static decimal? ScoreCriterion(BrandCriterionItem item)
    {
        if (!item.NumericValue.HasValue)
        {
            return null;
        }

        return Clamp(item.NumericValue.Value, 0m, 100m);
    }

    private static CriterionDisplayInfo? BuildDisplayInfo(BrandCriterionItem item)
    {
        if (!item.NumericValue.HasValue)
        {
            return null;
        }

        var value = Clamp(item.NumericValue.Value, 0m, 100m);

        return item.Name switch
        {
            "Fiber traceability" => BuildFiveOptionDisplay("traceability", value),
            "Chemical management" => BuildFiveOptionDisplay("chemical management", value),
            "Recycled content / Preferred material content" => BuildPercentDisplay("recycled content", value),
            "Certifications" => BuildCertificationDisplay(value),
            "Living wage commitment & coverage" => BuildFiveOptionDisplay("living wage coverage", value),
            "Worker safety & working hours" => BuildFiveOptionDisplay("worker safety", value),
            "Freedom of association / grievance mechanisms" => BuildFiveOptionDisplay("worker voice", value),
            "Supplier audit transparency" => BuildFiveOptionDisplay("audit transparency", value),
            "Reduction targets & progress" => BuildFiveOptionDisplay("reduction targets", value),
            "Renewable energy" => BuildPercentDisplay("renewable energy", value),
            "Transport & logistics" => BuildFiveOptionDisplay("transport and logistics", value),
            "Scope 1-3 measurement" => BuildFiveOptionDisplay("emissions measurement", value),
            "Durability Testing / Expected Lifetime" => BuildFiveOptionDisplay("durability", value),
            "Repairability & Repair Services" => BuildRepairabilityDisplay(value),
            "Circularity Programs" => BuildFiveOptionDisplay("circularity programs", value),
            _ => BuildFiveOptionDisplay(item.Name.ToLowerInvariant(), value)
        };
    }

    private static CriterionDisplayInfo BuildRepairabilityDisplay(decimal value)
    {
        if (value <= 0m)
        {
            return new CriterionDisplayInfo("no repair support", CriterionDisplayTier.Concern);
        }

        if (value <= 25m)
        {
            return new CriterionDisplayInfo("no repair support", CriterionDisplayTier.WeakConcern);
        }

        if (value <= 50m)
        {
            return new CriterionDisplayInfo("repair information available", CriterionDisplayTier.WeakStrength);
        }

        if (value <= 75m)
        {
            return new CriterionDisplayInfo("repair services or repair program offered", CriterionDisplayTier.Strength);
        }

        return new CriterionDisplayInfo("repair services with measurable usage/results", CriterionDisplayTier.StrongStrength);
    }

    private static CriterionDisplayInfo BuildFiveOptionDisplay(string noun, decimal value)
    {
        if (value <= 0m)
        {
            return new CriterionDisplayInfo($"no {noun}", CriterionDisplayTier.Concern);
        }

        if (value <= 25m)
        {
            return new CriterionDisplayInfo($"limited {noun}", CriterionDisplayTier.WeakConcern);
        }

        if (value <= 50m)
        {
            return new CriterionDisplayInfo($"some {noun}", CriterionDisplayTier.WeakStrength);
        }

        if (value <= 75m)
        {
            return new CriterionDisplayInfo($"good {noun}", CriterionDisplayTier.Strength);
        }

        return new CriterionDisplayInfo($"high {noun}", CriterionDisplayTier.StrongStrength);
    }

    private static CriterionDisplayInfo BuildPercentDisplay(string noun, decimal value)
    {
        return BuildFiveOptionDisplay(noun, value);
    }

    private static CriterionDisplayInfo BuildCertificationDisplay(decimal value)
    {
        if (value <= 0m)
        {
            return new CriterionDisplayInfo("no certifications", CriterionDisplayTier.Concern);
        }

        if (value <= 35m)
        {
            return new CriterionDisplayInfo("one relevant certification", CriterionDisplayTier.WeakStrength);
        }

        if (value <= 70m)
        {
            return new CriterionDisplayInfo("multiple relevant certifications", CriterionDisplayTier.Strength);
        }

        return new CriterionDisplayInfo("broad certification coverage", CriterionDisplayTier.StrongStrength);
    }

    private static bool IsRetiredCriterion(BrandCriterionItem item)
    {
        return string.Equals(item.Category, "Longevity", StringComparison.OrdinalIgnoreCase)
            && string.Equals(item.Name, "Care Instructions & User Guidance", StringComparison.OrdinalIgnoreCase);
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

    private readonly record struct CriterionDisplayInfo(string Text, CriterionDisplayTier Tier);

    private enum CriterionDisplayTier
    {
        Concern,
        WeakConcern,
        WeakStrength,
        Strength,
        StrongStrength
    }

    private readonly record struct CategoryScoreResult(decimal? Score, decimal WeightSum);

    private readonly record struct CriterionDefinition(
        string Category,
        string Name,
        decimal Weight,
        string? Unit);
}