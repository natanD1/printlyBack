namespace Printly.Application.DTOs.FilamentBrands;

public record FilamentBrandDto(
    Guid Id,
    string NameBrand,
    string? ImageBrand,
    bool IsActive,
    DateTime CreatedAt,
    DateTime? UpdatedAt);
