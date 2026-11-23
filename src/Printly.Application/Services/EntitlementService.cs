using Printly.Application.DTOs.Plans;
using Printly.Application.Interfaces;
using Printly.Domain.Entities;
using Printly.Domain.Enums;
using Printly.Domain.Exceptions;
using Printly.Domain.Interfaces;

namespace Printly.Application.Services;

public class EntitlementService : IEntitlementService
{
    private readonly IUserRepository _userRepository;
    private readonly IPlanRepository _planRepository;
    private readonly IPrinterRepository _printerRepository;
    private readonly IFilamentRepository _filamentRepository;
    private readonly IProductRepository _productRepository;
    private readonly TimeProvider _timeProvider;

    public EntitlementService(
        IUserRepository userRepository,
        IPlanRepository planRepository,
        IPrinterRepository printerRepository,
        IFilamentRepository filamentRepository,
        IProductRepository productRepository,
        TimeProvider timeProvider)
    {
        _userRepository = userRepository;
        _planRepository = planRepository;
        _printerRepository = printerRepository;
        _filamentRepository = filamentRepository;
        _productRepository = productRepository;
        _timeProvider = timeProvider;
    }

    public async Task EnsureWithinLimitAsync(Guid userId, PlanEntitlementKey key, CancellationToken cancellationToken = default)
    {
        var definition = PlanEntitlementCatalog.Get(key);

        if (definition.Type != PlanEntitlementType.Limit)
            throw new InvalidOperationException($"Regra de plano '{key}' nao e um limite.");

        var (user, plan) = await LoadAsync(userId, cancellationToken);

        if (user.IsAdmin)
            return;

        if (GetLimit(plan, key) is not { } limit)
            return;

        var used = await CountUsageAsync(userId, key, cancellationToken)
            ?? throw new InvalidOperationException($"Regra de plano '{key}' nao tem contagem de uso.");

        if (used < limit)
            return;

        var message = limit == 0
            ? $"Seu plano {plan.Name} nao permite cadastrar {definition.MessageUnitPlural}. Faca upgrade para liberar."
            : $"Seu plano {plan.Name} permite ate {limit} {(limit == 1 ? definition.MessageUnitSingular : definition.MessageUnitPlural)}. Faca upgrade para cadastrar mais.";

        throw new PlanRestrictionException(PlanRestrictionException.PlanLimitReached, message);
    }

    public async Task EnsureFeatureAsync(Guid userId, PlanEntitlementKey key, CancellationToken cancellationToken = default)
    {
        var definition = PlanEntitlementCatalog.Get(key);
        var (user, plan) = await LoadAsync(userId, cancellationToken);

        if (IsFeatureEnabled(user, plan, definition))
            return;

        throw new PlanRestrictionException(
            PlanRestrictionException.PlanFeatureUnavailable,
            $"Seu plano {plan.Name} nao permite {definition.MessageFeature}. Faca upgrade para liberar.");
    }

    public async Task<bool> IsFeatureEnabledAsync(Guid userId, PlanEntitlementKey key, CancellationToken cancellationToken = default)
    {
        var definition = PlanEntitlementCatalog.Get(key);
        var (user, plan) = await LoadAsync(userId, cancellationToken);

        return IsFeatureEnabled(user, plan, definition);
    }

    public async Task<MyPlanDto> GetMyPlanAsync(Guid userId, CancellationToken cancellationToken = default)
    {
        var (user, plan) = await LoadAsync(userId, cancellationToken);
        var entitlements = new List<MyPlanEntitlementDto>();

        foreach (var definition in PlanEntitlementCatalog.All)
        {
            var key = definition.Key.ToString();
            var type = PlanEntitlementCatalog.TypeName(definition.Type);

            if (definition.Type == PlanEntitlementType.Limit)
            {
                var limit = user.IsAdmin ? null : GetLimit(plan, definition.Key);
                var used = await CountUsageAsync(userId, definition.Key, cancellationToken);
                entitlements.Add(new MyPlanEntitlementDto(key, definition.Label, type, limit, used, null));
            }
            else
            {
                entitlements.Add(new MyPlanEntitlementDto(key, definition.Label, type, null, null, IsFeatureEnabled(user, plan, definition)));
            }
        }

        return new MyPlanDto(plan.Id, plan.Name, user.IsAdmin, entitlements);
    }

    public async Task<ProductHistoryWindow?> GetProductHistoryWindowAsync(Guid userId, CancellationToken cancellationToken = default)
    {
        var (user, plan) = await LoadAsync(userId, cancellationToken);

        if (user.IsAdmin || GetLimit(plan, PlanEntitlementKey.ProductHistoryMonths) is not { } months)
            return null;

        // O front escolhe o mes pelo calendario do navegador. Na virada do mes, quem esta ate 2h atras de
        // Sao Paulo (Acre, UTC-5) ainda ve o mes anterior: o mes de referencia segue esse relogio mais
        // atrasado para nao devolver 403 no "Este mes". Os limites do mes continuam em Sao Paulo.
        var reference = _timeProvider.GetUtcNow() - HistoryClientClockGrace;

        return new ProductHistoryWindow(BrazilTime.MonthStartUtc(reference, -months), months);
    }

    private static readonly TimeSpan HistoryClientClockGrace = TimeSpan.FromHours(2);

    /// <summary>
    /// Uso atual de um limite, sempre do proprio usuario. Nulo = regra sem contagem (ex.: historico).
    /// Limite novo com contagem: acrescentar o contador aqui.
    /// </summary>
    private async Task<int?> CountUsageAsync(Guid userId, PlanEntitlementKey key, CancellationToken cancellationToken)
    {
        switch (key)
        {
            case PlanEntitlementKey.MaxPrinters:
                return await _printerRepository.CountByUserIdAsync(userId, cancellationToken);
            case PlanEntitlementKey.MaxFilaments:
                return await _filamentRepository.CountByUserIdAsync(userId, cancellationToken);
            case PlanEntitlementKey.MaxProductsPerMonth:
                var now = _timeProvider.GetUtcNow();
                return await _productRepository.CountCreatedByUserIdAsync(
                    userId, BrazilTime.MonthStartUtc(now), BrazilTime.MonthStartUtc(now, 1), cancellationToken);
            default:
                return null;
        }
    }

    /// <summary>Linha ausente no plano = ilimitado.</summary>
    private static int? GetLimit(Plan plan, PlanEntitlementKey key) => plan.GetEntitlement(key)?.IntValue;

    /// <summary>Linha ausente no plano = desligado.</summary>
    private static bool IsFeatureEnabled(User user, Plan plan, PlanEntitlementDefinition definition)
    {
        if (definition.Type != PlanEntitlementType.Feature)
            throw new InvalidOperationException($"Regra de plano '{definition.Key}' nao e um recurso.");

        return user.IsAdmin || plan.GetEntitlement(definition.Key)?.BoolValue == true;
    }

    private async Task<(User User, Plan Plan)> LoadAsync(Guid userId, CancellationToken cancellationToken)
    {
        var user = await _userRepository.GetByIdAsync(userId, cancellationToken)
            ?? throw new DomainException("Usuario nao encontrado.");

        var plan = await _planRepository.GetByIdAsync(user.PlanId, cancellationToken)
            ?? throw new DomainException("Plano do usuario nao encontrado.");

        return (user, plan);
    }
}
