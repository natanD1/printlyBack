using Printly.Domain.Enums;
using Printly.Domain.Exceptions;

namespace Printly.Domain.Entities;

/// <summary>
/// Valor de uma regra dentro de um <see cref="Plan"/>. Limite usa <see cref="IntValue"/> (nulo = ilimitado);
/// recurso usa <see cref="BoolValue"/>. Chave primaria composta (PlanId, Key).
/// </summary>
public class PlanEntitlement
{
    public Guid PlanId { get; private set; }
    public PlanEntitlementKey Key { get; private set; }
    public int? IntValue { get; private set; }
    public bool? BoolValue { get; private set; }

    protected PlanEntitlement()
    {
    }

    internal PlanEntitlement(Guid planId, PlanEntitlementKey key, int? intValue, bool? boolValue)
    {
        PlanId = planId;
        Key = key;
        SetValues(intValue, boolValue);
    }

    internal void SetValues(int? intValue, bool? boolValue)
    {
        if (intValue is < 0)
            throw new DomainException("Valor de limite do plano nao pode ser negativo.");

        IntValue = intValue;
        BoolValue = boolValue;
    }
}
