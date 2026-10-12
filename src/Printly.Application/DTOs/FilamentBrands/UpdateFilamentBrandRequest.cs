namespace Printly.Application.DTOs.FilamentBrands;

public record UpdateFilamentBrandRequest(string NameBrand, string? ImageBrandBase64, bool IsActive);
