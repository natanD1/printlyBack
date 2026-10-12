namespace Printly.Application.DTOs.Auth;

public record UserDto(Guid Id, string Name, string Email, DateTime CreatedAt, bool IsAdmin);
