namespace Printly.Domain.Exceptions;

/// <summary>
/// Acao bloqueada pelo plano do usuario: o middleware responde 403 com <c>{ message, code }</c>
/// (e nao 400 como o <see cref="DomainException"/>), pra o front oferecer upgrade.
/// </summary>
public class PlanRestrictionException : Exception
{
    public const string PlanLimitReached = "PLAN_LIMIT_REACHED";
    public const string PlanFeatureUnavailable = "PLAN_FEATURE_UNAVAILABLE";

    public string Code { get; }

    public PlanRestrictionException(string code, string message) : base(message)
    {
        Code = code;
    }
}
