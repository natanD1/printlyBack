namespace Printly.Application.DTOs.Printers;

public record SavePrinterRequest(
    Guid PrinterModelId,
    string? Nickname,
    decimal AveragePowerWatts,
    decimal DefaultProfitMarginPercentage,
    decimal PrinterValue,
    decimal PrinterLifespanHours,
    decimal MaintenancePercentage,
    decimal FailureRatePercentage,
    bool DepreciationEnabled,
    bool MaintenanceEnabled,
    bool FailureEnabled);
