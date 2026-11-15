namespace Printly.Application.DTOs.PrinterBrands;

public record SavePrinterBrandRequest(string Name, bool IsActive = true);
