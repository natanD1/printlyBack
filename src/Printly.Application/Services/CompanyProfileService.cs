using Printly.Application.DTOs.CompanyProfile;
using Printly.Application.Interfaces;
using Printly.Domain.Entities;
using Printly.Domain.Interfaces;

namespace Printly.Application.Services;

public class CompanyProfileService : ICompanyProfileService
{
    private readonly ICompanyProfileRepository _companyProfileRepository;
    private readonly IUnitOfWork _unitOfWork;

    public CompanyProfileService(ICompanyProfileRepository companyProfileRepository, IUnitOfWork unitOfWork)
    {
        _companyProfileRepository = companyProfileRepository;
        _unitOfWork = unitOfWork;
    }

    public async Task<CompanyProfileDto?> GetAsync(Guid userId, CancellationToken cancellationToken = default)
    {
        var companyProfile = await _companyProfileRepository.GetByUserIdAsync(userId, cancellationToken);

        return companyProfile is null ? null : ToDto(companyProfile);
    }

    public async Task<CompanyProfileDto> SaveAsync(Guid userId, SaveCompanyProfileRequest request, CancellationToken cancellationToken = default)
    {
        var companyProfile = await _companyProfileRepository.GetByUserIdAsync(userId, cancellationToken);

        if (companyProfile is null)
        {
            companyProfile = new CompanyProfile(
                userId,
                request.Name,
                request.LogoBase64,
                request.Phone,
                request.Email,
                request.WhatsApp,
                request.Instagram,
                request.Website);

            await _companyProfileRepository.AddAsync(companyProfile, cancellationToken);
        }
        else
        {
            companyProfile.Update(
                request.Name,
                request.LogoBase64,
                request.RemoveLogo,
                request.Phone,
                request.Email,
                request.WhatsApp,
                request.Instagram,
                request.Website);
        }

        await _unitOfWork.SaveChangesAsync(cancellationToken);

        return ToDto(companyProfile);
    }

    private static CompanyProfileDto ToDto(CompanyProfile companyProfile) => new(
        companyProfile.Id,
        companyProfile.Name,
        companyProfile.LogoBase64,
        companyProfile.Phone,
        companyProfile.Email,
        companyProfile.WhatsApp,
        companyProfile.Instagram,
        companyProfile.Website,
        companyProfile.CreatedAt,
        companyProfile.UpdatedAt);
}
