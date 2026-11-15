namespace Printly.Application.DTOs.Products;

public record UpdateProductRequest(
    string ProductName,
    string? ProductPhotoBase64,
    decimal TotalHours,
    int Quantity,
    int PiecesPerPrint,
    decimal? SalePriceOverride,
    List<ProductFilamentRequest>? Filaments);
