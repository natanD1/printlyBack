namespace Printly.Application.DTOs.Products;

public record UpdateProductRequest(
    string ProductName,
    string? ProductPhotoBase64,
    decimal TotalHours,
    int Quantity,
    decimal? SalePriceOverride,
    List<ProductFilamentRequest>? Filaments);
