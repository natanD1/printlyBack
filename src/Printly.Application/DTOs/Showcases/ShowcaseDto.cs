namespace Printly.Application.DTOs.Showcases;

/// <summary>
/// Resposta do GET /showcase. Dois formatos (serializados pelo tipo concreto): <see cref="NoShowcaseDto"/>
/// (so <c>exists</c> e <c>planAllows</c>) e <see cref="ShowcaseDto"/>.
/// </summary>
public abstract record ShowcaseStateDto(bool Exists, bool PlanAllows);

/// <summary>Usuario ainda sem vitrine: serializa exatamente <c>{ "exists": false, "planAllows": ... }</c>.</summary>
public sealed record NoShowcaseDto(bool PlanAllows) : ShowcaseStateDto(false, PlanAllows);

/// <summary>
/// Vitrine do dono. <see cref="Limit"/> nulo = ilimitado; <see cref="Used"/> = mesmo contador do plano (visivel + produzido).
/// </summary>
public sealed record ShowcaseDto(
    bool PlanAllows,
    bool IsPublished,
    bool IsBlocked,
    string Slug,
    int? Limit,
    int Used,
    List<ShowcaseItemDto> Items) : ShowcaseStateDto(true, PlanAllows);

/// <summary><see cref="OutsidePlan"/>: visivel e produzido, mas alem do limite atual (nao aparece no publico).</summary>
public record ShowcaseItemDto(
    Guid ProductId,
    string Name,
    decimal UnitSalePrice,
    bool HasPhoto,
    int SortOrder,
    bool IsVisible,
    bool ProductIsProduced,
    bool OutsidePlan);
