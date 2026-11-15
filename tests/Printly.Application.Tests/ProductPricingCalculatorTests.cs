using Printly.Application.Services;
using Printly.Domain.Entities;

namespace Printly.Application.Tests;

public class ProductPricingCalculatorTests
{
    // 100 g de filamento a R$ 100/kg = R$ 10 de material; 2 h a 500 W e R$ 1/kWh = R$ 1 de energia.
    private static readonly (decimal GramsUsed, decimal FilamentPricePerKg)[] Filaments = { (100m, 100m) };

    private static UserSettings Settings(
        bool depreciation = false,
        bool maintenance = false,
        bool failure = false,
        decimal margin = 50m) =>
        new(
            kwhPrice: 1m,
            averagePowerWatts: 500m,
            defaultProfitMarginPercentage: margin,
            printerValue: 2000m,
            printerLifespanHours: 1000m,
            maintenancePercentage: 10m,
            failureRatePercentage: 20m,
            depreciationEnabled: depreciation,
            maintenanceEnabled: maintenance,
            failureEnabled: failure,
            userId: Guid.NewGuid());

    [Fact]
    public void AllOptionalsOff_MatchesLegacyFormula()
    {
        var cost = ProductPricingCalculator.CalculateCost(Filaments, 2m, 1, Settings());

        Assert.Equal(11m, cost);
    }

    [Fact]
    public void PrintRuns_MultipliesMaterialAndEnergy()
    {
        // 10 pecas, 5 por impressao => 2 impressoes da mesa (100 g, 2 h cada).
        var cost = ProductPricingCalculator.CalculateCost(Filaments, 2m, 10m / 5, Settings());

        Assert.Equal(22m, cost);
    }

    [Fact]
    public void PrintRuns_IsProportionalForPartialPlate()
    {
        // 12 pecas, 5 por impressao => 2.4 impressoes.
        var cost = ProductPricingCalculator.CalculateCost(Filaments, 2m, 12m / 5, Settings());

        Assert.Equal(26.4m, cost);
    }

    [Fact]
    public void Depreciation_UsesPrinterValueOverLifespanTimesBatchHours()
    {
        // 2000 / 1000 = R$ 2/h; 2 h x 2 impressoes = 4 h => R$ 8. Material 20 + energia 2 + 8 = 30.
        var cost = ProductPricingCalculator.CalculateCost(Filaments, 2m, 2, Settings(depreciation: true));

        Assert.Equal(30m, cost);
    }

    [Fact]
    public void Maintenance_IsPercentageOfDepreciation()
    {
        // depreciacao 4 (2 h x R$ 2/h) + manutencao 10% = 0.4; material 10 + energia 1 => 15.4
        var cost = ProductPricingCalculator.CalculateCost(Filaments, 2m, 1, Settings(depreciation: true, maintenance: true));

        Assert.Equal(15.4m, cost);
    }

    [Fact]
    public void Maintenance_WithoutDepreciation_AddsNothing()
    {
        var cost = ProductPricingCalculator.CalculateCost(Filaments, 2m, 1, Settings(maintenance: true));

        Assert.Equal(11m, cost);
    }

    [Fact]
    public void FailureLoss_IsPercentageOfProductionCost()
    {
        // producao 11 + 20% = 13.2
        var cost = ProductPricingCalculator.CalculateCost(Filaments, 2m, 1, Settings(failure: true));

        Assert.Equal(13.2m, cost);
    }

    [Fact]
    public void SuggestSalePrice_AppliesProfitMargin()
    {
        var price = ProductPricingCalculator.SuggestSalePrice(20m, Settings(margin: 50m));

        Assert.Equal(30m, price);
    }
}
