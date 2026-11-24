using Printly.Application.DTOs.CompanyProfile;
using Printly.Application.Interfaces;
using Printly.Domain.Entities;
using Printly.Domain.Enums;
using Printly.Domain.Exceptions;
using Printly.Domain.Interfaces;

namespace Printly.Application.Services;

public class CompanyProfileService : ICompanyProfileService
{
    private readonly ICompanyProfileRepository _companyProfileRepository;
    private readonly IEntitlementService _entitlementService;
    private readonly IShowcaseRepository _showcaseRepository;
    private readonly IUnitOfWork _unitOfWork;

    public CompanyProfileService(
        ICompanyProfileRepository companyProfileRepository,
        IEntitlementService entitlementService,
        IShowcaseRepository showcaseRepository,
        IUnitOfWork unitOfWork)
    {
        _companyProfileRepository = companyProfileRepository;
        _entitlementService = entitlementService;
        _showcaseRepository = showcaseRepository;
        _unitOfWork = unitOfWork;
    }

    public async Task<CompanyProfileDto?> GetAsync(Guid userId, CancellationToken cancellationToken = default)
    {
        var companyProfile = await _companyProfileRepository.GetByUserIdAsync(userId, cancellationToken);

        return companyProfile is null ? null : ToDto(companyProfile);
    }

    public async Task<CompanyProfileDto> SaveAsync(Guid userId, SaveCompanyProfileRequest request, CancellationToken cancellationToken = default)
    {
        // So enviar/trocar logo exige o recurso. Textos e RemoveLogo passam; logo ja salvo nao e apagado.
        if (!string.IsNullOrWhiteSpace(request.LogoBase64))
            await _entitlementService.EnsureFeatureAsync(userId, PlanEntitlementKey.CompanyLogoInQuote, cancellationToken);

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
            var previousName = companyProfile.Name;

            companyProfile.Update(
                request.Name,
                request.LogoBase64,
                request.RemoveLogo,
                request.Phone,
                request.Email,
                request.WhatsApp,
                request.Instagram,
                request.Website);

            if (companyProfile.Name != previousName)
                await SyncShowcaseSlugAsync(userId, companyProfile.Name, cancellationToken);
        }

        try
        {
            await _unitOfWork.SaveChangesAsync(cancellationToken);
        }
        catch (UniqueConstraintException)
        {
            // Corrida: outro usuario pegou o mesmo endereco entre a checagem e o save.
            throw new DomainException("Nao foi possivel atualizar o endereco da vitrine agora. Tente salvar de novo.");
        }

        return ToDto(companyProfile);
    }

    /// <summary>
    /// O endereco da vitrine nao e editavel: segue o nome da empresa. Trocar o nome troca o link (sem redirect do antigo).
    /// Grava no mesmo SaveChanges do perfil.
    /// </summary>
    private async Task SyncShowcaseSlugAsync(Guid userId, string companyName, CancellationToken cancellationToken)
    {
        var showcase = await _showcaseRepository.GetByUserIdAsync(userId, cancellationToken);

        if (showcase is null)
            return;

        var slug = await ShowcaseSlugResolver.ResolveFreeAsync(_showcaseRepository, companyName, showcase.Id, cancellationToken);

        if (slug != showcase.Slug)
            showcase.ChangeSlug(slug);
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
