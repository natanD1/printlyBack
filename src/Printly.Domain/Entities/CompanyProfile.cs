using Printly.Domain.Exceptions;

namespace Printly.Domain.Entities;

/// <summary>
/// Perfil da empresa do usuario (1:1 com <see cref="User"/>): nome, logo e contatos usados no orcamento.
/// </summary>
public class CompanyProfile : BaseEntity
{
    public const int MaxLogoBytes = 512_000;

    public Guid UserId { get; private set; }
    public string Name { get; private set; } = default!;
    public string? LogoBase64 { get; private set; }
    public string? Phone { get; private set; }
    public string? Email { get; private set; }
    public string? WhatsApp { get; private set; }
    public string? Instagram { get; private set; }
    public string? Website { get; private set; }

    protected CompanyProfile()
    {
    }

    public CompanyProfile(
        Guid userId,
        string name,
        string? logoBase64,
        string? phone,
        string? email,
        string? whatsApp,
        string? instagram,
        string? website)
    {
        var normalizedLogo = NormalizeOptional(logoBase64);

        Validate(name, normalizedLogo, phone, email, whatsApp, instagram, website);

        UserId = userId;
        Name = name.Trim();
        LogoBase64 = normalizedLogo;
        Phone = NormalizeOptional(phone);
        Email = NormalizeOptional(email);
        WhatsApp = NormalizeOptional(whatsApp);
        Instagram = NormalizeOptional(instagram);
        Website = NormalizeOptional(website);
    }

    /// <summary>Logo enviado substitui; senao <paramref name="removeLogo"/> apaga; senao mantem o atual.</summary>
    public void Update(
        string name,
        string? logoBase64,
        bool removeLogo,
        string? phone,
        string? email,
        string? whatsApp,
        string? instagram,
        string? website)
    {
        var normalizedLogo = NormalizeOptional(logoBase64);

        Validate(name, normalizedLogo, phone, email, whatsApp, instagram, website);

        Name = name.Trim();

        if (normalizedLogo is not null)
            LogoBase64 = normalizedLogo;
        else if (removeLogo)
            LogoBase64 = null;

        Phone = NormalizeOptional(phone);
        Email = NormalizeOptional(email);
        WhatsApp = NormalizeOptional(whatsApp);
        Instagram = NormalizeOptional(instagram);
        Website = NormalizeOptional(website);
        MarkAsUpdated();
    }

    /// <summary>Valida o logo em base64: decodifica, limita a 500 KB e confere PNG/JPG pelos magic bytes.</summary>
    public static void ValidateLogo(string logoBase64)
    {
        var buffer = new byte[(logoBase64.Length * 3 / 4) + 3];

        if (!Convert.TryFromBase64String(logoBase64, buffer, out var bytesWritten))
            throw new DomainException("Logo invalido.");

        if (bytesWritten > MaxLogoBytes)
            throw new DomainException("Logo deve ter no maximo 500 KB.");

        var isPng = bytesWritten >= 4
            && buffer[0] == 0x89 && buffer[1] == 0x50 && buffer[2] == 0x4E && buffer[3] == 0x47;
        var isJpg = bytesWritten >= 3
            && buffer[0] == 0xFF && buffer[1] == 0xD8 && buffer[2] == 0xFF;

        if (!isPng && !isJpg)
            throw new DomainException("Logo deve ser PNG ou JPG.");
    }

    private static void Validate(
        string name,
        string? logoBase64,
        string? phone,
        string? email,
        string? whatsApp,
        string? instagram,
        string? website)
    {
        if (string.IsNullOrWhiteSpace(name))
            throw new DomainException("Nome da empresa e obrigatorio.");

        if (name.Trim().Length > 120)
            throw new DomainException("Nome da empresa deve ter no maximo 120 caracteres.");

        EnsureMaxLength(phone, 30, "Telefone");
        EnsureMaxLength(email, 200, "E-mail");
        EnsureMaxLength(whatsApp, 30, "WhatsApp");
        EnsureMaxLength(instagram, 100, "Instagram");
        EnsureMaxLength(website, 200, "Site");

        if (logoBase64 is not null)
            ValidateLogo(logoBase64);
    }

    private static void EnsureMaxLength(string? value, int max, string field)
    {
        if (NormalizeOptional(value) is { } normalized && normalized.Length > max)
            throw new DomainException($"{field} deve ter no maximo {max} caracteres.");
    }

    private static string? NormalizeOptional(string? value) =>
        string.IsNullOrWhiteSpace(value) ? null : value.Trim();
}
