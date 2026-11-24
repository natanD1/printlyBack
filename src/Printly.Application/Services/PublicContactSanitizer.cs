using System.Net.Mail;
using System.Text.RegularExpressions;

namespace Printly.Application.Services;

/// <summary>
/// Limpa os contatos da empresa antes de irem pra pagina publica. Tudo que nao passa vira nulo
/// (nada de <c>javascript:</c>/<c>data:</c> em link, nem texto livre em campo de numero).
/// </summary>
public static class PublicContactSanitizer
{
    private static readonly TimeSpan RegexTimeout = TimeSpan.FromMilliseconds(100);
    private static readonly Regex InstagramHandle = new(@"^[A-Za-z0-9._]{1,30}\z", RegexOptions.CultureInvariant, RegexTimeout);
    private static readonly Regex PhoneChars = new(@"^[0-9+()\- ]{8,30}\z", RegexOptions.CultureInvariant, RegexTimeout);

    /// <summary>Mesma regra do <c>normalizePhone</c> do front: 10/11 digitos ganham 55; 12/13 com 55 passam.</summary>
    public static string? WhatsApp(string? value)
    {
        if (string.IsNullOrWhiteSpace(value))
            return null;

        var digits = new string(value.Where(char.IsAsciiDigit).ToArray());

        return digits.Length switch
        {
            10 or 11 => "55" + digits,
            12 or 13 when digits.StartsWith("55", StringComparison.Ordinal) => digits,
            _ => null
        };
    }

    /// <summary>Aceita <c>@handle</c>, <c>handle</c> ou URL do instagram.com; devolve so o handle.</summary>
    public static string? Instagram(string? value)
    {
        if (string.IsNullOrWhiteSpace(value))
            return null;

        var input = value.Trim();
        string handle;

        if (input.StartsWith('@'))
        {
            handle = input[1..];
        }
        else if (input.Contains("instagram.com", StringComparison.OrdinalIgnoreCase))
        {
            var withScheme = input.Contains("://", StringComparison.Ordinal) ? input : "https://" + input;

            if (!Uri.TryCreate(withScheme, UriKind.Absolute, out var uri)
                || (uri.Scheme != Uri.UriSchemeHttp && uri.Scheme != Uri.UriSchemeHttps))
                return null;

            var host = uri.Host.ToLowerInvariant();

            if (host != "instagram.com" && !host.EndsWith(".instagram.com", StringComparison.Ordinal))
                return null;

            handle = uri.AbsolutePath.Split('/', StringSplitOptions.RemoveEmptyEntries).FirstOrDefault() ?? string.Empty;
        }
        else
        {
            handle = input;
        }

        return InstagramHandle.IsMatch(handle) ? handle : null;
    }

    /// <summary>So http/https com host. Sem esquema, tenta https://. Devolve a URL normalizada.</summary>
    public static string? Website(string? value)
    {
        if (string.IsNullOrWhiteSpace(value))
            return null;

        var input = value.Trim();

        if (Uri.TryCreate(input, UriKind.Absolute, out var uri))
            return IsHttpWithHost(uri) ? uri.AbsoluteUri : null;

        return Uri.TryCreate("https://" + input, UriKind.Absolute, out uri) && IsHttpWithHost(uri)
            ? uri.AbsoluteUri
            : null;
    }

    public static string? Email(string? value)
    {
        if (string.IsNullOrWhiteSpace(value))
            return null;

        var input = value.Trim();

        if (input.Any(char.IsWhiteSpace))
            return null;

        return MailAddress.TryCreate(input, out var address) && address.Address == input ? input : null;
    }

    /// <summary>Telefone de exibicao: so digitos e <c>+()- </c>, 8 a 30 caracteres, ao menos 8 digitos.</summary>
    public static string? Phone(string? value)
    {
        if (string.IsNullOrWhiteSpace(value))
            return null;

        var input = value.Trim();

        return PhoneChars.IsMatch(input) && input.Count(char.IsAsciiDigit) >= 8 ? input : null;
    }

    private static bool IsHttpWithHost(Uri uri) =>
        (uri.Scheme == Uri.UriSchemeHttp || uri.Scheme == Uri.UriSchemeHttps) && !string.IsNullOrEmpty(uri.Host);
}
