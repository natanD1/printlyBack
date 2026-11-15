using Printly.Application.DTOs.PrinterBrands;

namespace Printly.Application.Interfaces;

public interface IPrinterBrandService
{
    Task<List<PrinterBrandDto>> GetAllAsync(CancellationToken cancellationToken = default);
    Task<PrinterBrandDto> CreateAsync(SavePrinterBrandRequest request, CancellationToken cancellationToken = default);
    Task<PrinterBrandDto> UpdateAsync(Guid id, SavePrinterBrandRequest request, CancellationToken cancellationToken = default);
}
