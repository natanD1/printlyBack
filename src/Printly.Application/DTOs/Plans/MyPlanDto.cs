namespace Printly.Application.DTOs.Plans;

/// <summary>Plano do usuario logado com uma entrada para cada item do catalogo.</summary>
public record MyPlanDto(Guid PlanId, string PlanName, bool IsAdmin, List<MyPlanEntitlementDto> Entitlements);

/// <summary>
/// Limite: <see cref="Limit"/> nulo = ilimitado; <see cref="Used"/> = uso atual (nulo quando a regra nao tem contagem).
/// Recurso: so <see cref="Enabled"/>.
/// </summary>
public record MyPlanEntitlementDto(string Key, string Label, string Type, int? Limit, int? Used, bool? Enabled);
