namespace Printly.Application.DTOs.Filaments;

public record UpdateFilamentRequest(string Name, Guid BrandId, Guid TypeId, string ColorHex, decimal Weight, decimal FilamentPrice);
