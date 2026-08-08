using PrintFlow.Domain.Enums;
using PrintFlow.Domain.Exceptions;

namespace PrintFlow.Domain.Entities;

public class InviteCode : BaseEntity
{
    public const int ValidityDays = 30;

    public string Code { get; private set; } = default!;
    public InviteCodeStatus Status { get; private set; } = InviteCodeStatus.Available;
    public DateTime ExpiresAt { get; private set; }
    public Guid? GeneratedByUserId { get; private set; }
    public Guid? UsedByUserId { get; private set; }
    public DateTime? UsedAt { get; private set; }
    public DateTime? RevokedAt { get; private set; }

    protected InviteCode()
    {
    }

    public InviteCode(string code, Guid? generatedByUserId)
    {
        if (string.IsNullOrWhiteSpace(code))
            throw new DomainException("Codigo de convite invalido.");

        Code = code.Trim().ToUpperInvariant();
        GeneratedByUserId = generatedByUserId;
        ExpiresAt = DateTime.UtcNow.AddDays(ValidityDays);
    }

    public bool IsExpired => DateTime.UtcNow >= ExpiresAt;

    public bool IsValid => Status == InviteCodeStatus.Available && !IsExpired;

    public void MarkAsUsed(Guid userId)
    {
        if (!IsValid)
            throw new DomainException("Codigo de convite nao esta disponivel para uso.");

        Status = InviteCodeStatus.Used;
        UsedByUserId = userId;
        UsedAt = DateTime.UtcNow;
        MarkAsUpdated();
    }

    public void Revoke()
    {
        if (Status == InviteCodeStatus.Used)
            throw new DomainException("Nao e possivel revogar um codigo ja utilizado.");

        Status = InviteCodeStatus.Revoked;
        RevokedAt = DateTime.UtcNow;
        MarkAsUpdated();
    }
}
