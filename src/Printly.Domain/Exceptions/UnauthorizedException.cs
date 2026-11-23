namespace Printly.Domain.Exceptions;

/// <summary>Sessao/credencial invalida: o middleware responde 401 (e nao 400 como o <see cref="DomainException"/>).</summary>
public class UnauthorizedException : Exception
{
    public UnauthorizedException(string message) : base(message)
    {
    }
}
