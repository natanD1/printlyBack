using System.Threading.RateLimiting;
using Microsoft.AspNetCore.RateLimiting;

namespace Printly.WebApi.Extensions;

/// <summary>
/// Rate limit da vitrine publica (anonima). Politica <see cref="PolicyName"/>: janela fixa por slug.
/// Global: janela fixa por IP remoto so em <c>/public/</c> (o resto nao tem limite).
/// Limitacao: em producao todo trafego chega pelo servidor Next (mesmo IP), entao o global protege o backend
/// como um todo; limite por visitante real fica no proxy reverso/CDN. X-Forwarded-For nao e lido (falsificavel).
/// </summary>
public static class PublicShowcaseRateLimits
{
    public const string PolicyName = "public-showcase";
    public const string PublicPathPrefix = "/public/";

    /// <summary>O Next cacheia JSON 60 s e imagens 300 s: por slug chegam poucas chamadas/min por instancia.</summary>
    public const int PerSlugPermitLimit = 120;

    public const int GlobalPermitLimit = 1200;

    public static readonly TimeSpan Window = TimeSpan.FromMinutes(1);

    public static void Configure(RateLimiterOptions options)
    {
        options.RejectionStatusCode = StatusCodes.Status429TooManyRequests;
        options.AddPolicy(PolicyName, context => SlugPartition(context));
        options.GlobalLimiter = PartitionedRateLimiter.Create<HttpContext, string>(context => GlobalPartition(context));
    }

    /// <summary>Particao pelo route value <c>slug</c> (minusculo).</summary>
    public static RateLimitPartition<string> SlugPartition(HttpContext context, int permitLimit = PerSlugPermitLimit) =>
        RateLimitPartition.GetFixedWindowLimiter(
            "slug:" + (context.Request.RouteValues["slug"]?.ToString()?.ToLowerInvariant() ?? string.Empty),
            _ => FixedWindow(permitLimit));

    /// <summary>Por IP remoto (Connection.RemoteIpAddress) so para <c>/public/</c>; demais caminhos sem limite.</summary>
    public static RateLimitPartition<string> GlobalPartition(HttpContext context, int permitLimit = GlobalPermitLimit)
    {
        if (!context.Request.Path.StartsWithSegments("/public", StringComparison.OrdinalIgnoreCase))
            return RateLimitPartition.GetNoLimiter("unlimited");

        var ip = context.Connection.RemoteIpAddress?.ToString() ?? "unknown";
        return RateLimitPartition.GetFixedWindowLimiter("ip:" + ip, _ => FixedWindow(permitLimit));
    }

    private static FixedWindowRateLimiterOptions FixedWindow(int permitLimit) => new()
    {
        PermitLimit = permitLimit,
        Window = Window,
        QueueLimit = 0,
        QueueProcessingOrder = QueueProcessingOrder.OldestFirst,
        AutoReplenishment = true
    };
}
