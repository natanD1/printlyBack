namespace Printly.Application.DTOs.PrinterModels;

public record SavePrinterModelRequest(string Name, Guid BrandId, string? ImageBase64, bool IsActive = true);
