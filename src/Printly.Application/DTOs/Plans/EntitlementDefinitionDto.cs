namespace Printly.Application.DTOs.Plans;

/// <summary>
/// Item do catalogo de regras. <see cref="Type"/> e "limit" ou "feature".
/// <see cref="Unit"/> (plural), <see cref="UnitSingular"/> e <see cref="ZeroLabel"/> sao textos de tela dos limites;
/// nulos nos recursos. <see cref="ZeroLabel"/> substitui o "0 + unidade" quando zero tem sentido proprio.
/// </summary>
public record EntitlementDefinitionDto(
    string Key,
    string Label,
    string Description,
    string Type,
    string Group,
    string? Unit,
    string? UnitSingular,
    string? ZeroLabel);
