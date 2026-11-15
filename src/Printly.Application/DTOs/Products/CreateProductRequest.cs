namespace Printly.Application.DTOs.Products;

public record CreateProductRequest(
    string ProductName,
    string? ProductPhotoBase64,
    decimal TotalHours,
    int Quantity,
    int PiecesPerPrint,
    decimal? SalePriceOverride,
    List<ProductFilamentRequest> Filaments);
