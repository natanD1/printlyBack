using Printly.Application.DTOs.Plans;
using Printly.Domain.Enums;

namespace Printly.Application.Interfaces;

/// <summary>
/// Aplica as regras do plano do usuario. Admin (lido do banco) passa em tudo.
/// Bloqueios lancam PlanRestrictionException (403 com code).
/// </summary>
public interface IEntitlementService
{
    /// <summary>Bloqueia se o uso atual ja chegou ao limite (criar mais um ultrapassaria). Limite nulo/ausente = ilimitado.</summary>
    Task EnsureWithinLimitAsync(Guid userId, PlanEntitlementKey key, CancellationToken cancellationToken = default);

    /// <summary>Bloqueia se o recurso esta desligado (ou ausente) no plano.</summary>
    Task EnsureFeatureAsync(Guid userId, PlanEntitlementKey key, CancellationToken cancellationToken = default);

    Task<bool> IsFeatureEnabledAsync(Guid userId, PlanEntitlementKey key, CancellationToken cancellationToken = default);

    Task<MyPlanDto> GetMyPlanAsync(Guid userId, CancellationToken cancellationToken = default);

    /// <summary>
    /// Inicio do historico de produtos consultavel: dia 1 do mes (atual - N) 00:00 em Sao Paulo, em UTC.
    /// Nulo = sem restricao (admin ou limite ilimitado).
    /// </summary>
    Task<ProductHistoryWindow?> GetProductHistoryWindowAsync(Guid userId, CancellationToken cancellationToken = default);
}
