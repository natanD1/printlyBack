using Printly.Application.DTOs.Settings;

namespace Printly.Application.Interfaces;

public interface ISettingsService
{
    Task<SettingsDto> GetAsync(Guid userId, CancellationToken cancellationToken = default);
    Task<SettingsDto> UpdateAsync(Guid userId, UpdateSettingsRequest request, CancellationToken cancellationToken = default);
}
