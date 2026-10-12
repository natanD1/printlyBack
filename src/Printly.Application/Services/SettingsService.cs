using Printly.Application.DTOs.Settings;
using Printly.Application.Interfaces;
using Printly.Domain.Entities;
using Printly.Domain.Interfaces;

namespace Printly.Application.Services;

public class SettingsService : ISettingsService
{
    private readonly IUserSettingsRepository _userSettingsRepository;
    private readonly IUnitOfWork _unitOfWork;

    public SettingsService(IUserSettingsRepository userSettingsRepository, IUnitOfWork unitOfWork)
    {
        _userSettingsRepository = userSettingsRepository;
        _unitOfWork = unitOfWork;
    }

    public async Task<SettingsDto> GetAsync(Guid userId, CancellationToken cancellationToken = default)
    {
        var settings = await _userSettingsRepository.GetOrCreateAsync(userId, cancellationToken);
        await _unitOfWork.SaveChangesAsync(cancellationToken);

        return ToDto(settings);
    }

    public async Task<SettingsDto> UpdateAsync(Guid userId, UpdateSettingsRequest request, CancellationToken cancellationToken = default)
    {
        var settings = await _userSettingsRepository.GetOrCreateAsync(userId, cancellationToken);

        settings.Update(request.KwhPrice, request.AveragePowerWatts, request.DefaultProfitMarginPercentage);

        await _unitOfWork.SaveChangesAsync(cancellationToken);

        return ToDto(settings);
    }

    private static SettingsDto ToDto(UserSettings settings) => new(
        settings.Id,
        settings.KwhPrice,
        settings.AveragePowerWatts,
        settings.DefaultProfitMarginPercentage,
        settings.CreatedAt,
        settings.UpdatedAt);
}
