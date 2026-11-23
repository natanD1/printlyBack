using Microsoft.EntityFrameworkCore;
using Printly.Domain.Entities;
using Printly.Domain.Interfaces;

namespace Printly.Infrastructure.Persistence.Repositories;

public class CompanyProfileRepository : ICompanyProfileRepository
{
    private readonly PrintlyDbContext _context;

    public CompanyProfileRepository(PrintlyDbContext context)
    {
        _context = context;
    }

    public Task<CompanyProfile?> GetByUserIdAsync(Guid userId, CancellationToken cancellationToken = default) =>
        _context.CompanyProfiles.FirstOrDefaultAsync(c => c.UserId == userId, cancellationToken);

    public async Task AddAsync(CompanyProfile companyProfile, CancellationToken cancellationToken = default) =>
        await _context.CompanyProfiles.AddAsync(companyProfile, cancellationToken);
}
