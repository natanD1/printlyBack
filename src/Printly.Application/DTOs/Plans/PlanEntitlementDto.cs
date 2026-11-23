namespace Printly.Application.DTOs.Plans;

/// <summary><see cref="Key"/> e o nome da chave (ex.: "MaxPrinters").</summary>
public record PlanEntitlementDto(string Key, int? IntValue, bool? BoolValue);
