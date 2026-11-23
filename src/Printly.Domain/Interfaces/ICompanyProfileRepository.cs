using Printly.Domain.Entities;

namespace Printly.Domain.Interfaces;

public interface ICompanyProfileRepository
{
    Task<CompanyProfile?> GetByUserIdAsync(Guid userId, CancellationToken cancellationToken = default);
    Task AddAsync(CompanyProfile companyProfile, CancellationToken cancellationToken = default);
}
