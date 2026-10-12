using Printly.Domain.Entities;

namespace Printly.Domain.Interfaces;

public interface IFeedbackRepository
{
    Task<Feedback?> GetByIdAsync(Guid id, CancellationToken cancellationToken = default);
    Task<List<Feedback>> GetAllAsync(CancellationToken cancellationToken = default);
    Task AddAsync(Feedback feedback, CancellationToken cancellationToken = default);
}
