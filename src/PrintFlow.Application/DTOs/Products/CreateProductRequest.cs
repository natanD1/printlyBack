namespace PrintFlow.Application.DTOs.Products;

public record CreateProductRequest(
    string ProductName,
    string? ProductPhotoBase64,
    decimal TotalHours,
    decimal? SalePriceOverride,
    List<ProductFilamentRequest> Filaments);
