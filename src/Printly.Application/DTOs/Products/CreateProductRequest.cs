namespace Printly.Application.DTOs.Products;

public record CreateProductRequest(
    string ProductName,
    string? ProductPhotoBase64,
    decimal TotalHours,
    int Quantity,
    decimal? SalePriceOverride,
    List<ProductFilamentRequest> Filaments);
