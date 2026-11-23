using Printly.Domain.Enums;
using Printly.Domain.Exceptions;

namespace Printly.Domain.Entities;

/// <summary>
/// Plano parametrizado pelo admin: limites e recursos ficam em <see cref="Entitlements"/>.
/// Exatamente um plano e o padrao (novos usuarios caem nele); a troca do padrao e coordenada pelo PlanService.
/// </summary>
public class Plan : BaseEntity
{
    public const int NameMaxLength = 80;
    public const decimal MaxMonthlyPrice = 99_999_999.99m;
    public const int DescriptionMaxLength = 300;

    private readonly List<PlanEntitlement> _entitlements = new();

    public string Name { get; private set; } = default!;
    public string? Description { get; private set; }
    public decimal MonthlyPrice { get; private set; }
    public bool IsActive { get; private set; }
    public bool IsDefault { get; private set; }

    /// <summary>
    /// Plano oculto nao aparece na lista de planos disponiveis para o usuario (so para quem ja esta nele).
    /// O admin ainda pode atribui-lo manualmente.
    /// </summary>
    public bool IsPublic { get; private set; }

    public int SortOrder { get; private set; }
    public IReadOnlyCollection<PlanEntitlement> Entitlements => _entitlements.AsReadOnly();

    protected Plan()
    {
    }

    public Plan(string name, string? description, decimal monthlyPrice, bool isActive, int sortOrder, bool isPublic = true)
    {
        Apply(name, description, monthlyPrice, isActive, sortOrder, isPublic);
    }

    public void Update(string name, string? description, decimal monthlyPrice, bool isActive, int sortOrder, bool isPublic)
    {
        if (IsDefault && !isActive)
            throw new DomainException("O plano padrao nao pode ser desativado.");

        if (IsDefault && !isPublic)
            throw new DomainException("O plano padrao nao pode ser oculto.");

        Apply(name, description, monthlyPrice, isActive, sortOrder, isPublic);
        MarkAsUpdated();
    }

    /// <summary>Torna este o plano padrao. Quem chama desmarca o padrao anterior no mesmo SaveChanges.</summary>
    public void MarkAsDefault()
    {
        if (!IsActive)
            throw new DomainException("O plano padrao precisa estar ativo.");

        // Novos usuarios caem no padrao: ele precisa aparecer na lista de planos disponiveis.
        if (!IsPublic)
            throw new DomainException("O plano padrao precisa estar visivel.");

        if (IsDefault)
            return;

        IsDefault = true;
        MarkAsUpdated();
    }

    /// <summary>So para a troca de padrao (outro plano foi marcado). Desmarcar direto e bloqueado no servico.</summary>
    public void UnmarkAsDefault()
    {
        if (!IsDefault)
            return;

        IsDefault = false;
        MarkAsUpdated();
    }

    public PlanEntitlement? GetEntitlement(PlanEntitlementKey key) =>
        _entitlements.FirstOrDefault(e => e.Key == key);

    /// <summary>
    /// Substitui o conjunto de regras pelo informado. Atualiza no lugar as que ja existem (evita remover e
    /// recriar a mesma chave composta no mesmo contexto do EF) e remove as que ficaram de fora.
    /// </summary>
    public void SetEntitlements(IEnumerable<(PlanEntitlementKey Key, int? IntValue, bool? BoolValue)> entitlements)
    {
        var items = entitlements.ToList();

        if (items.Select(i => i.Key).Distinct().Count() != items.Count)
            throw new DomainException("Regra de plano duplicada.");

        _entitlements.RemoveAll(e => items.All(i => i.Key != e.Key));

        foreach (var item in items)
        {
            var existing = GetEntitlement(item.Key);

            if (existing is null)
                _entitlements.Add(new PlanEntitlement(Id, item.Key, item.IntValue, item.BoolValue));
            else
                existing.SetValues(item.IntValue, item.BoolValue);
        }

        MarkAsUpdated();
    }

    private void Apply(string name, string? description, decimal monthlyPrice, bool isActive, int sortOrder, bool isPublic)
    {
        if (string.IsNullOrWhiteSpace(name))
            throw new DomainException("Nome do plano e obrigatorio.");

        if (name.Trim().Length > NameMaxLength)
            throw new DomainException($"Nome do plano deve ter no maximo {NameMaxLength} caracteres.");

        var normalizedDescription = string.IsNullOrWhiteSpace(description) ? null : description.Trim();

        if (normalizedDescription is { Length: > DescriptionMaxLength })
            throw new DomainException($"Descricao do plano deve ter no maximo {DescriptionMaxLength} caracteres.");

        if (monthlyPrice < 0)
            throw new DomainException("Preco do plano nao pode ser negativo.");

        // Coluna decimal(10,2): acima disso o banco recusaria com erro 500.
        if (monthlyPrice > MaxMonthlyPrice)
            throw new DomainException("Preco do plano deve ser no maximo 99999999.99.");

        Name = name.Trim();
        Description = normalizedDescription;
        MonthlyPrice = monthlyPrice;
        IsActive = isActive;
        IsPublic = isPublic;
        SortOrder = sortOrder;
    }
}
