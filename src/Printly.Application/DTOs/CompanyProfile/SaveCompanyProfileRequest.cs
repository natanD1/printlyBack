namespace Printly.Application.DTOs.CompanyProfile;

public record SaveCompanyProfileRequest(
    string Name,
    string? LogoBase64,
    bool RemoveLogo,
    string? Phone,
    string? Email,
    string? WhatsApp,
    string? Instagram,
    string? Website);
