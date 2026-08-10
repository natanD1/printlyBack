using PrintFlow.Application.DTOs.Feedbacks;
using PrintFlow.Application.Interfaces;
using PrintFlow.Domain.Entities;
using PrintFlow.Domain.Enums;
using PrintFlow.Domain.Exceptions;
using PrintFlow.Domain.Interfaces;

namespace PrintFlow.Application.Services;

public class FeedbackService : IFeedbackService
{
    private readonly IFeedbackRepository _feedbackRepository;
    private readonly IUserRepository _userRepository;
    private readonly IUnitOfWork _unitOfWork;

    public FeedbackService(IFeedbackRepository feedbackRepository, IUserRepository userRepository, IUnitOfWork unitOfWork)
    {
        _feedbackRepository = feedbackRepository;
        _userRepository = userRepository;
        _unitOfWork = unitOfWork;
    }

    public async Task<List<FeedbackDto>> GetAllAsync(CancellationToken cancellationToken = default)
    {
        var feedbacks = await _feedbackRepository.GetAllAsync(cancellationToken);

        var userIds = feedbacks.Select(f => f.UserId).Distinct().ToList();
        var usersById = (await _userRepository.GetByIdsAsync(userIds, cancellationToken))
            .ToDictionary(u => u.Id, u => u);

        return feedbacks.Select(feedback => ToDto(feedback, usersById)).ToList();
    }

    public async Task<FeedbackDto> SubmitAsync(CreateFeedbackRequest request, Guid userId, CancellationToken cancellationToken = default)
    {
        var user = await _userRepository.GetByIdAsync(userId, cancellationToken)
            ?? throw new DomainException("Usuario nao encontrado.");

        var feedback = new Feedback(request.Title, request.Description, userId);

        await _feedbackRepository.AddAsync(feedback, cancellationToken);
        await _unitOfWork.SaveChangesAsync(cancellationToken);

        return ToDto(feedback, new Dictionary<Guid, User> { [user.Id] = user });
    }

    public async Task<FeedbackDto> UpdateStatusAsync(Guid id, UpdateFeedbackStatusRequest request, CancellationToken cancellationToken = default)
    {
        var feedback = await _feedbackRepository.GetByIdAsync(id, cancellationToken)
            ?? throw new DomainException("Feedback nao encontrado.");

        if (!Enum.TryParse<FeedbackStatus>(request.Status, out var status))
            throw new DomainException("Status de feedback invalido.");

        switch (status)
        {
            case FeedbackStatus.Approved:
                feedback.Approve();
                break;
            case FeedbackStatus.Rejected:
                feedback.Reject();
                break;
            default:
                feedback.MarkAsPending();
                break;
        }

        await _unitOfWork.SaveChangesAsync(cancellationToken);

        var user = await _userRepository.GetByIdAsync(feedback.UserId, cancellationToken);

        return ToDto(feedback, user is null
            ? new Dictionary<Guid, User>()
            : new Dictionary<Guid, User> { [user.Id] = user });
    }

    private static FeedbackDto ToDto(Feedback feedback, IReadOnlyDictionary<Guid, User> usersById)
    {
        var submittedByUser = usersById.GetValueOrDefault(feedback.UserId);

        return new FeedbackDto(
            feedback.Id,
            feedback.Title,
            feedback.Description,
            feedback.Status.ToString(),
            feedback.CreatedAt,
            feedback.UpdatedAt,
            submittedByUser?.Name ?? string.Empty,
            submittedByUser?.Email ?? string.Empty);
    }
}
