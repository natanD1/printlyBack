using System.Net;
using System.Reflection;
using System.Security.Claims;
using System.Text.Encodings.Web;
using System.Text.Json;
using System.Threading.RateLimiting;
using Microsoft.AspNetCore.Authentication;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Builder;
using Microsoft.AspNetCore.Hosting;
using Microsoft.AspNetCore.Hosting.Server;
using Microsoft.AspNetCore.Hosting.Server.Features;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.RateLimiting;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;
using Printly.Application.DTOs.Showcases;
using Printly.Application.Interfaces;
using Printly.Application.Services;
using Printly.WebApi.Controllers;
using Printly.WebApi.Extensions;

namespace Printly.WebApi.Tests;

/// <summary>Atributos de autorizacao dos controllers da vitrine (sem subir servidor).</summary>
public class ShowcaseAuthorizationAttributesTests
{
    [Fact]
    public void OwnerController_RequiresAnyLoggedUser()
    {
        var authorize = Assert.Single(typeof(ShowcaseController).GetCustomAttributes<AuthorizeAttribute>());

        Assert.Null(authorize.Roles);
        Assert.Empty(typeof(ShowcaseController).GetCustomAttributes<AllowAnonymousAttribute>());
        Assert.Equal("showcase", typeof(ShowcaseController).GetCustomAttribute<RouteAttribute>()!.Template);
    }

    [Fact]
    public void AdminController_RequiresAdminRoleOnClass()
    {
        var authorize = Assert.Single(typeof(AdminShowcasesController).GetCustomAttributes<AuthorizeAttribute>());

        Assert.Equal("Admin", authorize.Roles);
        Assert.Equal("admin/showcases", typeof(AdminShowcasesController).GetCustomAttribute<RouteAttribute>()!.Template);
        Assert.All(
            typeof(AdminShowcasesController).GetMethods(BindingFlags.Public | BindingFlags.Instance | BindingFlags.DeclaredOnly),
            m => Assert.Empty(m.GetCustomAttributes<AllowAnonymousAttribute>()));
    }

    [Fact]
    public void PublicController_IsAnonymous_AndRateLimited()
    {
        var type = typeof(PublicShowcasesController);

        Assert.NotNull(type.GetCustomAttribute<AllowAnonymousAttribute>());
        Assert.Empty(type.GetCustomAttributes<AuthorizeAttribute>());
        Assert.Equal(PublicShowcaseRateLimits.PolicyName, type.GetCustomAttribute<EnableRateLimitingAttribute>()!.PolicyName);
        Assert.Equal("public-showcase", PublicShowcaseRateLimits.PolicyName);
        Assert.Equal("public/showcases", type.GetCustomAttribute<RouteAttribute>()!.Template);
        Assert.Contains(":minlength(3):maxlength(60)", PublicShowcasesController.SlugRoute);
    }
}

/// <summary>Fabricas do rate limit, sem servidor: estourar a janela nega o lease (e a rejeicao e 429).</summary>
public class PublicShowcaseRateLimitsTests
{
    private static HttpContext Context(string path, string? slug = null, string ip = "10.0.0.1")
    {
        var context = new DefaultHttpContext();
        context.Request.Path = path;
        context.Connection.RemoteIpAddress = IPAddress.Parse(ip);

        if (slug is not null)
            context.Request.RouteValues["slug"] = slug;

        return context;
    }

    [Fact]
    public void Configure_RejectsWith429_AndSetsGlobalLimiter()
    {
        var options = new RateLimiterOptions();

        PublicShowcaseRateLimits.Configure(options);

        Assert.Equal(StatusCodes.Status429TooManyRequests, options.RejectionStatusCode);
        Assert.NotNull(options.GlobalLimiter);
    }

    [Fact]
    public void Slug_ExceedingWindow_DeniesLease_PerSlugCaseInsensitive()
    {
        using var limiter = PartitionedRateLimiter.Create<HttpContext, string>(c => PublicShowcaseRateLimits.SlugPartition(c, permitLimit: 2));

        Assert.True(limiter.AttemptAcquire(Context("/public/showcases/loja-a", "loja-a")).IsAcquired);
        Assert.True(limiter.AttemptAcquire(Context("/public/showcases/LOJA-A", "LOJA-A")).IsAcquired);
        Assert.False(limiter.AttemptAcquire(Context("/public/showcases/loja-a", "loja-a")).IsAcquired);
        Assert.True(limiter.AttemptAcquire(Context("/public/showcases/loja-b", "loja-b")).IsAcquired);
    }

