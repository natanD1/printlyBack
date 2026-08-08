using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Hosting;
using Microsoft.Extensions.Logging;
using PrintFlow.Application.Interfaces;

namespace PrintFlow.Infrastructure.BackgroundServices;

public class InviteCodeExpirationService : BackgroundService
{
    private static readonly TimeSpan Interval = TimeSpan.FromHours(24);

    private readonly IServiceScopeFactory _scopeFactory;
    private readonly ILogger<InviteCodeExpirationService> _logger;

    public InviteCodeExpirationService(IServiceScopeFactory scopeFactory, ILogger<InviteCodeExpirationService> logger)
    {
        _scopeFactory = scopeFactory;
        _logger = logger;
    }

    protected override async Task ExecuteAsync(CancellationToken stoppingToken)
    {
        while (!stoppingToken.IsCancellationRequested)
        {
            try
            {
                using var scope = _scopeFactory.CreateScope();
                var inviteCodeService = scope.ServiceProvider.GetRequiredService<IInviteCodeService>();
                await inviteCodeService.PurgeExpiredAsync(stoppingToken);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Falha ao expurgar codigos de convite expirados.");
            }

            await Task.Delay(Interval, stoppingToken);
        }
    }
}
