using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.RateLimiting;
using Printly.Application.Interfaces;
using Printly.WebApi.Extensions;

namespace Printly.WebApi.Controllers;

/// <summary>
/// Vitrine publica, anonima. Slug fora do padrao nem casa a rota (404 sem tocar no banco); o servico ainda
/// confere maiusculas, ja que o constraint regex do ASP.NET ignora caixa.
/// </summary>
[ApiController]
[Route("public/showcases")]
[AllowAnonymous]
[EnableRateLimiting(PublicShowcaseRateLimits.PolicyName)]
public class PublicShowcasesController : ControllerBase
{
    /// <summary><c>[[</c>/<c>]]</c> escapam os colchetes do token replacement das rotas de atributo.</summary>
    public const string SlugRoute = "{slug:regex(^[[a-z0-9]]+(-[[a-z0-9]]+)*$):minlength(3):maxlength(60)}";

    private const string ImageCacheControl = "public, max-age=300";

    private readonly IPublicShowcaseService _publicShowcaseService;

    public PublicShowcasesController(IPublicShowcaseService publicShowcaseService)
    {
        _publicShowcaseService = publicShowcaseService;
    }

    /// <summary>200 com a vitrine ou exatamente <c>{ "available": false }</c>; 404 se o slug nao existe.</summary>
    [HttpGet(SlugRoute)]
    public async Task<IActionResult> Get(string slug, CancellationToken cancellationToken)
    {
        object showcase = await _publicShowcaseService.GetAsync(slug, cancellationToken);

        Response.Headers.CacheControl = "public, max-age=60";
        return Ok(showcase);
    }

    [HttpGet(SlugRoute + "/logo")]
    public async Task<IActionResult> GetLogo(string slug, CancellationToken cancellationToken) =>
        ImageResults.From(this, await _publicShowcaseService.GetLogoAsync(slug, cancellationToken), ImageCacheControl);

    [HttpGet(SlugRoute + "/items/{productId:guid}/photo")]
    public async Task<IActionResult> GetItemPhoto(string slug, Guid productId, CancellationToken cancellationToken) =>
        ImageResults.From(this, await _publicShowcaseService.GetItemPhotoAsync(slug, productId, cancellationToken), ImageCacheControl);
}
