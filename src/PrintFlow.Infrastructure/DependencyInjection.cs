using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using PrintFlow.Application.Interfaces;
using PrintFlow.Application.Services;
using PrintFlow.Domain.Interfaces;
using PrintFlow.Infrastructure.BackgroundServices;
using PrintFlow.Infrastructure.Persistence;
using PrintFlow.Infrastructure.Persistence.Repositories;
using PrintFlow.Infrastructure.Security;

namespace PrintFlow.Infrastructure;

public static class DependencyInjection
{
    public static IServiceCollection AddInfrastructure(this IServiceCollection services, IConfiguration configuration)
    {
        var connectionString = configuration.GetConnectionString("DefaultConnection")
            ?? throw new InvalidOperationException("Connection string 'DefaultConnection' not found.");

        services.AddDbContext<PrintFlowDbContext>(options =>
            options.UseMySql(connectionString, ServerVersion.AutoDetect(connectionString)));

        services.Configure<JwtSettings>(configuration.GetSection(JwtSettings.SectionName));

        services.AddScoped<IUnitOfWork, UnitOfWork>();
        services.AddScoped<IUserRepository, UserRepository>();
        services.AddScoped<IInviteCodeRepository, InviteCodeRepository>();
        services.AddScoped<IProductRepository, ProductRepository>();
        services.AddScoped<IFilamentRepository, FilamentRepository>();
        services.AddScoped<IUserSettingsRepository, UserSettingsRepository>();

        services.AddScoped<IPasswordHasher, PasswordHasher>();
        services.AddScoped<ITokenService, TokenService>();

        services.AddScoped<IAuthService, AuthService>();
        services.AddScoped<IInviteCodeService, InviteCodeService>();
        services.AddScoped<IProductService, ProductService>();
        services.AddScoped<IFilamentService, FilamentService>();
        services.AddScoped<ISettingsService, SettingsService>();

        services.AddHostedService<InviteCodeExpirationService>();

        return services;
    }
}
