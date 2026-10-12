using Microsoft.EntityFrameworkCore;
using Printly.Domain.Entities;
using Printly.Domain.Interfaces;

namespace Printly.Infrastructure.Persistence.Repositories;

public class FeedbackRepository : IFeedbackRepository
{
    private readonly PrintlyDbContext _context;

    public FeedbackRepository(PrintlyDbContext context)
    {
        _context = context;
    }

    public Task<Feedback?> GetByIdAsync(Guid id, CancellationToken cancellationToken = default) =>
        _context.Feedbacks.FirstOrDefaultAsync(f => f.Id == id, cancellationToken);

    public Task<List<Feedback>> GetAllAsync(CancellationToken cancellationToken = default) =>
        _context.Feedbacks
            .OrderByDescending(f => f.CreatedAt)
            .ToListAsync(cancellationToken);

    public async Task AddAsync(Feedback feedback, CancellationToken cancellationToken = default) =>
        await _context.Feedbacks.AddAsync(feedback, cancellationToken);
}
