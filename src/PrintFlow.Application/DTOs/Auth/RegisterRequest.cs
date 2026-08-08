namespace PrintFlow.Application.DTOs.Auth;

public record RegisterRequest(string Name, string Email, string Password, string InviteCode);
