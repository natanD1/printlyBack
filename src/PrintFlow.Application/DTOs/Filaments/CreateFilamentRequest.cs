namespace PrintFlow.Application.DTOs.Filaments;

public record CreateFilamentRequest(string Name, string Brand, string ColorHex, decimal Weight, decimal FilamentPrice);
