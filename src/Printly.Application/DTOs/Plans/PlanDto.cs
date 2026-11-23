namespace Printly.Application.DTOs.Plans;

public record PlanDto(
    Guid Id,
    string Name,
    string? Description,
    decimal MonthlyPrice,
    bool IsActive,
    bool IsDefault,
    bool IsPublic,
    int SortOrder,
    int UserCount,
    List<PlanEntitlementDto> Entitlements);
