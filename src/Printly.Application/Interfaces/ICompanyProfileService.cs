using Printly.Application.DTOs.CompanyProfile;

namespace Printly.Application.Interfaces;

public interface ICompanyProfileService
{
    /// <summary>Nao cria registro: devolve null quando o usuario ainda nao salvou o perfil.</summary>
    Task<CompanyProfileDto?> GetAsync(Guid userId, CancellationToken cancellationToken = default);
    Task<CompanyProfileDto> SaveAsync(Guid userId, SaveCompanyProfileRequest request, CancellationToken cancellationToken = default);
}
