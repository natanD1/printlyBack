using Printly.Domain.Exceptions;

namespace Printly.Domain.Entities;

/// <summary>
/// Impressora do usuario (modelo do catalogo + parametros de custo proprios).
/// Os parametros alimentam o calculo de preco dos produtos impressos nela.
/// </summary>
public class Printer : BaseEntity
{
    public Guid PrinterModelId { get; private set; }
    public string? Nickname { get; private set; }
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

    protected Printer()
    {
    }

    public Printer(
        Guid printerModelId,
        string? nickname,
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
        UserId = userId;
        Apply(printerModelId, nickname, averagePowerWatts, defaultProfitMarginPercentage, printerValue,
            printerLifespanHours, maintenancePercentage, failureRatePercentage, depreciationEnabled,
            maintenanceEnabled, failureEnabled);
    }

    public void Update(
        Guid printerModelId,
        string? nickname,
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
        Apply(printerModelId, nickname, averagePowerWatts, defaultProfitMarginPercentage, printerValue,
            printerLifespanHours, maintenancePercentage, failureRatePercentage, depreciationEnabled,
            maintenanceEnabled, failureEnabled);
        MarkAsUpdated();
    }

    private void Apply(
        Guid printerModelId,
        string? nickname,
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

        PrinterModelId = printerModelId;
        Nickname = string.IsNullOrWhiteSpace(nickname) ? null : nickname.Trim();
        AveragePowerWatts = averagePowerWatts;
        DefaultProfitMarginPercentage = defaultProfitMarginPercentage;
        PrinterValue = printerValue;
        PrinterLifespanHours = printerLifespanHours;
        MaintenancePercentage = maintenancePercentage;
        FailureRatePercentage = failureRatePercentage;
        DepreciationEnabled = depreciationEnabled;
        MaintenanceEnabled = maintenanceEnabled;
        FailureEnabled = failureEnabled;
    }
}
