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
            [Key("Material", "Fiber provenance")] = new("Material", "Fiber provenance", 0.30m, false, 8m, 4m, "%"),
            [Key("Material", "Material toxicity & chemical management")] = new("Material", "Material toxicity & chemical management", 0.20m, false, 8m, 4m, null),
            [Key("Material", "Recycled / regenerative content")] = new("Material", "Recycled / regenerative content", 0.20m, false, 50m, 20m, "%"),
            [Key("Material", "Certifications & standards")] = new("Material", "Certifications & standards", 0.15m, false, 8m, 4m, null),
            [Key("Material", "Packaging sustainability")] = new("Material", "Packaging sustainability", 0.15m, false, 80m, 50m, "%"),

            // Labor
            [Key("Labor", "Living wage coverage")] = new("Labor", "Living wage coverage", 0.35m, false, 100m, 80m, "%"),
            [Key("Labor", "Worker safety & working hours")] = new("Labor", "Worker safety & working hours", 0.25m, false, 8m, 4m, null),
            [Key("Labor", "Freedom of association / grievance mechanisms")] = new("Labor", "Freedom of association / grievance mechanisms", 0.20m, false, 8m, 4m, null),
            [Key("Labor", "Supplier audit transparency")] = new("Labor", "Supplier audit transparency", 0.20m, false, 8m, 4m, null),

            // Carbon
            [Key("Carbon", "Measured footprint (Scope 1–3)")] = new("Carbon", "Measured footprint (Scope 1–3)", 0.40m, true, 8m, 4m, null),
            [Key("Carbon", "Reduction targets & progress")] = new("Carbon", "Reduction targets & progress", 0.30m, false, 8m, 4m, null),
            [Key("Carbon", "Energy sourcing (% renewable)")] = new("Carbon", "Energy sourcing (% renewable)", 0.20m, false, 80m, 50m, "%"),
            [Key("Carbon", "Transport & logistics efficiency")] = new("Carbon", "Transport & logistics efficiency", 0.10m, false, 8m, 4m, null),

            // Longevity
            [Key("Longevity", "Durability testing / expected lifetime")] = new("Longevity", "Durability testing / expected lifetime", 0.40m, false, 8m, 4m, null),
            [Key("Longevity", "Repairability & spare parts")] = new("Longevity", "Repairability & spare parts", 0.30m, false, 8m, 4m, null),
            [Key("Longevity", "Design for timelessness / modularity")] = new("Longevity", "Design for timelessness / modularity", 0.20m, false, 8m, 4m, null),
            [Key("Longevity", "Care instructions & user guidance")] = new("Longevity", "Care instructions & user guidance", 0.10m, false, 8m, 4m, null)
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
            var weightedAverage = weightedTotal / weightSum;
            var completenessPenalty = 0.55m + (0.45m * coverageRatio);
            brand.SustainabilityScore = RoundToOneDecimal(Clamp(weightedAverage * completenessPenalty, 1m, 10m));
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
                item.LowerIsBetter = definition.Value.LowerIsBetter;
                item.GoodThreshold = definition.Value.GoodThreshold;
                item.WarningThreshold = definition.Value.WarningThreshold;
                item.Unit = definition.Value.Unit ?? item.Unit;
            }
            else
            {
                item.Weight = Clamp(item.Weight, 0.1m, 10m);
                if (item.GoodThreshold.HasValue)
                {
                    item.GoodThreshold = RoundToOneDecimal(item.GoodThreshold.Value);
                }
                if (item.WarningThreshold.HasValue)
                {
                    item.WarningThreshold = RoundToOneDecimal(item.WarningThreshold.Value);
                }
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
            if (itemScore >= 7m)
            {
                pros.Add($"{item.Name}: {reason}");
            }
            else if (itemScore <= 4m)
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

        var value = item.NumericValue.Value;
        var good = item.GoodThreshold;
        var warning = item.WarningThreshold;

        if (item.LowerIsBetter)
        {
            if (good.HasValue && value <= good.Value)
            {
                return 10m;
            }

            if (warning.HasValue && value <= warning.Value)
            {
                return 7m;
            }

            return 3m;
        }

        if (good.HasValue && value >= good.Value)
        {
            return 10m;
        }

        if (warning.HasValue && value >= warning.Value)
        {
            return 7m;
        }

        return 3m;
    }

    private static string BuildReason(BrandCriterionItem item)
    {
        if (!item.NumericValue.HasValue)
        {
            return "no numeric data was provided";
        }

        var unit = string.IsNullOrWhiteSpace(item.Unit) ? string.Empty : $" {item.Unit}";
        var value = item.NumericValue.Value.ToString("0.0");
        var good = item.GoodThreshold.HasValue ? item.GoodThreshold.Value.ToString("0.0") : null;
        var warning = item.WarningThreshold.HasValue ? item.WarningThreshold.Value.ToString("0.0") : null;

        if (item.LowerIsBetter)
        {
            if (good is not null && item.NumericValue.Value <= item.GoodThreshold!.Value)
            {
                return $"{value}{unit} is at or below the best threshold ({good}{unit}).";
            }

            if (warning is not null && item.NumericValue.Value <= item.WarningThreshold!.Value)
            {
                return $"{value}{unit} is between the best threshold ({good}{unit}) and the warning threshold ({warning}{unit}).";
            }

            return $"{value}{unit} is above the warning threshold ({warning ?? "n/a"}{unit}).";
        }

        if (good is not null && item.NumericValue.Value >= item.GoodThreshold!.Value)
        {
            return $"{value}{unit} is at or above the best threshold ({good}{unit}).";
        }

        if (warning is not null && item.NumericValue.Value >= item.WarningThreshold!.Value)
        {
            return $"{value}{unit} is between the warning threshold ({warning}{unit}) and the best threshold ({good}{unit}).";
        }

        return $"{value}{unit} is below the warning threshold ({warning ?? "n/a"}{unit}).";
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

    private readonly record struct CriterionDefinition(string Category, string Name, decimal Weight, bool LowerIsBetter, decimal GoodThreshold, decimal WarningThreshold, string? Unit);
}