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
        var material = ClampScore(brand.MaterialSustainabilityScore);
        var labor = ClampScore(brand.LaborPracticesScore);
        var carbon = ClampScore(brand.CarbonFootprintScore);
        var longevity = ClampScore(brand.ProductLongevityScore);

        brand.MaterialSustainabilityScore = material;
        brand.LaborPracticesScore = labor;
        brand.CarbonFootprintScore = carbon;
        brand.ProductLongevityScore = longevity;

        var weightedTotal = 0m;
        var weightSum = 0m;

        AddWeightedScore(material, MaterialWeight, ref weightedTotal, ref weightSum);
        AddWeightedScore(labor, LaborWeight, ref weightedTotal, ref weightSum);
        AddWeightedScore(carbon, CarbonWeight, ref weightedTotal, ref weightSum);
        AddWeightedScore(longevity, LongevityWeight, ref weightedTotal, ref weightSum);

        var coverageCount = CountProvidedScores(material, labor, carbon, longevity);
        var coverageRatio = coverageCount / 4m;

        if (weightSum > 0m)
        {
            var weightedAverage = weightedTotal / weightSum;
            var completenessPenalty = 0.60m + (0.40m * coverageRatio);
            brand.SustainabilityScore = RoundToOneDecimal(Clamp(weightedAverage * completenessPenalty, 1m, 10m));
        }
        else
        {
            brand.SustainabilityScore = null;
        }

        var sourceFactor = Clamp(brand.EvidenceSourceCount, 0, 4);
        var transparency = 1m + (coverageRatio * 2.5m) + (sourceFactor * 0.5m);
        brand.TransparencyScore = RoundToOneDecimal(Clamp(transparency, 1m, 5m));
        brand.UpdatedAtUtc = DateTime.UtcNow;
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

    private static int CountProvidedScores(params decimal?[] scores)
    {
        return scores.Count(score => score.HasValue);
    }

    private static decimal? ClampScore(decimal? score)
    {
        if (score is null)
        {
            return null;
        }

        return Clamp(score.Value, 0m, 10m);
    }

    private static decimal Clamp(decimal value, decimal min, decimal max)
    {
        return Math.Min(Math.Max(value, min), max);
    }

    private static decimal RoundToOneDecimal(decimal value)
    {
        return Math.Round(value, 1, MidpointRounding.AwayFromZero);
    }
}