using Printly.Domain.Exceptions;

namespace Printly.Domain.Entities;

public class Product : BaseEntity
{
    private readonly List<ProductFilament> _filaments = new();

    public string ProductName { get; private set; } = null!;
    public string? ProductPhoto { get; private set; }
    public decimal TotalHours { get; private set; }
    public int Quantity { get; private set; } = 1;
    public int PiecesPerPrint { get; private set; } = 1;
    public decimal TotalFilament { get; private set; }
    public decimal CostPrice { get; private set; }
    public decimal SalePrice { get; private set; }

    /// <summary>Impressora usada no calculo de custo. Nulo so em produtos anteriores ao modulo de impressoras.</summary>
    public Guid? PrinterId { get; private set; }

    /// <summary>
    /// Falso = orcamento: so calcula preco, sem descontar estoque nem entrar nos indicadores.
    /// Verdadeiro = produzido: filamento descontado do estoque e contabilizado.
    /// </summary>
    public bool IsProduced { get; private set; }

    public Guid UserId { get; private set; }
    public IReadOnlyCollection<ProductFilament> Filaments => _filaments.AsReadOnly();

    /// <summary>
    /// Quantas impressoes o lote exige (proporcional, sem arredondar). Horas e gramas sao por impressao.
    /// </summary>
    public decimal PrintRuns => (decimal)Quantity / PiecesPerPrint;

    /// <summary>Preco de venda de uma unidade (o <see cref="SalePrice"/> e do lote inteiro).</summary>
    public decimal UnitSalePrice => CalculateUnitSalePrice(SalePrice, Quantity);

    /// <summary>Fonte unica da conta do preco unitario (tambem usada nas projecoes da vitrine).</summary>
    public static decimal CalculateUnitSalePrice(decimal salePrice, int quantity) => salePrice / quantity;

    protected Product()
    {
    }

    public Product(string productName, string? productPhoto, decimal totalHours, int quantity, int piecesPerPrint, Guid printerId, Guid userId)
    {
        Validate(productName, totalHours, quantity, piecesPerPrint);

        ProductName = productName.Trim();
        ProductPhoto = productPhoto;
        TotalHours = totalHours;
        Quantity = quantity;
        PiecesPerPrint = piecesPerPrint;
        PrinterId = printerId;
        UserId = userId;
    }

    public void Update(string productName, string? productPhoto, decimal totalHours, int quantity, int piecesPerPrint, Guid printerId)
    {
        Validate(productName, totalHours, quantity, piecesPerPrint);

        ProductName = productName.Trim();
        ProductPhoto = productPhoto;
        TotalHours = totalHours;
        Quantity = quantity;
        PiecesPerPrint = piecesPerPrint;
        PrinterId = printerId;
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

    public void SetProduced(bool isProduced)
    {
        IsProduced = isProduced;
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

    private static void Validate(string productName, decimal totalHours, int quantity, int piecesPerPrint)
    {
        if (string.IsNullOrWhiteSpace(productName))
            throw new DomainException("Nome do produto e obrigatorio.");

        if (totalHours < 0)
            throw new DomainException("Total de horas nao pode ser negativo.");

        if (quantity < 1)
            throw new DomainException("Quantidade deve ser no minimo 1.");

        if (piecesPerPrint < 1)
            throw new DomainException("Pecas por impressao deve ser no minimo 1.");
    }
}
