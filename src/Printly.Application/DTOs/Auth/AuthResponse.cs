namespace Printly.Application.DTOs.Auth;

public record AuthResponse(string Token, DateTime ExpiresAt, UserDto User);
