using Printly.Application.DTOs.Plans;
using Printly.Application.DTOs.Users;
using Printly.Application.Interfaces;
using Printly.Domain.Entities;
using Printly.Domain.Enums;
using Printly.Domain.Exceptions;
using Printly.Domain.Interfaces;

namespace Printly.Application.Services;

public class PlanService : IPlanService
{
    private readonly IPlanRepository _planRepository;
    private readonly IUserRepository _userRepository;
    private readonly IUnitOfWork _unitOfWork;

    public PlanService(IPlanRepository planRepository, IUserRepository userRepository, IUnitOfWork unitOfWork)
    {
        _planRepository = planRepository;
        _userRepository = userRepository;
        _unitOfWork = unitOfWork;
    }

    public List<EntitlementDefinitionDto> GetCatalog() =>
        PlanEntitlementCatalog.All
            .Select(d => new EntitlementDefinitionDto(
                d.Key.ToString(),
                d.Label,
                d.Description,
                PlanEntitlementCatalog.TypeName(d.Type),
                d.Group,
                d.UnitPlural,
                d.UnitSingular,
                d.ZeroLabel))
            .ToList();

    public async Task<List<PlanDto>> GetAllAsync(CancellationToken cancellationToken = default)
    {
        var plans = await _planRepository.GetAllAsync(cancellationToken);
        var userCounts = await _planRepository.CountUsersByPlanAsync(cancellationToken);

        return plans.Select(p => ToDto(p, userCounts.GetValueOrDefault(p.Id))).ToList();
    }

    public async Task<PlanDto> GetByIdAsync(Guid id, CancellationToken cancellationToken = default)
    {
        var plan = await GetExistingPlanAsync(id, cancellationToken);
        return ToDto(plan, await _planRepository.CountUsersAsync(plan.Id, cancellationToken));
    }

    public async Task<PlanDto> CreateAsync(SavePlanRequest request, CancellationToken cancellationToken = default)
    {
        var entitlements = BuildEntitlements(request.Entitlements);
        await EnsureUniqueNameAsync(request.Name, null, cancellationToken);

        var plan = new Plan(request.Name, request.Description, request.MonthlyPrice, request.IsActive, request.SortOrder, request.IsPublic ?? true);
        plan.SetEntitlements(entitlements);

        if (request.IsDefault)
            await MakeDefaultAsync(plan, cancellationToken);

        await _planRepository.AddAsync(plan, cancellationToken);
        await _unitOfWork.SaveChangesAsync(cancellationToken);

        return ToDto(plan, 0);
    }

    public async Task<PlanDto> UpdateAsync(Guid id, SavePlanRequest request, CancellationToken cancellationToken = default)
    {
        var plan = await GetExistingPlanAsync(id, cancellationToken);
        var entitlements = BuildEntitlements(request.Entitlements);
        await EnsureUniqueNameAsync(request.Name, plan.Id, cancellationToken);

        // O padrao so muda marcando outro plano: desmarcar direto deixaria o cadastro sem plano.
        if (plan.IsDefault && !request.IsDefault)
            throw new DomainException("Para trocar o plano padrao, marque outro plano como padrao.");

        plan.Update(request.Name, request.Description, request.MonthlyPrice, request.IsActive, request.SortOrder, request.IsPublic ?? plan.IsPublic);
        plan.SetEntitlements(entitlements);

        if (request.IsDefault)
            await MakeDefaultAsync(plan, cancellationToken);

        await _unitOfWork.SaveChangesAsync(cancellationToken);

        return ToDto(plan, await _planRepository.CountUsersAsync(plan.Id, cancellationToken));
    }

    public async Task DeleteAsync(Guid id, CancellationToken cancellationToken = default)
    {
        var plan = await GetExistingPlanAsync(id, cancellationToken);

        if (plan.IsDefault)
            throw new DomainException("O plano padrao nao pode ser excluido.");

        if (await _planRepository.CountUsersAsync(plan.Id, cancellationToken) > 0)
            throw new DomainException("Nao e possivel excluir um plano com usuarios. Desative o plano ou mova os usuarios para outro plano.");

        _planRepository.Remove(plan);
        await _unitOfWork.SaveChangesAsync(cancellationToken);
    }

