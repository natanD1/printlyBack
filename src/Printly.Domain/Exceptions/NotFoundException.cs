namespace Printly.Domain.Exceptions;

/// <summary>Recurso inexistente (ou de outro usuario, sem distinguir): o middleware responde 404 com <c>{ message }</c>.</summary>
public class NotFoundException : Exception
{
    public NotFoundException(string message) : base(message)
    {
    }
}
