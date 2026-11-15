using Printly.Domain.Entities;

namespace Printly.Application.Services;

public static class ProductPricingCalculator
{
    /// <summary>
    /// Custo real total do lote. Gramas e horas sao por impressao (valor do fatiador);
    /// printRuns (quantidade / pecas por impressao) multiplica material, energia e desgaste.
    /// </summary>
    public static decimal CalculateCost(
        IEnumerable<(decimal GramsUsed, decimal FilamentPricePerKg)> filaments,
        decimal totalHours,
        decimal printRuns,
        UserSettings settings)
    {
        var batchHours = totalHours * printRuns;

        var material = filaments.Sum(f => f.GramsUsed * printRuns / 1000m * f.FilamentPricePerKg);
        var energy = settings.AveragePowerWatts / 1000m * batchHours * settings.KwhPrice;

        var depreciation = settings.DepreciationEnabled && settings.PrinterLifespanHours > 0
            ? settings.PrinterValue / settings.PrinterLifespanHours * batchHours
            : 0m;
        var maintenance = settings.MaintenanceEnabled
            ? depreciation * settings.MaintenancePercentage / 100m
            : 0m;

        var productionCost = material + energy + depreciation + maintenance;
        var failureLoss = settings.FailureEnabled
            ? productionCost * settings.FailureRatePercentage / 100m
            : 0m;

        return productionCost + failureLoss;
    }

    public static decimal SuggestSalePrice(decimal costPrice, UserSettings settings) =>
        costPrice * (1 + settings.DefaultProfitMarginPercentage / 100m);
}
