using PrintFlow.Application.DTOs.Settings;

namespace PrintFlow.Application.Interfaces;

public interface ISettingsService
{
    Task<SettingsDto> GetAsync(Guid userId, CancellationToken cancellationToken = default);
    Task<SettingsDto> UpdateAsync(Guid userId, UpdateSettingsRequest request, CancellationToken cancellationToken = default);
}
