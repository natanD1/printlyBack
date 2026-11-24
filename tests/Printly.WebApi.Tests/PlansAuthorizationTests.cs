using System.Net;
using System.Net.Http.Json;
using System.Security.Claims;
using System.Text.Encodings.Web;
using System.Text.Json;
using Microsoft.AspNetCore.Authentication;
using Microsoft.AspNetCore.Builder;
using Microsoft.AspNetCore.Hosting;
using Microsoft.AspNetCore.Hosting.Server;
using Microsoft.AspNetCore.Hosting.Server.Features;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;
using Printly.Application.DTOs.Plans;
using Printly.Application.DTOs.Users;
using Printly.Application.Interfaces;
using Printly.Domain.Enums;
using Printly.Domain.Exceptions;
using Printly.WebApi.Controllers;
using Printly.WebApi.Extensions;

namespace Printly.WebApi.Tests;

/// <summary>
/// Sobe os controllers reais num Kestrel local (porta livre) com autenticacao de teste, pra garantir que o
/// Roles = "Admin" do PlansController nao bloqueia o GET /plans/me nem o GET /plans/available e que o 403 de plano
/// sai com { message, code }.
/// </summary>
public sealed class PlansAuthorizationTests : IAsyncLifetime
{
    private static readonly Guid BlockedUserId = Guid.Parse("11111111-1111-1111-1111-111111111111");

    private WebApplication _app = default!;
    private HttpClient _client = default!;

    public async Task InitializeAsync()
    {
        var builder = WebApplication.CreateBuilder();
        builder.Logging.ClearProviders();
        builder.WebHost.UseUrls("http://127.0.0.1:0");

        builder.Services.AddControllers().AddApplicationPart(typeof(PlansController).Assembly);
        builder.Services
            .AddAuthentication(TestAuthHandler.SchemeName)
            .AddScheme<AuthenticationSchemeOptions, TestAuthHandler>(TestAuthHandler.SchemeName, _ => { });
        builder.Services.AddAuthorization();
        builder.Services.AddSingleton<IEntitlementService, StubEntitlementService>();
        builder.Services.AddSingleton<IPlanService, StubPlanService>();
        builder.Services.AddSingleton<IUserManagementService, StubUserManagementService>();

        _app = builder.Build();
        _app.UseMiddleware<ExceptionHandlingMiddleware>();
        _app.UseAuthentication();
        _app.UseAuthorization();
        _app.MapControllers();

        await _app.StartAsync();

        var address = _app.Services.GetRequiredService<IServer>().Features.Get<IServerAddressesFeature>()!.Addresses.First();
        _client = new HttpClient { BaseAddress = new Uri(address) };
    }

    public async Task DisposeAsync()
    {
        _client.Dispose();
        await _app.DisposeAsync();
    }

    private Task<HttpResponseMessage> GetAsync(string url, string? role = null, Guid? userId = null)
    {
        var request = new HttpRequestMessage(HttpMethod.Get, url);

        if (role is not null)
        {
            request.Headers.Add(TestAuthHandler.UserHeader, (userId ?? Guid.NewGuid()).ToString());
            request.Headers.Add(TestAuthHandler.RoleHeader, role);
        }

        return _client.SendAsync(request);
    }

    [Theory]
    [InlineData("User")]
    [InlineData("Admin")]
    public async Task MyPlan_AnyLoggedUser_Gets200(string role)
    {
        var response = await GetAsync("/plans/me", role);

        Assert.Equal(HttpStatusCode.OK, response.StatusCode);
        using var json = JsonDocument.Parse(await response.Content.ReadAsStringAsync());
        var entitlement = json.RootElement.GetProperty("entitlements")[0];
        Assert.Equal(new[] { "key", "label", "type", "limit", "used", "enabled" }, entitlement.EnumerateObject().Select(p => p.Name));
        Assert.Equal("limit", entitlement.GetProperty("type").GetString());
    }

    [Fact]
    public async Task MyPlan_Anonymous_Gets401()
    {
        var response = await GetAsync("/plans/me");

        Assert.Equal(HttpStatusCode.Unauthorized, response.StatusCode);
    }

    [Theory]
    [InlineData("User")]
    [InlineData("Admin")]
    public async Task AvailablePlans_AnyLoggedUser_Gets200_WithoutAdminFields(string role)
    {
        var response = await GetAsync("/plans/available", role);

        Assert.Equal(HttpStatusCode.OK, response.StatusCode);
        using var json = JsonDocument.Parse(await response.Content.ReadAsStringAsync());

        var rule = json.RootElement.GetProperty("rules")[0];
        Assert.Equal(
            new[] { "key", "label", "description", "type", "group", "unit", "unitSingular", "zeroLabel" },
            rule.EnumerateObject().Select(p => p.Name));

        var plan = json.RootElement.GetProperty("plans")[0];
        var planFields = plan.EnumerateObject().Select(p => p.Name).ToList();
        Assert.Equal(new[] { "id", "name", "description", "monthlyPrice", "sortOrder", "isCurrent", "entitlements" }, planFields);
        Assert.DoesNotContain("userCount", planFields);
        Assert.DoesNotContain("isDefault", planFields);
        Assert.DoesNotContain("isActive", planFields);
        Assert.DoesNotContain("isPublic", planFields);
    }

    [Fact]
    public async Task AvailablePlans_Anonymous_Gets401()
    {
        var response = await GetAsync("/plans/available");

        Assert.Equal(HttpStatusCode.Unauthorized, response.StatusCode);
    }

