using Printly.Application.DTOs.Products;

namespace Printly.Application.Interfaces;

public interface IProductService
{
    Task<List<ProductDto>> GetAllAsync(Guid userId, DateTime? from = null, DateTime? to = null, CancellationToken cancellationToken = default);
    Task<ProductDto> GetByIdAsync(Guid id, Guid userId, CancellationToken cancellationToken = default);
    Task<ProductDto> CreateAsync(CreateProductRequest request, Guid userId, CancellationToken cancellationToken = default);
    Task<ProductDto> UpdateAsync(Guid id, UpdateProductRequest request, Guid userId, CancellationToken cancellationToken = default);
    Task<ProductDto> SetProducedAsync(Guid id, bool isProduced, Guid userId, CancellationToken cancellationToken = default);
    Task DeleteAsync(Guid id, Guid userId, CancellationToken cancellationToken = default);
}
