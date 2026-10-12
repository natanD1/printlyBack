using Microsoft.EntityFrameworkCore;
using Printly.Domain.Entities;
using Printly.Domain.Interfaces;

namespace Printly.Infrastructure.Persistence.Repositories;

public class UserSettingsRepository : IUserSettingsRepository
{
    private readonly PrintlyDbContext _context;

    public UserSettingsRepository(PrintlyDbContext context)
    {
        _context = context;
    }

    public Task<UserSettings?> GetByUserIdAsync(Guid userId, CancellationToken cancellationToken = default) =>
        _context.UserSettings.FirstOrDefaultAsync(s => s.UserId == userId, cancellationToken);

    public async Task<UserSettings> GetOrCreateAsync(Guid userId, CancellationToken cancellationToken = default)
    {
        var settings = await GetByUserIdAsync(userId, cancellationToken);

        if (settings is not null)
            return settings;

        settings = UserSettings.CreateDefault(userId);
        await AddAsync(settings, cancellationToken);

        return settings;
    }

    public async Task AddAsync(UserSettings settings, CancellationToken cancellationToken = default) =>
        await _context.UserSettings.AddAsync(settings, cancellationToken);
}
