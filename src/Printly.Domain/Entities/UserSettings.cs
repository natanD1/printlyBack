using Printly.Domain.Exceptions;

namespace Printly.Domain.Entities;

/// <summary>
/// Parametros globais do usuario. Os custos por impressora (watts, desgaste, margem) ficam em <see cref="Printer"/>.
/// </summary>
public class UserSettings : BaseEntity
{
    public decimal KwhPrice { get; private set; }
    public Guid UserId { get; private set; }

    protected UserSettings()
    {
    }

    public UserSettings(decimal kwhPrice, Guid userId)
    {
        Validate(kwhPrice);

        KwhPrice = kwhPrice;
        UserId = userId;
    }

    public static UserSettings CreateDefault(Guid userId) => new(kwhPrice: 0.75m, userId);

    public void Update(decimal kwhPrice)
    {
        Validate(kwhPrice);

        KwhPrice = kwhPrice;
        MarkAsUpdated();
    }

    private static void Validate(decimal kwhPrice)
    {
        if (kwhPrice < 0)
            throw new DomainException("Preco do kWh nao pode ser negativo.");
    }
}
