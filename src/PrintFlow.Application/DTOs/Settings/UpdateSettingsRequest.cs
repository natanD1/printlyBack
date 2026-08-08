namespace PrintFlow.Application.DTOs.Settings;

public record UpdateSettingsRequest(
    decimal KwhPrice,
    decimal AveragePowerWatts,
    decimal DefaultProfitMarginPercentage);
