namespace Printly.Application.DTOs.Products;

public record CreateProductRequest(
    string ProductName,
    string? ProductPhotoBase64,
    decimal TotalHours,
    int Quantity,
    int PiecesPerPrint,
    Guid? PrinterId,
    decimal? SalePriceOverride,
    List<ProductFilamentRequest> Filaments);
