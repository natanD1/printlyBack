namespace Printly.Application.DTOs.Showcases;

/// <summary><see cref="ItemCount"/> = total de itens, em qualquer estado.</summary>
public record AdminShowcaseDto(
    Guid Id,
    Guid UserId,
    string Slug,
    string OwnerName,
    string OwnerEmail,
    bool IsPublished,
    bool IsBlocked,
    int ItemCount);
