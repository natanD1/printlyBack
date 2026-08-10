using PrintFlow.Domain.Enums;
using PrintFlow.Domain.Exceptions;

namespace PrintFlow.Domain.Entities;

public class Feedback : BaseEntity
{
    public string Title { get; private set; } = default!;
    public string Description { get; private set; } = default!;
    public FeedbackStatus Status { get; private set; } = FeedbackStatus.Pending;
    public Guid UserId { get; private set; }

    protected Feedback()
    {
    }

    public Feedback(string title, string description, Guid userId)
    {
        Validate(title, description);

        Title = title.Trim();
        Description = description.Trim();
        UserId = userId;
    }

    public void Approve()
    {
        Status = FeedbackStatus.Approved;
        MarkAsUpdated();
    }

    public void Reject()
    {
        Status = FeedbackStatus.Rejected;
        MarkAsUpdated();
    }

    public void MarkAsPending()
    {
        Status = FeedbackStatus.Pending;
        MarkAsUpdated();
    }

    private static void Validate(string title, string description)
    {
        if (string.IsNullOrWhiteSpace(title))
            throw new DomainException("Titulo do feedback e obrigatorio.");

        if (string.IsNullOrWhiteSpace(description))
            throw new DomainException("Descricao do feedback e obrigatoria.");
    }
}
