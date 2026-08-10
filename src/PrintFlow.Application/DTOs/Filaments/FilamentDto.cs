using PrintFlow.Application.DTOs.FilamentBrands;
using PrintFlow.Application.DTOs.FilamentTypes;

namespace PrintFlow.Application.DTOs.Filaments;

public record FilamentDto(
    Guid Id,
    string Name,
    FilamentBrandDto Brand,
    FilamentTypeDto Type,
    string ColorHex,
    decimal Weight,
    decimal FilamentPrice,
    DateTime CreatedAt,
    DateTime? UpdatedAt);
