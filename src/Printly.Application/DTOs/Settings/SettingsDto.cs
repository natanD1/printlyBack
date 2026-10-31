namespace Printly.Application.DTOs.Settings;

public record SettingsDto(
    Guid Id,
    decimal KwhPrice,
    decimal AveragePowerWatts,
    decimal DefaultProfitMarginPercentage,
    decimal PrinterValue,
    decimal PrinterLifespanHours,
    decimal MaintenancePercentage,
    decimal FailureRatePercentage,
    bool DepreciationEnabled,
    bool MaintenanceEnabled,
    bool FailureEnabled,
    DateTime CreatedAt,
    DateTime? UpdatedAt);
