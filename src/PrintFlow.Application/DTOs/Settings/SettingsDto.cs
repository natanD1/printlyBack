namespace PrintFlow.Application.DTOs.Settings;

public record SettingsDto(
    Guid Id,
    decimal KwhPrice,
    decimal AveragePowerWatts,
    decimal DefaultProfitMarginPercentage,
    DateTime CreatedAt,
    DateTime? UpdatedAt);
