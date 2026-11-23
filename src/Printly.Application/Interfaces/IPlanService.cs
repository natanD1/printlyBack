using Printly.Application.DTOs.Plans;
using Printly.Application.DTOs.Users;

namespace Printly.Application.Interfaces;

public interface IPlanService
{
    List<EntitlementDefinitionDto> GetCatalog();
    Task<List<PlanDto>> GetAllAsync(CancellationToken cancellationToken = default);
    Task<PlanDto> GetByIdAsync(Guid id, CancellationToken cancellationToken = default);
    Task<PlanDto> CreateAsync(SavePlanRequest request, CancellationToken cancellationToken = default);
    Task<PlanDto> UpdateAsync(Guid id, SavePlanRequest request, CancellationToken cancellationToken = default);
    Task DeleteAsync(Guid id, CancellationToken cancellationToken = default);

    /// <summary>
    /// Planos ativos e publicos, mais o plano atual do usuario mesmo inativo ou oculto (sem duplicar),
    /// ordenados por SortOrder e Name. Aberto a qualquer usuario logado.
    /// </summary>
    Task<AvailablePlansDto> GetAvailablePlansAsync(Guid userId, CancellationToken cancellationToken = default);

    /// <summary>
    /// Unico ponto de troca de plano de um usuario (PATCH /users/{id}/plan e, no futuro, o gateway de pagamento).
    /// Valida plano existente e ativo.
    /// </summary>
    Task<ManagedUserDto> ChangeUserPlanAsync(Guid userId, Guid planId, CancellationToken cancellationToken = default);
}
