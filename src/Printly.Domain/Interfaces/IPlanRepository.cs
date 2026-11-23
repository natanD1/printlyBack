using Printly.Domain.Entities;

namespace Printly.Domain.Interfaces;

/// <summary>Planos sempre carregados com as regras (<see cref="Plan.Entitlements"/>).</summary>
public interface IPlanRepository
{
    Task<Plan?> GetByIdAsync(Guid id, CancellationToken cancellationToken = default);
    /// <summary>Ordenados por SortOrder e depois Name.</summary>
    Task<List<Plan>> GetAllAsync(CancellationToken cancellationToken = default);
    Task<Plan?> GetDefaultAsync(CancellationToken cancellationToken = default);
    /// <summary>Compara sem diferenciar maiusculas; <paramref name="exceptId"/> ignora o proprio plano na edicao.</summary>
    Task<bool> NameExistsAsync(string name, Guid? exceptId = null, CancellationToken cancellationToken = default);
    Task<int> CountUsersAsync(Guid planId, CancellationToken cancellationToken = default);
    /// <summary>Quantidade de usuarios por plano (planos sem usuario ficam fora do dicionario).</summary>
    Task<Dictionary<Guid, int>> CountUsersByPlanAsync(CancellationToken cancellationToken = default);
    Task AddAsync(Plan plan, CancellationToken cancellationToken = default);
    void Remove(Plan plan);
}
