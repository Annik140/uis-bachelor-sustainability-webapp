using uis_bachelor_sustainability_webapp.Models;

namespace uis_bachelor_sustainability_webapp.Services;

public static class BrandScoreCalculator
{
    private const decimal MaterialWeight = 0.30m;
    private const decimal LaborWeight = 0.20m;
    private const decimal CarbonWeight = 0.25m;
    private const decimal LongevityWeight = 0.25m;

    private static readonly IReadOnlyDictionary<string, CriterionDefinition> CriterionDefinitions =
        new Dictionary<string, CriterionDefinition>(StringComparer.OrdinalIgnoreCase)
        {
            // Material
            [Key("Material", "Fiber traceability")] = new("Material", "Fiber traceability", 0.25m, null),
            [Key("Material", "Chemical management")] = new("Material", "Chemical management", 0.25m, null),
            [Key("Material", "Recycled content / Preferred material content")] = new("Material", "Recycled content / Preferred material content", 0.25m, "%"),
            [Key("Material", "Certifications")] = new("Material", "Certifications", 0.25m, null),

            // Labor
            [Key("Labor", "Living wage commitment & coverage")] = new("Labor", "Living wage commitment & coverage", 0.30m, null),
            [Key("Labor", "Worker safety & working hours")] = new("Labor", "Worker safety & working hours", 0.30m, null),
            [Key("Labor", "Freedom of association / grievance mechanisms")] = new("Labor", "Freedom of association / grievance mechanisms", 0.20m, null),
            [Key("Labor", "Supplier audit transparency")] = new("Labor", "Supplier audit transparency", 0.20m, null),

            // Carbon
            [Key("Carbon", "Reduction targets & progress")] = new("Carbon", "Reduction targets & progress", 0.30m, null),
            [Key("Carbon", "Renewable energy")] = new("Carbon", "Renewable energy", 0.30m, "%"),
            [Key("Carbon", "Transport & logistics")] = new("Carbon", "Transport & logistics", 0.25m, null),
            [Key("Carbon", "Scope 1-3 measurement")] = new("Carbon", "Scope 1-3 measurement", 0.15m, null),

            // Longevity
            [Key("Longevity", "Durability Testing / Expected Lifetime")] = new("Longevity", "Durability Testing / Expected Lifetime", 0.35m, null),
            [Key("Longevity", "Repairability & Repair Services")] = new("Longevity", "Repairability & Repair Services", 0.30m, null),
            [Key("Longevity", "Circularity Programs")] = new("Longevity", "Circularity Programs", 0.35m, null)
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

        // Transparency reflects criteria completion (0-5), not sustainability performance.
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
        // Coverage penalty formula: S'c = Sc * (0.6 + 0.4 * rc)
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
            "Fiber traceability" => BuildTieredDisplay(
                value,
                "no fiber traceability disclosed",
                "traceability only covers direct suppliers",
                "traceability covers early supply-chain tiers",
                "traceability covers most supply-chain tiers",
                "traceability reaches farm-level sources"),
            "Chemical management" => BuildTieredDisplay(
                value,
                "no chemical management policy disclosed",
                "chemical policy disclosed but with limited evidence",
                "chemical controls and testing are in place",
                "recognized chemical standards are used",
                "chemical management is verified with published progress"),
            "Recycled content / Preferred material content" => BuildHalfThresholdDisplay(
                value,
                "no recycled or preferred content disclosed",
                "low recycled or preferred material use",
                "high recycled or preferred material use",
                "very high recycled or preferred material use"),
            "Certifications" => BuildCertificationDisplay(value),
            "Living wage commitment & coverage" => BuildTieredDisplay(
                value,
                "no living wage commitment disclosed",
                "living wage commitment stated with limited coverage evidence",
                "living wage pilot coverage reported",
                "partial living wage coverage documented",
                "majority living wage coverage documented"),
            "Worker safety & working hours" => BuildTieredDisplay(
                value,
                "no worker safety commitments disclosed",
                "basic worker safety policy disclosed",
                "worker safety audits reported",
                "worker safety performance metrics reported",
                "strong verified worker safety performance"),
            "Freedom of association / grievance mechanisms" => BuildTieredDisplay(
                value,
                "no worker voice or grievance mechanisms disclosed",
                "policy commitment without implementation evidence",
                "grievance mechanism or freedom-of-association policy disclosed",
                "grievance mechanism and freedom-of-association policy disclosed",
                "worker voice outcomes and remediation reported"),
            "Supplier audit transparency" => BuildTieredDisplay(
                value,
                "no supplier audit transparency disclosed",
                "audits mentioned without detail",
                "audit process and frequency disclosed",
                "audit findings or statistics published",
                "supplier findings and corrective-action progress published"),
            "Reduction targets & progress" => BuildTieredDisplay(
                value,
                "no emissions reduction targets disclosed",
                "general climate commitment disclosed",
                "quantified emissions reduction targets disclosed",
                "science-based targets disclosed",
                "science-based targets with measurable progress"),
            "Renewable energy" => BuildHalfThresholdDisplay(
                value,
                "no renewable energy adoption disclosed",
                "low renewable energy use",
                "high renewable energy use",
                "very high renewable energy use"),
            "Transport & logistics" => BuildTieredDisplay(
                value,
                "no transport or logistics initiatives disclosed",
                "limited logistics efficiency initiatives",
                "specific lower-carbon logistics actions disclosed",
                "comprehensive logistics strategy with targets",
                "logistics strategy with measurable emissions results"),
            "Scope 1-3 measurement" => BuildTieredDisplay(
                value,
                "no scope 1-3 emissions measurement disclosed",
                "scope 1 emissions measurement disclosed",
                "scope 1-2 emissions measurement disclosed",
                "scope 1-3 emissions measurement disclosed",
                "scope 1-3 measurement with methodology and trend data"),
            "Durability Testing / Expected Lifetime" => BuildTieredDisplay(
                value,
                "no durability testing or lifetime evidence disclosed",
                "general durability claims only",
                "internal durability testing disclosed",
                "standardized durability testing disclosed",
                "standardized testing with published durability results"),
            "Repairability & Repair Services" => BuildRepairabilityDisplay(value),
            "Circularity Programs" => BuildTieredDisplay(
                value,
                "no circularity programs disclosed",
                "general circularity commitment disclosed",
                "one active circularity program disclosed",
                "multiple active circularity programs disclosed",
                "multiple circularity programs with measurable outcomes"),
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

    // Fallback summary formatter, kept as safety net
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

    private static CriterionDisplayInfo BuildTieredDisplay(
        decimal value,
        string concern,
        string weakConcern,
        string weakStrength,
        string strength,
        string strongStrength)
    {
        if (value <= 0m)
        {
            return new CriterionDisplayInfo(concern, CriterionDisplayTier.Concern);
        }

        if (value <= 25m)
        {
            return new CriterionDisplayInfo(weakConcern, CriterionDisplayTier.WeakConcern);
        }

        if (value <= 50m)
        {
            return new CriterionDisplayInfo(weakStrength, CriterionDisplayTier.WeakStrength);
        }

        if (value <= 75m)
        {
            return new CriterionDisplayInfo(strength, CriterionDisplayTier.Strength);
        }

        return new CriterionDisplayInfo(strongStrength, CriterionDisplayTier.StrongStrength);
    }

    private static CriterionDisplayInfo BuildHalfThresholdDisplay(
        decimal value,
        string noInfoConcern,
        string underHalfConcern,
        string atLeastHalfStrength,
        string highStrength)
    {
        if (value <= 0m)
        {
            return new CriterionDisplayInfo(noInfoConcern, CriterionDisplayTier.Concern);
        }

        if (value < 50m)
        {
            return new CriterionDisplayInfo(underHalfConcern, CriterionDisplayTier.WeakConcern);
        }

        if (value < 75m)
        {
            return new CriterionDisplayInfo(atLeastHalfStrength, CriterionDisplayTier.Strength);
        }

        return new CriterionDisplayInfo(highStrength, CriterionDisplayTier.StrongStrength);
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