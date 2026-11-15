using Printly.Domain.Entities;

namespace Printly.Application.Services;

public static class ProductPricingCalculator
{
    /// <summary>
    /// Custo real total do lote. Gramas e horas sao por impressao (valor do fatiador);
    /// printRuns (quantidade / pecas por impressao) multiplica material, energia e desgaste.
    /// Watts, desgaste e falha vem da impressora escolhida; o kWh e global do usuario.
    /// </summary>
    public static decimal CalculateCost(
        IEnumerable<(decimal GramsUsed, decimal FilamentPricePerKg)> filaments,
        decimal totalHours,
        decimal printRuns,
        Printer printer,
        decimal kwhPrice)
    {
        var batchHours = totalHours * printRuns;

        var material = filaments.Sum(f => f.GramsUsed * printRuns / 1000m * f.FilamentPricePerKg);
        var energy = printer.AveragePowerWatts / 1000m * batchHours * kwhPrice;

        var depreciation = printer.DepreciationEnabled && printer.PrinterLifespanHours > 0
            ? printer.PrinterValue / printer.PrinterLifespanHours * batchHours
            : 0m;
        var maintenance = printer.MaintenanceEnabled
            ? depreciation * printer.MaintenancePercentage / 100m
            : 0m;

        var productionCost = material + energy + depreciation + maintenance;
        var failureLoss = printer.FailureEnabled
            ? productionCost * printer.FailureRatePercentage / 100m
            : 0m;

        return productionCost + failureLoss;
    }

    public static decimal SuggestSalePrice(decimal costPrice, Printer printer) =>
        costPrice * (1 + printer.DefaultProfitMarginPercentage / 100m);
}
