using Printly.Domain.Exceptions;

namespace Printly.Domain.Entities;

public class UserSettings : BaseEntity
{
    public decimal KwhPrice { get; private set; }
    public decimal AveragePowerWatts { get; private set; }
    public decimal DefaultProfitMarginPercentage { get; private set; }
    public decimal PrinterValue { get; private set; }
    public decimal PrinterLifespanHours { get; private set; }
    public decimal MaintenancePercentage { get; private set; }
    public decimal FailureRatePercentage { get; private set; }
    public bool DepreciationEnabled { get; private set; }
    public bool MaintenanceEnabled { get; private set; }
    public bool FailureEnabled { get; private set; }
    public Guid UserId { get; private set; }

    protected UserSettings()
    {
    }

    public UserSettings(
        decimal kwhPrice,
        decimal averagePowerWatts,
        decimal defaultProfitMarginPercentage,
        decimal printerValue,
        decimal printerLifespanHours,
        decimal maintenancePercentage,
        decimal failureRatePercentage,
        bool depreciationEnabled,
        bool maintenanceEnabled,
        bool failureEnabled,
        Guid userId)
    {
        Validate(kwhPrice, averagePowerWatts, defaultProfitMarginPercentage, printerValue, printerLifespanHours,
            maintenancePercentage, failureRatePercentage, depreciationEnabled);

        KwhPrice = kwhPrice;
        AveragePowerWatts = averagePowerWatts;
        DefaultProfitMarginPercentage = defaultProfitMarginPercentage;
        PrinterValue = printerValue;
        PrinterLifespanHours = printerLifespanHours;
        MaintenancePercentage = maintenancePercentage;
        FailureRatePercentage = failureRatePercentage;
        DepreciationEnabled = depreciationEnabled;
        MaintenanceEnabled = maintenanceEnabled;
        FailureEnabled = failureEnabled;
        UserId = userId;
    }

    public static UserSettings CreateDefault(Guid userId) =>
        new(
            kwhPrice: 0.75m,
            averagePowerWatts: 150m,
            defaultProfitMarginPercentage: 50m,
            printerValue: 0m,
            printerLifespanHours: 2000m,
            maintenancePercentage: 10m,
            failureRatePercentage: 5m,
            depreciationEnabled: false,
            maintenanceEnabled: false,
            failureEnabled: false,
            userId);

    public void Update(
        decimal kwhPrice,
        decimal averagePowerWatts,
        decimal defaultProfitMarginPercentage,
        decimal printerValue,
        decimal printerLifespanHours,
        decimal maintenancePercentage,
        decimal failureRatePercentage,
        bool depreciationEnabled,
        bool maintenanceEnabled,
        bool failureEnabled)
    {
        Validate(kwhPrice, averagePowerWatts, defaultProfitMarginPercentage, printerValue, printerLifespanHours,
            maintenancePercentage, failureRatePercentage, depreciationEnabled);

        KwhPrice = kwhPrice;
        AveragePowerWatts = averagePowerWatts;
        DefaultProfitMarginPercentage = defaultProfitMarginPercentage;
        PrinterValue = printerValue;
        PrinterLifespanHours = printerLifespanHours;
        MaintenancePercentage = maintenancePercentage;
        FailureRatePercentage = failureRatePercentage;
        DepreciationEnabled = depreciationEnabled;
        MaintenanceEnabled = maintenanceEnabled;
        FailureEnabled = failureEnabled;
        MarkAsUpdated();
    }

    private static void Validate(
        decimal kwhPrice,
        decimal averagePowerWatts,
        decimal defaultProfitMarginPercentage,
        decimal printerValue,
        decimal printerLifespanHours,
        decimal maintenancePercentage,
        decimal failureRatePercentage,
        bool depreciationEnabled)
    {
        if (kwhPrice < 0)
            throw new DomainException("Preco do kWh nao pode ser negativo.");

        if (averagePowerWatts < 0)
            throw new DomainException("Potencia media nao pode ser negativa.");

        if (defaultProfitMarginPercentage < 0)
            throw new DomainException("Margem de lucro nao pode ser negativa.");

        if (printerValue < 0)
            throw new DomainException("Valor da impressora nao pode ser negativo.");

        if (printerLifespanHours < 0)
            throw new DomainException("Vida util da impressora nao pode ser negativa.");

        if (depreciationEnabled && printerLifespanHours <= 0)
            throw new DomainException("Vida util da impressora deve ser maior que zero para calcular a depreciacao.");

        if (maintenancePercentage < 0)
            throw new DomainException("Percentual de manutencao nao pode ser negativo.");

        if (failureRatePercentage is < 0 or > 100)
            throw new DomainException("Taxa de falha deve estar entre 0 e 100.");
    }
}
