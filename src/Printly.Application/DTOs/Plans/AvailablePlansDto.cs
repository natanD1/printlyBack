namespace Printly.Application.DTOs.Plans;

/// <summary>
/// GET /plans/available: planos que o usuario pode ver para comparar. <see cref="Rules"/> segue a ordem do catalogo.
/// </summary>
public record AvailablePlansDto(List<EntitlementDefinitionDto> Rules, List<AvailablePlanDto> Plans);

/// <summary>Plano visto pelo usuario: sem contagem de usuarios nem flags de administracao (padrao/ativo/publico).</summary>
public record AvailablePlanDto(
    Guid Id,
    string Name,
    string? Description,
    decimal MonthlyPrice,
    int SortOrder,
    bool IsCurrent,
    List<PlanEntitlementDto> Entitlements);
