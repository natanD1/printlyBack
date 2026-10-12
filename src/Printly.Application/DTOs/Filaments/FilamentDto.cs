using Printly.Application.DTOs.FilamentBrands;
using Printly.Application.DTOs.FilamentTypes;

namespace Printly.Application.DTOs.Filaments;

public record FilamentDto(
    Guid Id,
    string Name,
    FilamentBrandDto Brand,
    FilamentTypeDto Type,
    string ColorHex,
    decimal Weight,
    decimal InitialWeight,
    decimal FilamentPrice,
    DateTime CreatedAt,
    DateTime? UpdatedAt);
