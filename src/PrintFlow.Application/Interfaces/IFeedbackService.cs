using PrintFlow.Application.DTOs.Feedbacks;

namespace PrintFlow.Application.Interfaces;

public interface IFeedbackService
{
    Task<List<FeedbackDto>> GetAllAsync(CancellationToken cancellationToken = default);
    Task<FeedbackDto> SubmitAsync(CreateFeedbackRequest request, Guid userId, CancellationToken cancellationToken = default);
    Task<FeedbackDto> UpdateStatusAsync(Guid id, UpdateFeedbackStatusRequest request, CancellationToken cancellationToken = default);
}
