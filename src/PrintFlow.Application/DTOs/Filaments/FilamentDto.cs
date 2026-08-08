namespace PrintFlow.Application.DTOs.Filaments;

public record FilamentDto(
    Guid Id,
    string Name,
    string Brand,
    string ColorHex,
    decimal Weight,
    decimal FilamentPrice,
    DateTime CreatedAt,
    DateTime? UpdatedAt);
