using PrintFlow.Domain.Exceptions;

namespace PrintFlow.Domain.Entities;

public class Product : BaseEntity
{
    private readonly List<ProductFilament> _filaments = new();

    public string ProductName { get; private set; } = default!;
    public string? ProductPhoto { get; private set; }
    public decimal TotalHours { get; private set; }
    public decimal TotalFilament { get; private set; }
    public decimal CostPrice { get; private set; }
    public decimal SalePrice { get; private set; }
    public Guid UserId { get; private set; }
    public IReadOnlyCollection<ProductFilament> Filaments => _filaments.AsReadOnly();

    protected Product()
    {
    }

    public Product(string productName, string? productPhoto, decimal totalHours, Guid userId)
    {
        Validate(productName, totalHours);

        ProductName = productName.Trim();
        ProductPhoto = productPhoto;
        TotalHours = totalHours;
        UserId = userId;
    }

    public void Update(string productName, string? productPhoto, decimal totalHours)
    {
        Validate(productName, totalHours);

        ProductName = productName.Trim();
        ProductPhoto = productPhoto;
        TotalHours = totalHours;
        MarkAsUpdated();
    }

    public void SetFilaments(IEnumerable<(Guid FilamentId, decimal GramsUsed)> filaments)
    {
        var items = filaments.ToList();

        _filaments.Clear();
        foreach (var item in items)
            _filaments.Add(new ProductFilament(Id, item.FilamentId, item.GramsUsed));

        TotalFilament = items.Sum(f => f.GramsUsed);
        MarkAsUpdated();
    }

    public void SetPricing(decimal costPrice, decimal salePrice)
    {
        if (costPrice < 0)
            throw new DomainException("Preco de custo nao pode ser negativo.");

        if (salePrice < 0)
            throw new DomainException("Preco de venda nao pode ser negativo.");

        CostPrice = costPrice;
        SalePrice = salePrice;
        MarkAsUpdated();
    }

    private static void Validate(string productName, decimal totalHours)
    {
        if (string.IsNullOrWhiteSpace(productName))
            throw new DomainException("Nome do produto e obrigatorio.");

        if (totalHours < 0)
            throw new DomainException("Total de horas nao pode ser negativo.");
    }
}
