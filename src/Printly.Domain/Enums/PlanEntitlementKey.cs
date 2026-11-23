namespace Printly.Domain.Enums;

/// <summary>
/// Regras parametrizaveis de um plano. Gravado como string no banco: nunca renomear um valor existente.
/// Regra nova: acrescentar aqui e a definicao em <c>PlanEntitlementCatalog</c> (Application).
/// </summary>
public enum PlanEntitlementKey
{
    MaxPrinters,
    MaxFilaments,
    MaxProductsPerMonth,
    ProductHistoryMonths,
    CompanyLogoInQuote,
    RemoveBrandingFooter,
    OptionalCostsEnabled
}
