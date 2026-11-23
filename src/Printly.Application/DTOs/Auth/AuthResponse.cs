namespace Printly.Application.DTOs.Auth;

/// <summary>Tempos de expiracao em segundos (int) para nao passar pelo conversor de data brasileiro.</summary>
public record AuthResponse(
    string AccessToken,
    int AccessTokenExpiresIn,
    string RefreshToken,
    int RefreshTokenExpiresIn,
    UserDto User);
