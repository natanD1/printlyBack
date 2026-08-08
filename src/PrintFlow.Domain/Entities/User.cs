using PrintFlow.Domain.Exceptions;

namespace PrintFlow.Domain.Entities;

public class User : BaseEntity
{
    public string Name { get; private set; } = default!;
    public string Email { get; private set; } = default!;
    public string PasswordHash { get; private set; } = default!;
    public Guid InviteCodeId { get; private set; }
    public bool IsAdmin { get; private set; }

    protected User()
    {
    }

    public User(string name, string email, string passwordHash, Guid inviteCodeId)
    {
        if (string.IsNullOrWhiteSpace(name))
            throw new DomainException("Nome e obrigatorio.");

        if (string.IsNullOrWhiteSpace(email))
            throw new DomainException("Email e obrigatorio.");

        if (string.IsNullOrWhiteSpace(passwordHash))
            throw new DomainException("Senha e obrigatoria.");

        Name = name.Trim();
        Email = email.Trim().ToLowerInvariant();
        PasswordHash = passwordHash;
        InviteCodeId = inviteCodeId;
        IsAdmin = false;
    }

    public void MarkAsAdmin()
    {
        IsAdmin = true;
        MarkAsUpdated();
    }
}
