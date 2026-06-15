using uis_bachelor_sustainability_webapp.Models;
using uis_bachelor_sustainability_webapp.Services;

namespace uis_bachelor_sustainability_webapp.Tests;

public class BrandScoreCalculatorTests
{
    [Fact]
    public void ApplyScores_WithNoCriteria_SetsNullSustainabilityAndZeroTransparency()
    {
        var before = DateTime.UtcNow;
        var brand = new ClothingBrand { BrandName = "Empty" };

        BrandScoreCalculator.ApplyScores(brand);

        Assert.Null(brand.SustainabilityScore);
        Assert.Equal(0m, brand.TransparencyScore);
        Assert.True(brand.UpdatedAtUtc >= before);
    }

    [Fact]
    public void ApplyScores_WithOneCriterionPerCategory_ComputesExpectedWeightedSustainability()
    {
        var brand = new ClothingBrand
        {
            BrandName = "Weighted",
            CriteriaItems = new List<BrandCriterionItem>
            {
                new() { Category = "Material", Name = "Fiber traceability", NumericValue = 100m, Weight = 1m },
                new() { Category = "Labor", Name = "Living wage commitment & coverage", NumericValue = 50m, Weight = 1m },
                new() { Category = "Carbon", Name = "Scope 1-3 measurement", NumericValue = 0m, Weight = 1m },
                new() { Category = "Longevity", Name = "Durability Testing / Expected Lifetime", NumericValue = 75m, Weight = 1m },
            }
        };

        BrandScoreCalculator.ApplyScores(brand);

        Assert.Equal(58.8m, brand.SustainabilityScore);
        Assert.Equal(5m, brand.TransparencyScore);
        Assert.Equal(100m, brand.MaterialSustainabilityScore);
        Assert.Equal(50m, brand.LaborPracticesScore);
        Assert.Equal(0m, brand.CarbonFootprintScore);
        Assert.Equal(75m, brand.ProductLongevityScore);
    }

    [Fact]
    public void ApplyScores_ExcludesMissingNumericValuesFromSustainabilityButCountsForTransparency()
    {
        var brand = new ClothingBrand
        {
            BrandName = "MissingData",
            CriteriaItems = new List<BrandCriterionItem>
            {
                new() { Category = "Material", Name = "Fiber traceability", NumericValue = null, Weight = 1m },
                new() { Category = "Labor", Name = "Living wage commitment & coverage", NumericValue = 80m, Weight = 1m },
            }
        };

        BrandScoreCalculator.ApplyScores(brand);

        Assert.Equal(80m, brand.SustainabilityScore);
        Assert.Equal(2.5m, brand.TransparencyScore);
    }

    [Fact]
    public void NormalizeCriteria_NormalizesKnownDefinitionsAndClampsUnknownWeights()
    {
        var brand = new ClothingBrand
        {
            BrandName = "Normalize",
            CriteriaItems = new List<BrandCriterionItem>
            {
                new()
                {
                    Category = " labour ",
                    Name = " Living wage commitment & coverage ",
                    NumericValue = 63.37m,
                    Unit = "  %  ",
                    Weight = 9m,
                    Notes = "  noted  "
                },
                new()
                {
                    Category = "Other",
                    Name = " Unknown criterion ",
                    NumericValue = 12.34m,
                    Weight = 100m,
                }
            }
        };

        BrandScoreCalculator.NormalizeCriteria(brand);

        var known = brand.CriteriaItems.First(item => item.Category == "Labor");
        var unknown = brand.CriteriaItems.First(item => item.Category == "Other");

        Assert.Equal("Living wage commitment & coverage", known.Name);
        Assert.Equal(63.4m, known.NumericValue);
        Assert.Equal(0.30m, known.Weight);
        Assert.Equal("%", known.Unit);
        Assert.Equal("noted", known.Notes);

        Assert.Equal("Unknown criterion", unknown.Name);
        Assert.Equal(12.3m, unknown.NumericValue);
        Assert.True(unknown.Weight >= 0m);
    }
}
