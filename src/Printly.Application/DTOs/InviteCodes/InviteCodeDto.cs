namespace Printly.Application.DTOs.InviteCodes;

public record InviteCodeDto(
    Guid Id,
    string Code,
    string Status,
    DateTime CreatedAt,
    DateTime ExpiresAt,
    DateTime? UsedAt,
    Guid? UsedByUserId,
    string? UsedByUserName,
    string? UsedByUserEmail,
    DateTime? RevokedAt);
