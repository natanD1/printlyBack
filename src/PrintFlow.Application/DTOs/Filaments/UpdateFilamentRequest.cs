namespace PrintFlow.Application.DTOs.Filaments;

public record UpdateFilamentRequest(string Name, string Brand, string ColorHex, decimal Weight, decimal FilamentPrice);
