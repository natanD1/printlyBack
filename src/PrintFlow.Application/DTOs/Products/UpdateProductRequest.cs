namespace PrintFlow.Application.DTOs.Products;

public record UpdateProductRequest(
    string ProductName,
    string? ProductPhotoBase64,
    decimal TotalHours,
    decimal? SalePriceOverride,
    List<ProductFilamentRequest>? Filaments);