    public async Task<AvailablePlansDto> GetAvailablePlansAsync(Guid userId, CancellationToken cancellationToken = default)
    {
        var user = await _userRepository.GetByIdAsync(userId, cancellationToken)
            ?? throw new DomainException("Usuario nao encontrado.");

        // GetAllAsync ja vem ordenado por SortOrder e Name. Um unico filtro sobre a lista evita duplicar o plano atual.
        var plans = (await _planRepository.GetAllAsync(cancellationToken))
            .Where(p => (p.IsActive && p.IsPublic) || p.Id == user.PlanId)
            .Select(p => new AvailablePlanDto(
                p.Id,
                p.Name,
                p.Description,
                p.MonthlyPrice,
                p.SortOrder,
                p.Id == user.PlanId,
                ToEntitlementDtos(p)))
            .ToList();

        return new AvailablePlansDto(GetCatalog(), plans);
    }

    public async Task<ManagedUserDto> ChangeUserPlanAsync(Guid userId, Guid planId, CancellationToken cancellationToken = default)
    {
        var user = await _userRepository.GetByIdAsync(userId, cancellationToken)
            ?? throw new DomainException("Usuario nao encontrado.");

        var plan = await _planRepository.GetByIdAsync(planId, cancellationToken)
            ?? throw new DomainException("Plano nao encontrado.");

        if (!plan.IsActive)
            throw new DomainException("Nao e possivel atribuir um plano inativo.");

        user.ChangePlan(plan.Id);
        await _unitOfWork.SaveChangesAsync(cancellationToken);

        return UserManagementService.ToDto(user, plan.Name);
    }

    /// <summary>Marca o plano como padrao e desmarca o anterior (gravados juntos no mesmo SaveChanges).</summary>
    private async Task MakeDefaultAsync(Plan plan, CancellationToken cancellationToken)
    {
        if (plan.IsDefault)
            return;

        plan.MarkAsDefault();

        var previous = await _planRepository.GetDefaultAsync(cancellationToken);

        if (previous is not null && previous.Id != plan.Id)
            previous.UnmarkAsDefault();
    }

    private async Task EnsureUniqueNameAsync(string name, Guid? exceptId, CancellationToken cancellationToken)
    {
        if (string.IsNullOrWhiteSpace(name))
            throw new DomainException("Nome do plano e obrigatorio.");

        if (await _planRepository.NameExistsAsync(name.Trim(), exceptId, cancellationToken))
            throw new DomainException("Ja existe um plano com este nome.");
    }

    /// <summary>
    /// Valida as regras do request contra o catalogo. Limite guarda so IntValue; recurso guarda so BoolValue
    /// (nulo vira desligado).
    /// </summary>
    private static List<(PlanEntitlementKey Key, int? IntValue, bool? BoolValue)> BuildEntitlements(List<PlanEntitlementDto>? entitlements)
    {
        var result = new List<(PlanEntitlementKey, int?, bool?)>();

        foreach (var item in entitlements ?? new List<PlanEntitlementDto>())
        {
            if (item is null || !PlanEntitlementCatalog.TryGet(item.Key, out var definition))
                throw new DomainException("Regra de plano invalida.");

            if (definition.Type == PlanEntitlementType.Limit)
            {
                if (item.IntValue is < 0)
                    throw new DomainException($"Valor de \"{definition.Key}\" nao pode ser negativo.");

                result.Add((definition.Key, item.IntValue, null));
            }
            else
            {
                result.Add((definition.Key, null, item.BoolValue ?? false));
            }
        }

        return result;
    }

    private async Task<Plan> GetExistingPlanAsync(Guid id, CancellationToken cancellationToken) =>
        await _planRepository.GetByIdAsync(id, cancellationToken)
            ?? throw new DomainException("Plano nao encontrado.");

    private static PlanDto ToDto(Plan plan, int userCount) => new(
        plan.Id,
        plan.Name,
        plan.Description,
        plan.MonthlyPrice,
        plan.IsActive,
        plan.IsDefault,
        plan.IsPublic,
        plan.SortOrder,
        userCount,
        ToEntitlementDtos(plan));

    private static List<PlanEntitlementDto> ToEntitlementDtos(Plan plan) =>
        plan.Entitlements
            .OrderBy(e => e.Key)
            .Select(e => new PlanEntitlementDto(e.Key.ToString(), e.IntValue, e.BoolValue))
            .ToList();
}
