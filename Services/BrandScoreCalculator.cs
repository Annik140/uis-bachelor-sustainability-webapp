using uis_bachelor_sustainability_webapp.Models;

namespace uis_bachelor_sustainability_webapp.Services;

public static class BrandScoreCalculator
{
    private const decimal MaterialWeight = 0.25m;
    private const decimal LaborWeight = 0.30m;
    private const decimal CarbonWeight = 0.25m;
    private const decimal LongevityWeight = 0.20m;

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
            item.Weight = Clamp(item.Weight, 0.1m, 10m);
            if (item.NumericValue.HasValue)
            {
                item.NumericValue = RoundToOneDecimal(item.NumericValue.Value);
            }
            if (item.GoodThreshold.HasValue)
            {
                item.GoodThreshold = RoundToOneDecimal(item.GoodThreshold.Value);
            }
            if (item.WarningThreshold.HasValue)
            {
                item.WarningThreshold = RoundToOneDecimal(item.WarningThreshold.Value);
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

    private static decimal RoundToOneDecimal(decimal value)
    {
        return Math.Round(value, 1, MidpointRounding.AwayFromZero);
    }

    private readonly record struct CategoryScoreResult(decimal? Score, decimal WeightSum);
}