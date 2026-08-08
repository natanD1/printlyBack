namespace PrintFlow.Application.DTOs.Products;

public record ProductFilamentDto(
    Guid FilamentId,
    string Name,
    string Brand,
    string ColorHex,
    decimal GramsUsed);
