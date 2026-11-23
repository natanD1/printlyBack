namespace Printly.Application.DTOs.Users;

public record ManagedUserDto(
    Guid Id,
    string Name,
    string Email,
    bool IsAdmin,
    DateTime CreatedAt,
    Guid PlanId,
    string PlanName);
