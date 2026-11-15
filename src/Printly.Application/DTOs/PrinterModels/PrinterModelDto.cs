namespace Printly.Application.DTOs.PrinterModels;

public record PrinterModelDto(
    Guid Id,
    string Name,
    Guid BrandId,
    string BrandName,
    string? Image,
    bool IsActive,
    DateTime CreatedAt,
    DateTime? UpdatedAt);
