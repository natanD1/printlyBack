using Microsoft.EntityFrameworkCore;
using PrintFlow.Domain.Entities;
using PrintFlow.Domain.Interfaces;

namespace PrintFlow.Infrastructure.Persistence.Repositories;

public class InviteCodeRepository : IInviteCodeRepository
{
    private readonly PrintFlowDbContext _context;

    public InviteCodeRepository(PrintFlowDbContext context)
    {
        _context = context;
    }

    public Task<InviteCode?> GetByIdAsync(Guid id, CancellationToken cancellationToken = default) =>
        _context.InviteCodes.FirstOrDefaultAsync(i => i.Id == id, cancellationToken);

    public Task<InviteCode?> GetByCodeAsync(string code, CancellationToken cancellationToken = default) =>
        _context.InviteCodes.FirstOrDefaultAsync(i => i.Code == code.Trim().ToUpper(), cancellationToken);

    public Task<List<InviteCode>> GetAllAsync(CancellationToken cancellationToken = default) =>
        _context.InviteCodes.OrderByDescending(i => i.CreatedAt).ToListAsync(cancellationToken);

    public Task<List<InviteCode>> GetExpiredUnusedAsync(CancellationToken cancellationToken = default) =>
        _context.InviteCodes
            .Where(i => i.Status == Domain.Enums.InviteCodeStatus.Available && i.ExpiresAt <= DateTime.UtcNow)
            .ToListAsync(cancellationToken);

    public async Task AddAsync(InviteCode inviteCode, CancellationToken cancellationToken = default) =>
        await _context.InviteCodes.AddAsync(inviteCode, cancellationToken);

    public void Remove(InviteCode inviteCode) => _context.InviteCodes.Remove(inviteCode);
}
