namespace Printly.Application.DTOs.Settings;

public record UpdateSettingsRequest(
    decimal KwhPrice,
    decimal AveragePowerWatts,
    decimal DefaultProfitMarginPercentage,
    decimal PrinterValue,
    decimal PrinterLifespanHours,
    decimal MaintenancePercentage,
    decimal FailureRatePercentage,
    bool DepreciationEnabled,
    bool MaintenanceEnabled,
    bool FailureEnabled);
