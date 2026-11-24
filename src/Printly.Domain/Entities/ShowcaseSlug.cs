using System.Globalization;
using System.Text;
using System.Text.RegularExpressions;

namespace Printly.Domain.Entities;

/// <summary>
/// Endereco publico da vitrine (<c>/loja/{slug}</c>): minusculo, letras/numeros separados por hifen unico, 3 a 60 caracteres.
/// </summary>
public static class ShowcaseSlug
{
    public const int MinLength = 3;
    public const int MaxLength = 60;

    /// <summary>Tamanho maximo do slug gerado a partir do nome (sobra espaco pro sufixo de colisao).</summary>
    public const int GeneratedMaxLength = 40;

    public const string Pattern = "^[a-z0-9]+(-[a-z0-9]+)*$";
    public const string Fallback = "loja";

    public const string InvalidMessage = "Endereco invalido: use de 3 a 60 letras minusculas, numeros e hifens.";
    public const string ReservedMessage = "Esse endereco e reservado. Escolha outro.";
    public const string InUseMessage = "Esse endereço já está em uso.";

    public static IReadOnlySet<string> Reserved { get; } = new HashSet<string>(StringComparer.Ordinal)
    {
        "admin", "api", "auth", "loja", "www", "printly", "suporte", "ajuda"
    };

    // \z (e nao $) pra nao aceitar quebra de linha no fim. Sem IgnoreCase: maiuscula e invalida.
    private static readonly Regex SlugRegex = new(
        @"^[a-z0-9]+(-[a-z0-9]+)*\z",
        RegexOptions.CultureInvariant,
        TimeSpan.FromMilliseconds(100));

    public static bool IsValid(string? slug) =>
        slug is not null
        && slug.Length is >= MinLength and <= MaxLength
        && SlugRegex.IsMatch(slug);

    public static bool IsReserved(string slug) => Reserved.Contains(slug);

    /// <summary>
    /// Slug base a partir do nome da empresa: remove acentos, minusculo, troca o resto por hifen, colapsa e apara,
    /// corta em <see cref="GeneratedMaxLength"/>. Menos de 3 caracteres vira <see cref="Fallback"/>.
    /// Pode sair reservado: quem chama resolve com <see cref="WithSuffix"/>.
    /// </summary>
    public static string Generate(string? name)
    {
        var decomposed = (name ?? string.Empty).Normalize(NormalizationForm.FormD);
        var builder = new StringBuilder(decomposed.Length);

        foreach (var c in decomposed)
        {
            if (CharUnicodeInfo.GetUnicodeCategory(c) == UnicodeCategory.NonSpacingMark)
                continue;

            var lower = char.ToLowerInvariant(c);
            var allowed = lower is >= 'a' and <= 'z' or >= '0' and <= '9';

            if (allowed)
                builder.Append(lower);
            else if (builder.Length > 0 && builder[^1] != '-')
                builder.Append('-');
        }

        var slug = builder.ToString().Trim('-');

        if (slug.Length > GeneratedMaxLength)
            slug = slug[..GeneratedMaxLength].TrimEnd('-');

        return slug.Length < MinLength ? Fallback : slug;
    }

    /// <summary>Candidato de colisao: <c>base-2</c>, <c>base-3</c>...</summary>
    public static string WithSuffix(string baseSlug, int number) =>
        number <= 1 ? baseSlug : $"{baseSlug}-{number}";
}
