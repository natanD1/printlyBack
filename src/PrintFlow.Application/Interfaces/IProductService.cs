using PrintFlow.Application.DTOs.Products;

namespace PrintFlow.Application.Interfaces;

public interface IProductService
{
    Task<List<ProductDto>> GetAllAsync(Guid userId, CancellationToken cancellationToken = default);
    Task<ProductDto> GetByIdAsync(Guid id, Guid userId, CancellationToken cancellationToken = default);
    Task<ProductDto> CreateAsync(CreateProductRequest request, Guid userId, CancellationToken cancellationToken = default);
    Task<ProductDto> UpdateAsync(Guid id, UpdateProductRequest request, Guid userId, CancellationToken cancellationToken = default);
    Task DeleteAsync(Guid id, Guid userId, CancellationToken cancellationToken = default);
}
