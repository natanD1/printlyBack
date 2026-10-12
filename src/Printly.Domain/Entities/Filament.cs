using System.Text.RegularExpressions;
using Printly.Domain.Exceptions;

namespace Printly.Domain.Entities;

public partial class Filament : BaseEntity
{
    public string Name { get; private set; } = default!;
    public Guid BrandId { get; private set; }
    public Guid TypeId { get; private set; }
    public string ColorHex { get; private set; } = default!;
    public decimal Weight { get; private set; }
    public decimal InitialWeight { get; private set; }
    public decimal FilamentPrice { get; private set; }
    public Guid UserId { get; private set; }

    protected Filament()
    {
    }

    public Filament(string name, Guid brandId, Guid typeId, string colorHex, decimal weight, decimal filamentPrice, Guid userId)
    {
        Validate(name, colorHex, weight, filamentPrice);

        Name = name.Trim();
        BrandId = brandId;
        TypeId = typeId;
        ColorHex = colorHex.Trim();
        Weight = weight;
        InitialWeight = weight;
        FilamentPrice = filamentPrice;
        UserId = userId;
    }

    public void Update(string name, Guid brandId, Guid typeId, string colorHex, decimal weight, decimal filamentPrice)
    {
        Validate(name, colorHex, weight, filamentPrice);

        Name = name.Trim();
        BrandId = brandId;
        TypeId = typeId;
        ColorHex = colorHex.Trim();
        Weight = weight;
        FilamentPrice = filamentPrice;
        MarkAsUpdated();
    }

    public void DeductStock(decimal kilograms)
    {
        if (kilograms < 0)
            throw new DomainException("Quantidade a deduzir nao pode ser negativa.");

        if (Weight < kilograms)
            throw new DomainException($"Estoque insuficiente de '{Name}': disponivel {Weight * 1000}g.");

        Weight -= kilograms;
        MarkAsUpdated();
    }

    public void RestoreStock(decimal kilograms)
    {
        if (kilograms < 0)
            throw new DomainException("Quantidade a devolver nao pode ser negativa.");

        Weight += kilograms;
        MarkAsUpdated();
    }

    private static void Validate(string name, string colorHex, decimal weight, decimal filamentPrice)
    {
        if (string.IsNullOrWhiteSpace(name))
            throw new DomainException("Nome do filamento e obrigatorio.");

        if (string.IsNullOrWhiteSpace(colorHex) || !ColorHexRegex().IsMatch(colorHex.Trim()))
            throw new DomainException("Cor do filamento deve estar no formato hexadecimal (ex: #FFAA00).");

        if (weight < 0)
            throw new DomainException("Peso do filamento nao pode ser negativo.");

        if (filamentPrice < 0)
            throw new DomainException("Preco do filamento nao pode ser negativo.");
    }

    [GeneratedRegex("^#([0-9A-Fa-f]{6}|[0-9A-Fa-f]{3})$")]
    private static partial Regex ColorHexRegex();
}
