using Printly.Application.DTOs.Feedbacks;

namespace Printly.Application.Interfaces;

public interface IFeedbackService
{
    Task<List<FeedbackDto>> GetAllAsync(CancellationToken cancellationToken = default);
    Task<FeedbackDto> SubmitAsync(CreateFeedbackRequest request, Guid userId, CancellationToken cancellationToken = default);
    Task<FeedbackDto> UpdateStatusAsync(Guid id, UpdateFeedbackStatusRequest request, CancellationToken cancellationToken = default);
}