    [Fact]
    public void Global_LimitsByRemoteIp_OnlyOnPublicPaths_IgnoringForwardedFor()
    {
        using var limiter = PartitionedRateLimiter.Create<HttpContext, string>(c => PublicShowcaseRateLimits.GlobalPartition(c, permitLimit: 1));

        Assert.True(limiter.AttemptAcquire(Context("/public/showcases/loja-a", ip: "10.0.0.1")).IsAcquired);

        var spoofed = Context("/public/showcases/loja-b", ip: "10.0.0.1");
        spoofed.Request.Headers["X-Forwarded-For"] = "203.0.113.9";
        Assert.False(limiter.AttemptAcquire(spoofed).IsAcquired);

        Assert.True(limiter.AttemptAcquire(Context("/public/showcases/loja-a", ip: "10.0.0.2")).IsAcquired);

        for (var i = 0; i < 50; i++)
            Assert.True(limiter.AttemptAcquire(Context("/products", ip: "10.0.0.1")).IsAcquired);
    }

    [Fact]
    public void Defaults_MatchContract()
    {
        Assert.Equal(120, PublicShowcaseRateLimits.PerSlugPermitLimit);
        Assert.Equal(1200, PublicShowcaseRateLimits.GlobalPermitLimit);
        Assert.Equal(TimeSpan.FromMinutes(1), PublicShowcaseRateLimits.Window);
    }
}

/// <summary>
/// Controllers reais num Kestrel local com servicos stub: rota com constraint, JSON exato, headers de imagem,
/// 500 generico e 429 do rate limit.
/// </summary>
public sealed class ShowcaseEndpointsTests : IAsyncLifetime
{
    private static readonly byte[] Png = { 0x89, 0x50, 0x4E, 0x47, 0x0D, 0x0A, 0x1A, 0x0A, 0x00 };
    private static readonly byte[] Jpeg = { 0xFF, 0xD8, 0xFF, 0xE0 };

    private readonly StubPublicShowcaseService _public = new();
    private WebApplication _app = default!;
    private HttpClient _client = default!;

