using Printly.Domain.Enums;

namespace Printly.Application.Services;

public enum PlanEntitlementType
{
    /// <summary>Quantidade maxima (IntValue; nulo = ilimitado, 0 = bloqueado).</summary>
    Limit,

    /// <summary>Recurso liga/desliga (BoolValue).</summary>
    Feature
}

/// <summary>
/// Definicao de uma regra de plano. <see cref="Label"/>/<see cref="Description"/> e as unidades de tela
/// (<see cref="UnitSingular"/>, <see cref="UnitPlural"/>, <see cref="ZeroLabel"/>) vao pra tela (pt-BR com acento);
/// os textos <c>Message*</c> montam as mensagens de erro (sem acento, como as demais).
/// </summary>
public sealed record PlanEntitlementDefinition(
    PlanEntitlementKey Key,
    string Label,
    string Description,
    PlanEntitlementType Type,
    string Group,
    string? MessageUnitSingular = null,
    string? MessageUnitPlural = null,
    string? MessageFeature = null,
    string? UnitSingular = null,
    string? UnitPlural = null,
    string? ZeroLabel = null)
{
    public static PlanEntitlementDefinition Limit(
        PlanEntitlementKey key,
        string label,
        string description,
        string group,
        string messageUnitSingular,
        string messageUnitPlural,
        string unitSingular,
        string unitPlural,
        string? zeroLabel = null) =>
        new(key, label, description, PlanEntitlementType.Limit, group, messageUnitSingular, messageUnitPlural,
            UnitSingular: unitSingular, UnitPlural: unitPlural, ZeroLabel: zeroLabel);

    public static PlanEntitlementDefinition Feature(
        PlanEntitlementKey key, string label, string description, string group, string featureMessage) =>
        new(key, label, description, PlanEntitlementType.Feature, group, MessageFeature: featureMessage);
}

/// <summary>
/// Catalogo de regras de plano: fonte unica do que existe. A tela /planos e o GET /plans/me sao montados a partir daqui.
/// Regra nova: acrescentar a chave em <see cref="PlanEntitlementKey"/>, a definicao abaixo e a chamada a
/// EnsureWithinLimitAsync/EnsureFeatureAsync no servico da acao (limite com uso: contador no EntitlementService).
/// </summary>
public static class PlanEntitlementCatalog
{
    public static IReadOnlyList<PlanEntitlementDefinition> All { get; } = new[]
    {
        PlanEntitlementDefinition.Limit(
            PlanEntitlementKey.MaxPrinters,
            "Impressoras",
            "Máximo de impressoras cadastradas pelo usuário.",
            "Cadastros",
            messageUnitSingular: "impressora",
            messageUnitPlural: "impressoras",
            unitSingular: "impressora",
            unitPlural: "impressoras"),
        PlanEntitlementDefinition.Limit(
            PlanEntitlementKey.MaxFilaments,
            "Filamentos",
            "Máximo de filamentos cadastrados pelo usuário.",
            "Cadastros",
            messageUnitSingular: "filamento",
            messageUnitPlural: "filamentos",
            unitSingular: "filamento",
            unitPlural: "filamentos"),
        PlanEntitlementDefinition.Limit(
            PlanEntitlementKey.MaxProductsPerMonth,
            "Produtos por mês",
            "Máximo de produtos criados no mês corrente (orçamentos e produzidos).",
            "Produtos",
            messageUnitSingular: "produto por mes",
            messageUnitPlural: "produtos por mes",
            unitSingular: "produto por mês",
            unitPlural: "produtos por mês"),
        PlanEntitlementDefinition.Limit(
            PlanEntitlementKey.ProductHistoryMonths,
            "Histórico de produtos (meses)",
            "Meses anteriores ao atual que podem ser consultados na tela de produtos.",
            "Produtos",
            messageUnitSingular: "mes anterior",
            messageUnitPlural: "meses anteriores",
            unitSingular: "mês anterior ao atual",
            unitPlural: "meses anteriores ao atual",
            // 0 aqui nao bloqueia: o usuario ainda ve o mes atual.
            zeroLabel: "Só o mês atual"),
        PlanEntitlementDefinition.Feature(
            PlanEntitlementKey.CompanyLogoInQuote,
            "Logo da empresa (orçamento e vitrine)",
            "Permite cadastrar o logo da empresa e usá-lo no PDF do orçamento e na vitrine pública.",
            "Orçamento",
            "usar o logo da empresa no orcamento"),
        PlanEntitlementDefinition.Feature(
            PlanEntitlementKey.RemoveBrandingFooter,
            "Remover marca Printly (orçamento e vitrine)",
            "Gera o PDF do orçamento sem o rodapé do Printly e esconde a marca Printly na vitrine pública.",
            "Orçamento",
            "remover o rodape do Printly no orcamento"),
        PlanEntitlementDefinition.Feature(
            PlanEntitlementKey.OptionalCostsEnabled,
            "Custos opcionais",
            "Permite ligar depreciação, manutenção e perda por falha nas impressoras.",
            "Impressoras",
            "ligar depreciacao, manutencao e perda por falha nas impressoras"),
        PlanEntitlementDefinition.Feature(
            PlanEntitlementKey.ShowcaseEnabled,
            "Vitrine pública",
            "Página pública com os produtos escolhidos, para compartilhar com clientes.",
            "Vitrine",
            "usar a vitrine"),
        PlanEntitlementDefinition.Limit(
            PlanEntitlementKey.MaxShowcaseItems,
            "Produtos na vitrine",
            "Máximo de produtos visíveis na vitrine pública.",
            "Vitrine",
            messageUnitSingular: "item na vitrine",
            messageUnitPlural: "itens na vitrine",
            unitSingular: "item na vitrine",
            unitPlural: "itens na vitrine"),
    };

    private static readonly Dictionary<PlanEntitlementKey, PlanEntitlementDefinition> ByKey = All.ToDictionary(d => d.Key);

    private static readonly Dictionary<string, PlanEntitlementDefinition> ByName =
        All.ToDictionary(d => d.Key.ToString(), StringComparer.Ordinal);

    public static PlanEntitlementDefinition Get(PlanEntitlementKey key) =>
        ByKey.TryGetValue(key, out var definition)
            ? definition
            : throw new InvalidOperationException($"Regra de plano '{key}' sem definicao no catalogo.");

    /// <summary>Busca pelo nome exato da chave (como vem do JSON). Numero ou nome fora do catalogo nao casa.</summary>
    public static bool TryGet(string? key, out PlanEntitlementDefinition definition)
    {
        definition = null!;
        return key is not null && ByName.TryGetValue(key, out definition!);
    }

    /// <summary>"limit"/"feature", como o contrato JSON espera.</summary>
    public static string TypeName(PlanEntitlementType type) => type == PlanEntitlementType.Limit ? "limit" : "feature";
}
