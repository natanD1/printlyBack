namespace Printly.Application.DTOs.Printers;

public record PrinterDto(
    Guid Id,
    string? Nickname,
    string DisplayName,
    Guid PrinterModelId,
    string ModelName,
    Guid BrandId,
    string BrandName,
    string? ModelImage,
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
