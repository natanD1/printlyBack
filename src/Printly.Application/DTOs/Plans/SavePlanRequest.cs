namespace Printly.Application.DTOs.Plans;

/// <summary>
/// O PUT substitui o conjunto de regras do plano pelo enviado. <see cref="IsPublic"/> ausente no JSON (nulo):
/// no POST o plano nasce publico; no PUT mantem a visibilidade atual (cliente anterior ao campo nao revela
/// um plano oculto sem querer).
/// </summary>
public record SavePlanRequest(
    string Name,
    string? Description,
    decimal MonthlyPrice,
    bool IsActive,
    bool IsDefault,
    int SortOrder,
    List<PlanEntitlementDto>? Entitlements,
    bool? IsPublic = null);
