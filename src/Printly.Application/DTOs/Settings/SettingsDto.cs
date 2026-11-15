namespace Printly.Application.DTOs.Settings;

public record SettingsDto(
    Guid Id,
    decimal KwhPrice,
    DateTime CreatedAt,
    DateTime? UpdatedAt);