    [Theory]
    [InlineData("/plans")]
    [InlineData("/plans/entitlement-catalog")]
    [InlineData("/users")]
    public async Task AdminRoutes_CommonUser_Gets403(string url)
    {
        var response = await GetAsync(url, "User");

        Assert.Equal(HttpStatusCode.Forbidden, response.StatusCode);
    }

    [Theory]
    [InlineData("/plans")]
    [InlineData("/plans/entitlement-catalog")]
    [InlineData("/users")]
    public async Task AdminRoutes_Admin_Gets200(string url)
    {
        var response = await GetAsync(url, "Admin");

        Assert.Equal(HttpStatusCode.OK, response.StatusCode);
    }

    [Fact]
    public async Task PlanRestriction_Returns403_WithMessageAndCode()
    {
        var response = await GetAsync("/plans/me", "User", BlockedUserId);

        Assert.Equal(HttpStatusCode.Forbidden, response.StatusCode);
        var body = await response.Content.ReadFromJsonAsync<Dictionary<string, string>>();
        Assert.Equal("Bloqueado.", body!["message"]);
        Assert.Equal(PlanRestrictionException.PlanLimitReached, body["code"]);
    }

    private sealed class TestAuthHandler : AuthenticationHandler<AuthenticationSchemeOptions>
    {
        public const string SchemeName = "Test";
        public const string UserHeader = "X-Test-User";
        public const string RoleHeader = "X-Test-Role";

        public TestAuthHandler(IOptionsMonitor<AuthenticationSchemeOptions> options, ILoggerFactory logger, UrlEncoder encoder)
            : base(options, logger, encoder)
        {
        }

        protected override Task<AuthenticateResult> HandleAuthenticateAsync()
        {
            if (!Request.Headers.TryGetValue(UserHeader, out var userId))
                return Task.FromResult(AuthenticateResult.NoResult());

            var claims = new List<Claim> { new(ClaimTypes.NameIdentifier, userId.ToString()) };

            if (Request.Headers.TryGetValue(RoleHeader, out var role))
                claims.Add(new Claim(ClaimTypes.Role, role.ToString()));

            var principal = new ClaimsPrincipal(new ClaimsIdentity(claims, SchemeName));
            return Task.FromResult(AuthenticateResult.Success(new AuthenticationTicket(principal, SchemeName)));
        }
    }

    private sealed class StubEntitlementService : IEntitlementService
    {
        public Task EnsureWithinLimitAsync(Guid userId, PlanEntitlementKey key, CancellationToken cancellationToken = default) => Task.CompletedTask;

        public Task EnsureWithinLimitAsync(Guid userId, PlanEntitlementKey key, int additional, CancellationToken cancellationToken = default) => Task.CompletedTask;

        public Task<int?> GetLimitAsync(Guid userId, PlanEntitlementKey key, CancellationToken cancellationToken = default) => Task.FromResult<int?>(null);

        public Task EnsureFeatureAsync(Guid userId, PlanEntitlementKey key, CancellationToken cancellationToken = default) => Task.CompletedTask;

        public Task<bool> IsFeatureEnabledAsync(Guid userId, PlanEntitlementKey key, CancellationToken cancellationToken = default) => Task.FromResult(true);

        public Task<MyPlanDto> GetMyPlanAsync(Guid userId, CancellationToken cancellationToken = default)
        {
            if (userId == BlockedUserId)
                throw new PlanRestrictionException(PlanRestrictionException.PlanLimitReached, "Bloqueado.");

            return Task.FromResult(new MyPlanDto(Guid.NewGuid(), "Free", false, new List<MyPlanEntitlementDto>
            {
                new("MaxPrinters", "Impressoras", "limit", 1, 0, null),
            }));
        }

        public Task<ProductHistoryWindow?> GetProductHistoryWindowAsync(Guid userId, CancellationToken cancellationToken = default) =>
            Task.FromResult<ProductHistoryWindow?>(null);
    }

    private sealed class StubPlanService : IPlanService
    {
        public List<EntitlementDefinitionDto> GetCatalog() => new();

        public Task<List<PlanDto>> GetAllAsync(CancellationToken cancellationToken = default) => Task.FromResult(new List<PlanDto>());

        public Task<PlanDto> GetByIdAsync(Guid id, CancellationToken cancellationToken = default) => throw new NotSupportedException();

        public Task<PlanDto> CreateAsync(SavePlanRequest request, CancellationToken cancellationToken = default) => throw new NotSupportedException();

        public Task<PlanDto> UpdateAsync(Guid id, SavePlanRequest request, CancellationToken cancellationToken = default) => throw new NotSupportedException();

        public Task DeleteAsync(Guid id, CancellationToken cancellationToken = default) => throw new NotSupportedException();

        public Task<AvailablePlansDto> GetAvailablePlansAsync(Guid userId, CancellationToken cancellationToken = default) =>
            Task.FromResult(new AvailablePlansDto(
                new List<EntitlementDefinitionDto>
                {
                    new("MaxPrinters", "Impressoras", "Maximo de impressoras.", "limit", "Cadastros", "impressoras", "impressora", null),
                },
                new List<AvailablePlanDto>
                {
                    new(Guid.NewGuid(), "Free", null, 0m, 0, true, new List<PlanEntitlementDto> { new("MaxPrinters", 1, null) }),
                }));

        public Task<ManagedUserDto> ChangeUserPlanAsync(Guid userId, Guid planId, CancellationToken cancellationToken = default) =>
            throw new NotSupportedException();
    }

    private sealed class StubUserManagementService : IUserManagementService
    {
        public Task<List<ManagedUserDto>> GetAllAsync(CancellationToken cancellationToken = default) => Task.FromResult(new List<ManagedUserDto>());
    }
}