    public async Task InitializeAsync()
    {
        var builder = WebApplication.CreateBuilder();
        builder.Logging.ClearProviders();
        builder.WebHost.UseUrls("http://127.0.0.1:0");

        builder.Services.AddControllers().AddApplicationPart(typeof(PublicShowcasesController).Assembly);
        builder.Services
            .AddAuthentication(TestAuthHandler.SchemeName)
            .AddScheme<AuthenticationSchemeOptions, TestAuthHandler>(TestAuthHandler.SchemeName, _ => { });
        builder.Services.AddAuthorization();
        builder.Services.AddRateLimiter(PublicShowcaseRateLimits.Configure);
        builder.Services.AddSingleton<IPublicShowcaseService>(_public);
        builder.Services.AddSingleton<IShowcaseService, StubShowcaseService>();

        _app = builder.Build();
        _app.UseMiddleware<ExceptionHandlingMiddleware>();
        _app.UseRouting();
        _app.UseRateLimiter();
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

    private Task<HttpResponseMessage> GetAsync(string url, string? role = null)
    {
        var request = new HttpRequestMessage(HttpMethod.Get, url);

        if (role is not null)
        {
            request.Headers.Add(TestAuthHandler.UserHeader, Guid.NewGuid().ToString());
            request.Headers.Add(TestAuthHandler.RoleHeader, role);
        }

        return _client.SendAsync(request);
    }

    [Fact]
    public async Task Unavailable_IsExactlyAvailableFalse_WithPublicCache()
    {
        var response = await GetAsync("/public/showcases/indisponivel");

        Assert.Equal(HttpStatusCode.OK, response.StatusCode);
        Assert.Equal("{\"available\":false}", await response.Content.ReadAsStringAsync());
        Assert.Equal("public, max-age=60", response.Headers.NonValidated["Cache-Control"].ToString());
    }

    [Fact]
    public async Task Available_SerializesConcreteTypeFields()
    {
        var response = await GetAsync("/public/showcases/minha-loja");

        using var json = JsonDocument.Parse(await response.Content.ReadAsStringAsync());
        Assert.Equal(
            new[] { "available", "contacts", "hasLogo", "items", "showBranding", "storeName" },
            json.RootElement.EnumerateObject().Select(p => p.Name).OrderBy(n => n, StringComparer.Ordinal));
    }

    [Fact]
    public async Task UnknownSlug_404WithMessage()
    {
        var response = await GetAsync("/public/showcases/nao-existe");

        Assert.Equal(HttpStatusCode.NotFound, response.StatusCode);
        Assert.Equal("{\"message\":\"Vitrine nao encontrada.\"}", await response.Content.ReadAsStringAsync());
    }

    [Theory]
    [InlineData("/public/showcases/%2e%2e")]
    [InlineData("/public/showcases/..%2Fetc")]
    [InlineData("/public/showcases/minha%20loja")]
    [InlineData("/public/showcases/loj%C3%A3")]
    [InlineData("/public/showcases/ab")]
    [InlineData("/public/showcases/minha_loja")]
    [InlineData("/public/showcases/-loja")]
    [InlineData("/public/showcases/aaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaa")]
    [InlineData("/public/showcases/%2e%2e/logo")]
    [InlineData("/public/showcases/minha-loja/items/nao-e-guid/photo")]
    public async Task SlugOutsidePattern_404_WithoutCallingService(string url)
    {
        var response = await GetAsync(url);

        Assert.Equal(HttpStatusCode.NotFound, response.StatusCode);
        Assert.Equal(0, _public.Calls);
    }

    [Fact]
    public async Task Images_DetectedType_Nosniff_PublicCache()
    {
        var logo = await GetAsync("/public/showcases/minha-loja/logo");
        var photo = await GetAsync($"/public/showcases/minha-loja/items/{Guid.NewGuid()}/photo");

        Assert.Equal("image/png", logo.Content.Headers.ContentType!.MediaType);
        Assert.Equal(Png, await logo.Content.ReadAsByteArrayAsync());
        Assert.Equal("image/jpeg", photo.Content.Headers.ContentType!.MediaType);

        foreach (var response in new[] { logo, photo })
        {
            Assert.Equal(HttpStatusCode.OK, response.StatusCode);
            Assert.Equal("nosniff", response.Headers.GetValues("X-Content-Type-Options").Single());
            Assert.Equal("public, max-age=300", response.Headers.NonValidated["Cache-Control"].ToString());
        }
    }

    [Fact]
    public async Task Image_Unavailable_404()
    {
        var response = await GetAsync("/public/showcases/sem-logo/logo");

        Assert.Equal(HttpStatusCode.NotFound, response.StatusCode);
    }

    [Fact]
    public async Task UnhandledException_Generic500_WithoutDetails()
    {
        var response = await GetAsync("/public/showcases/explode");

        Assert.Equal(HttpStatusCode.InternalServerError, response.StatusCode);
        Assert.Equal("{\"message\":\"Erro interno.\"}", await response.Content.ReadAsStringAsync());
    }

    [Fact]
    public async Task RateLimit_PerSlug_Returns429AfterWindowIsExhausted()
    {
        for (var i = 0; i < PublicShowcaseRateLimits.PerSlugPermitLimit; i++)
            Assert.Equal(HttpStatusCode.OK, (await GetAsync("/public/showcases/minha-loja")).StatusCode);

        Assert.Equal(HttpStatusCode.TooManyRequests, (await GetAsync("/public/showcases/minha-loja")).StatusCode);
        Assert.Equal(HttpStatusCode.OK, (await GetAsync("/public/showcases/outra-loja")).StatusCode);
    }

    [Fact]
    public async Task Owner_WithoutShowcase_IsExactlyExistsFalse()
    {
        var response = await GetAsync("/showcase", "User");

        Assert.Equal(HttpStatusCode.OK, response.StatusCode);
        Assert.Equal("{\"exists\":false,\"planAllows\":true}", await response.Content.ReadAsStringAsync());
    }

    [Fact]
    public async Task Owner_Anonymous_401()
    {
        Assert.Equal(HttpStatusCode.Unauthorized, (await GetAsync("/showcase")).StatusCode);
        Assert.Equal(HttpStatusCode.Unauthorized, (await GetAsync($"/showcase/products/{Guid.NewGuid()}/photo")).StatusCode);
    }

    [Fact]
    public async Task Owner_Photo_PrivateCache_Nosniff()
    {
        var response = await GetAsync($"/showcase/products/{Guid.NewGuid()}/photo", "User");

        Assert.Equal(HttpStatusCode.OK, response.StatusCode);
        Assert.Equal("image/png", response.Content.Headers.ContentType!.MediaType);
        Assert.Equal("nosniff", response.Headers.GetValues("X-Content-Type-Options").Single());
        Assert.Equal("private, max-age=300", response.Headers.NonValidated["Cache-Control"].ToString());
    }

    [Theory]
    [InlineData("User", HttpStatusCode.Forbidden)]
    [InlineData("Admin", HttpStatusCode.OK)]
    public async Task Admin_Route_OnlyAdmin(string role, HttpStatusCode expected)
    {
        Assert.Equal(expected, (await GetAsync("/admin/showcases", role)).StatusCode);
    }

    private sealed class StubPublicShowcaseService : IPublicShowcaseService
    {
        private int _calls;

        public int Calls => _calls;

        public Task<PublicShowcaseResponse> GetAsync(string slug, CancellationToken cancellationToken = default)
        {
            Interlocked.Increment(ref _calls);

            return slug switch
            {
                "indisponivel" => Task.FromResult<PublicShowcaseResponse>(new PublicShowcaseUnavailableDto()),
                "nao-existe" => throw new Domain.Exceptions.NotFoundException("Vitrine nao encontrada."),
                "explode" => throw new InvalidOperationException("detalhe interno que nao pode vazar"),
                _ => Task.FromResult<PublicShowcaseResponse>(new PublicShowcaseDto(
                    "Loja", false, true, new PublicShowcaseContactsDto(null, null, null, null, null), new List<PublicShowcaseItemDto>()))
            };
        }

        public Task<ImageContent?> GetLogoAsync(string slug, CancellationToken cancellationToken = default)
        {
            Interlocked.Increment(ref _calls);
            return Task.FromResult(slug == "sem-logo" ? null : new ImageContent(Png, "image/png"));
        }

        public Task<ImageContent?> GetItemPhotoAsync(string slug, Guid productId, CancellationToken cancellationToken = default)
        {
            Interlocked.Increment(ref _calls);
            return Task.FromResult<ImageContent?>(new ImageContent(Jpeg, "image/jpeg"));
        }
    }

    private sealed class StubShowcaseService : IShowcaseService
    {
        public Task<ShowcaseStateDto> GetAsync(Guid userId, CancellationToken cancellationToken = default) =>
            Task.FromResult<ShowcaseStateDto>(new NoShowcaseDto(true));

        public Task<ImageContent?> GetProductPhotoAsync(Guid userId, Guid productId, CancellationToken cancellationToken = default) =>
            Task.FromResult<ImageContent?>(new ImageContent(Png, "image/png"));

        public Task<List<AdminShowcaseDto>> GetAllForAdminAsync(CancellationToken cancellationToken = default) =>
            Task.FromResult(new List<AdminShowcaseDto>());

        public Task<ShowcaseDto> CreateAsync(Guid userId, CancellationToken cancellationToken = default) => throw new NotSupportedException();
        public Task<ShowcaseDto> SetPublishedAsync(Guid userId, bool isPublished, CancellationToken cancellationToken = default) => throw new NotSupportedException();
        public Task<ShowcaseDto> AddItemsAsync(Guid userId, IReadOnlyList<Guid>? productIds, CancellationToken cancellationToken = default) => throw new NotSupportedException();
        public Task<ShowcaseDto> ReorderItemsAsync(Guid userId, IReadOnlyList<Guid>? productIds, CancellationToken cancellationToken = default) => throw new NotSupportedException();
        public Task<ShowcaseDto> SetItemVisibilityAsync(Guid userId, Guid productId, bool isVisible, CancellationToken cancellationToken = default) => throw new NotSupportedException();
        public Task RemoveItemAsync(Guid userId, Guid productId, CancellationToken cancellationToken = default) => throw new NotSupportedException();
        public Task<List<AvailableProductDto>> GetAvailableProductsAsync(Guid userId, string? search, CancellationToken cancellationToken = default) => throw new NotSupportedException();
        public Task<AdminShowcaseDto> SetBlockedAsync(Guid showcaseId, bool isBlocked, CancellationToken cancellationToken = default) => throw new NotSupportedException();
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
}
