namespace Printly.Application.DTOs.Products;

public record ProductDto(
    Guid Id,
    string ProductName,
    string? ProductPhoto,
    decimal TotalHours,
    decimal TotalFilament,
    decimal CostPrice,
    decimal SalePrice,
    decimal SuggestedSalePrice,
    IReadOnlyList<ProductFilamentDto> Filaments,
    DateTime CreatedAt,
    DateTime? UpdatedAt);
