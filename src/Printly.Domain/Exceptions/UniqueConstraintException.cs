namespace Printly.Domain.Exceptions;

/// <summary>
/// Violacao de indice unico detectada no SaveChanges (a Infrastructure traduz o erro do banco).
/// Nao herda de <see cref="DomainException"/>: cada servico decide a mensagem; sem tratamento vira 500.
/// </summary>
public class UniqueConstraintException : Exception
{
    public UniqueConstraintException(string message, Exception innerException) : base(message, innerException)
    {
    }
}
