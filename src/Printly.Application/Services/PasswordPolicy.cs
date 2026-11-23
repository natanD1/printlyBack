using System.Text;
using Printly.Domain.Exceptions;

namespace Printly.Application.Services;

public static class PasswordPolicy
{
    public const int MinLength = 8;
    public const int MaxBytes = 72;

    public static void Validate(string? password)
    {
        password ??= string.Empty;

        if (Encoding.UTF8.GetByteCount(password) > MaxBytes)
            throw new DomainException($"A senha deve ter no maximo {MaxBytes} bytes.");

        var missing = new List<string>();

        if (password.Length < MinLength)
            missing.Add($"ao menos {MinLength} caracteres");
        if (!password.Any(char.IsUpper))
            missing.Add("uma letra maiuscula");
        if (!password.Any(char.IsLower))
            missing.Add("uma letra minuscula");
        if (!password.Any(char.IsDigit))
            missing.Add("um numero");
        if (!password.Any(c => !char.IsLetterOrDigit(c)))
            missing.Add("um caractere especial");

        if (missing.Count == 0)
            return;

        var list = missing.Count == 1
            ? missing[0]
            : string.Join(", ", missing.Take(missing.Count - 1)) + " e " + missing[^1];

        throw new DomainException($"A senha deve ter {list}.");
    }
}
