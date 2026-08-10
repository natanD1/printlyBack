namespace PrintFlow.Application.DTOs.Filaments;

public record CreateFilamentRequest(string Name, Guid BrandId, Guid TypeId, string ColorHex, decimal Weight, decimal FilamentPrice);
