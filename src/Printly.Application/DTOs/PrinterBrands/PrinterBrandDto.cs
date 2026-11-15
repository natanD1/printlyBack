namespace Printly.Application.DTOs.PrinterBrands;

public record PrinterBrandDto(Guid Id, string Name, bool IsActive, DateTime CreatedAt, DateTime? UpdatedAt);
