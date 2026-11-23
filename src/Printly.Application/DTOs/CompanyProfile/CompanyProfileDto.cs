namespace Printly.Application.DTOs.CompanyProfile;

public record CompanyProfileDto(
    Guid Id,
    string Name,
    string? LogoBase64,
    string? Phone,
    string? Email,
    string? WhatsApp,
    string? Instagram,
    string? Website,
    DateTime CreatedAt,
    DateTime? UpdatedAt);
