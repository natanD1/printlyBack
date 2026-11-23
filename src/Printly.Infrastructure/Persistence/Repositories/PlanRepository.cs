using Microsoft.EntityFrameworkCore;
using Printly.Domain.Entities;
using Printly.Domain.Interfaces;

namespace Printly.Infrastructure.Persistence.Repositories;

public class PlanRepository : IPlanRepository
{
    private readonly PrintlyDbContext _context;

    public PlanRepository(PrintlyDbContext context)
    {
        _context = context;
    }

    public Task<Plan?> GetByIdAsync(Guid id, CancellationToken cancellationToken = default) =>
        _context.Plans
            .Include(p => p.Entitlements)
            .FirstOrDefaultAsync(p => p.Id == id, cancellationToken);

    public Task<List<Plan>> GetAllAsync(CancellationToken cancellationToken = default) =>
        _context.Plans
            .Include(p => p.Entitlements)
            .OrderBy(p => p.SortOrder)
            .ThenBy(p => p.Name)
            .ToListAsync(cancellationToken);

    public Task<Plan?> GetDefaultAsync(CancellationToken cancellationToken = default) =>
        _context.Plans
            .Include(p => p.Entitlements)
            .FirstOrDefaultAsync(p => p.IsDefault, cancellationToken);

    public Task<bool> NameExistsAsync(string name, Guid? exceptId = null, CancellationToken cancellationToken = default)
    {
        var normalized = name.Trim().ToLower();

        return _context.Plans.AnyAsync(
            p => p.Name.ToLower() == normalized && (exceptId == null || p.Id != exceptId),
            cancellationToken);
    }

    public Task<int> CountUsersAsync(Guid planId, CancellationToken cancellationToken = default) =>
        _context.Users.CountAsync(u => u.PlanId == planId, cancellationToken);

    public Task<Dictionary<Guid, int>> CountUsersByPlanAsync(CancellationToken cancellationToken = default) =>
        _context.Users
            .GroupBy(u => u.PlanId)
            .Select(g => new { PlanId = g.Key, Count = g.Count() })
            .ToDictionaryAsync(x => x.PlanId, x => x.Count, cancellationToken);

    public async Task AddAsync(Plan plan, CancellationToken cancellationToken = default) =>
        await _context.Plans.AddAsync(plan, cancellationToken);

    public void Remove(Plan plan) => _context.Plans.Remove(plan);
}
