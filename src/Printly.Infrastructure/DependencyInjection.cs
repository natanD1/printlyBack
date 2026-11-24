using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.DependencyInjection.Extensions;
using Printly.Application.Interfaces;
using Printly.Application.Services;
using Printly.Domain.Interfaces;
using Printly.Infrastructure.BackgroundServices;
using Printly.Infrastructure.Persistence;
using Printly.Infrastructure.Persistence.Repositories;
using Printly.Infrastructure.Security;

namespace Printly.Infrastructure;

public static class DependencyInjection
{
    public static IServiceCollection AddInfrastructure(this IServiceCollection services, IConfiguration configuration)
    {
        var connectionString = configuration.GetConnectionString("DefaultConnection")
            ?? throw new InvalidOperationException("Connection string 'DefaultConnection' not found.");

        services.AddDbContext<PrintlyDbContext>(options =>
            options.UseMySql(connectionString, ServerVersion.AutoDetect(connectionString)));

        services.Configure<JwtSettings>(configuration.GetSection(JwtSettings.SectionName));

        services.TryAddSingleton(TimeProvider.System);

        services.AddScoped<IUnitOfWork, UnitOfWork>();
        services.AddScoped<IUserRepository, UserRepository>();
        services.AddScoped<IInviteCodeRepository, InviteCodeRepository>();
        services.AddScoped<IProductRepository, ProductRepository>();
        services.AddScoped<IFilamentRepository, FilamentRepository>();
        services.AddScoped<IFilamentBrandRepository, FilamentBrandRepository>();
        services.AddScoped<IFilamentTypeRepository, FilamentTypeRepository>();
        services.AddScoped<IUserSettingsRepository, UserSettingsRepository>();
        services.AddScoped<IFeedbackRepository, FeedbackRepository>();
        services.AddScoped<IPrinterBrandRepository, PrinterBrandRepository>();
        services.AddScoped<IPrinterModelRepository, PrinterModelRepository>();
        services.AddScoped<IPrinterRepository, PrinterRepository>();
        services.AddScoped<ICompanyProfileRepository, CompanyProfileRepository>();
        services.AddScoped<IRefreshTokenRepository, RefreshTokenRepository>();
        services.AddScoped<IPlanRepository, PlanRepository>();
        services.AddScoped<IShowcaseRepository, ShowcaseRepository>();

        services.AddScoped<IPasswordHasher, PasswordHasher>();
        services.AddScoped<ITokenService, TokenService>();

        services.AddScoped<IAuthService, AuthService>();
        services.AddScoped<IInviteCodeService, InviteCodeService>();
        services.AddScoped<IProductService, ProductService>();
        services.AddScoped<IFilamentService, FilamentService>();
        services.AddScoped<IFilamentBrandService, FilamentBrandService>();
        services.AddScoped<IFilamentTypeService, FilamentTypeService>();
        services.AddScoped<ISettingsService, SettingsService>();
        services.AddScoped<IFeedbackService, FeedbackService>();
        services.AddScoped<IPrinterBrandService, PrinterBrandService>();
        services.AddScoped<IPrinterModelService, PrinterModelService>();
        services.AddScoped<IPrinterService, PrinterService>();
        services.AddScoped<ICompanyProfileService, CompanyProfileService>();
        services.AddScoped<IEntitlementService, EntitlementService>();
        services.AddScoped<IPlanService, PlanService>();
        services.AddScoped<IUserManagementService, UserManagementService>();
        services.AddScoped<IShowcaseService, ShowcaseService>();
        services.AddScoped<IPublicShowcaseService, PublicShowcaseService>();

        services.AddHostedService<InviteCodeExpirationService>();
        services.AddHostedService<RefreshTokenCleanupService>();

        return services;
    }
}
