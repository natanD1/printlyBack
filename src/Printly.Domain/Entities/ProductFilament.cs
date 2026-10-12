using Printly.Domain.Exceptions;

namespace Printly.Domain.Entities;

public class ProductFilament
{
    public Guid ProductId { get; private set; }
    public Guid FilamentId { get; private set; }
    public decimal GramsUsed { get; private set; }

    protected ProductFilament()
    {
    }

    public ProductFilament(Guid productId, Guid filamentId, decimal gramsUsed)
    {
        if (gramsUsed < 0)
            throw new DomainException("Gramas utilizadas nao pode ser negativo.");

        ProductId = productId;
        FilamentId = filamentId;
        GramsUsed = gramsUsed;
    }
}
