namespace Printly.Application.DTOs.Feedbacks;

public record FeedbackDto(
    Guid Id,
    string Title,
    string Description,
    string Status,
    DateTime CreatedAt,
    DateTime? UpdatedAt,
    string SubmittedByUserName,
    string SubmittedByUserEmail);
