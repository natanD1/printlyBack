using PrintFlow.Domain.Exceptions;

namespace PrintFlow.Domain.Entities;

public class UserSettings : BaseEntity
{
    public decimal KwhPrice { get; private set; }
    public decimal AveragePowerWatts { get; private set; }
    public decimal DefaultProfitMarginPercentage { get; private set; }
    public Guid UserId { get; private set; }

    protected UserSettings()
    {
    }

    public UserSettings(decimal kwhPrice, decimal averagePowerWatts, decimal defaultProfitMarginPercentage, Guid userId)
    {
        Validate(kwhPrice, averagePowerWatts, defaultProfitMarginPercentage);

        KwhPrice = kwhPrice;
        AveragePowerWatts = averagePowerWatts;
        DefaultProfitMarginPercentage = defaultProfitMarginPercentage;
        UserId = userId;
    }

    public static UserSettings CreateDefault(Guid userId) =>
        new(kwhPrice: 0.75m, averagePowerWatts: 150m, defaultProfitMarginPercentage: 50m, userId);

    public void Update(decimal kwhPrice, decimal averagePowerWatts, decimal defaultProfitMarginPercentage)
    {
        Validate(kwhPrice, averagePowerWatts, defaultProfitMarginPercentage);

        KwhPrice = kwhPrice;
        AveragePowerWatts = averagePowerWatts;
        DefaultProfitMarginPercentage = defaultProfitMarginPercentage;
        MarkAsUpdated();
    }

    private static void Validate(decimal kwhPrice, decimal averagePowerWatts, decimal defaultProfitMarginPercentage)
    {
        if (kwhPrice < 0)
            throw new DomainException("Preco do kWh nao pode ser negativo.");

        if (averagePowerWatts < 0)
            throw new DomainException("Potencia media nao pode ser negativa.");

        if (defaultProfitMarginPercentage < 0)
            throw new DomainException("Margem de lucro nao pode ser negativa.");
    }
}
