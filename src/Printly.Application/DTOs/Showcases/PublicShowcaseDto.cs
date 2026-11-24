namespace Printly.Application.DTOs.Showcases;

/// <summary>
/// Resposta do GET publico. Allowlist: so o que esta aqui sai pra internet (nunca entidade, ids de usuario,
/// custo, quantidade, datas...). Dois formatos, serializados pelo tipo concreto.
/// </summary>
public abstract record PublicShowcaseResponse(bool Available);

/// <summary>Nao publicada, bloqueada ou dono sem o recurso: serializa exatamente <c>{ "available": false }</c>.</summary>
public sealed record PublicShowcaseUnavailableDto() : PublicShowcaseResponse(false);

public sealed record PublicShowcaseDto(
    string StoreName,
    bool HasLogo,
    bool ShowBranding,
    PublicShowcaseContactsDto Contacts,
    List<PublicShowcaseItemDto> Items) : PublicShowcaseResponse(true);

/// <summary>Contatos ja sanitizados (<c>PublicContactSanitizer</c>): invalido vira nulo.</summary>
public record PublicShowcaseContactsDto(string? Phone, string? Email, string? WhatsApp, string? Instagram, string? Website);

/// <summary><see cref="ProductId"/> serve so pra montar a URL da foto.</summary>
public record PublicShowcaseItemDto(Guid ProductId, string Name, decimal UnitPrice, bool HasPhoto);
