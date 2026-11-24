namespace Printly.Application.DTOs.Showcases;

/// <summary>Produto produzido que ainda nao esta na vitrine.</summary>
public record AvailableProductDto(Guid Id, string Name, decimal UnitSalePrice, bool HasPhoto);
