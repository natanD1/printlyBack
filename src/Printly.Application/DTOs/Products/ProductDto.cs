namespace Printly.Application.DTOs.Products;

public record ProductDto(
    Guid Id,
    string ProductName,
    string? ProductPhoto,
    decimal TotalHours,
    int Quantity,
    int PiecesPerPrint,
    decimal BatchHours,
    decimal TotalFilament,
    decimal BatchFilament,
    decimal CostPrice,
    decimal SalePrice,
    decimal UnitSalePrice,
    decimal SuggestedSalePrice,
    IReadOnlyList<ProductFilamentDto> Filaments,
    DateTime CreatedAt,
    DateTime? UpdatedAt);
