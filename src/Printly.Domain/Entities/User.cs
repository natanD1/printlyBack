using Printly.Domain.Exceptions;

namespace Printly.Domain.Entities;

public class User : BaseEntity
{
    public string Name { get; private set; } = default!;
    public string Email { get; private set; } = default!;
    public string PasswordHash { get; private set; } = default!;
    public Guid InviteCodeId { get; private set; }
    public Guid PlanId { get; private set; }
    public bool IsAdmin { get; private set; }
    public DateTime? OnboardingCompletedAt { get; private set; }

    protected User()
    {
    }

    public User(string name, string email, string passwordHash, Guid inviteCodeId, Guid planId)
    {
        if (string.IsNullOrWhiteSpace(name))
            throw new DomainException("Nome e obrigatorio.");

        if (string.IsNullOrWhiteSpace(email))
            throw new DomainException("Email e obrigatorio.");

        if (string.IsNullOrWhiteSpace(passwordHash))
            throw new DomainException("Senha e obrigatoria.");

        if (planId == Guid.Empty)
            throw new DomainException("Plano e obrigatorio.");

        Name = name.Trim();
        Email = email.Trim().ToLowerInvariant();
        PasswordHash = passwordHash;
        InviteCodeId = inviteCodeId;
        PlanId = planId;
        IsAdmin = false;
    }

    /// <summary>Troca de plano: chamar so pelo PlanService.ChangeUserPlanAsync, que valida plano existente e ativo.</summary>
    public void ChangePlan(Guid planId)
    {
        if (planId == Guid.Empty)
            throw new DomainException("Plano e obrigatorio.");

        if (PlanId == planId)
            return;

        PlanId = planId;
        MarkAsUpdated();
    }

    public void MarkAsAdmin()
    {
        IsAdmin = true;
        MarkAsUpdated();
    }

    public void CompleteOnboarding()
    {
        if (OnboardingCompletedAt is not null)
            return;

        OnboardingCompletedAt = DateTime.UtcNow;
        MarkAsUpdated();
    }
}
