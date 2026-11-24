using System.Security.Claims;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Printly.Application.DTOs.Showcases;
using Printly.Application.Interfaces;
using Printly.WebApi.Extensions;

namespace Printly.WebApi.Controllers;

/// <summary>Vitrine do usuario logado. Escritas exigem ShowcaseEnabled (403 com code); GET e DELETE de item nao.</summary>
[ApiController]
[Route("showcase")]
[Authorize]
public class ShowcaseController : ControllerBase
{
    private readonly IShowcaseService _showcaseService;

    public ShowcaseController(IShowcaseService showcaseService)
    {
        _showcaseService = showcaseService;
    }

    /// <summary>Sempre 200: <c>{ exists: false, planAllows }</c> ou a vitrine completa (serializada pelo tipo concreto).</summary>
    [HttpGet]
    public async Task<IActionResult> Get(CancellationToken cancellationToken)
    {
        object showcase = await _showcaseService.GetAsync(GetUserId(), cancellationToken);
        return Ok(showcase);
    }

    [HttpPost]
    public async Task<ActionResult<ShowcaseDto>> Create(CancellationToken cancellationToken) =>
        Ok(await _showcaseService.CreateAsync(GetUserId(), cancellationToken));

    [HttpPatch("published")]
    public async Task<ActionResult<ShowcaseDto>> SetPublished(SetShowcasePublishedRequest request, CancellationToken cancellationToken) =>
        Ok(await _showcaseService.SetPublishedAsync(GetUserId(), request.IsPublished, cancellationToken));

    [HttpPost("items")]
    public async Task<ActionResult<ShowcaseDto>> AddItems(ShowcaseProductIdsRequest request, CancellationToken cancellationToken) =>
        Ok(await _showcaseService.AddItemsAsync(GetUserId(), request.ProductIds, cancellationToken));

    [HttpPut("items/order")]
    public async Task<ActionResult<ShowcaseDto>> ReorderItems(ShowcaseProductIdsRequest request, CancellationToken cancellationToken) =>
        Ok(await _showcaseService.ReorderItemsAsync(GetUserId(), request.ProductIds, cancellationToken));

    [HttpPatch("items/{productId:guid}/visibility")]
    public async Task<ActionResult<ShowcaseDto>> SetItemVisibility(Guid productId, SetShowcaseItemVisibilityRequest request, CancellationToken cancellationToken) =>
        Ok(await _showcaseService.SetItemVisibilityAsync(GetUserId(), productId, request.IsVisible, cancellationToken));

    [HttpDelete("items/{productId:guid}")]
    public async Task<IActionResult> RemoveItem(Guid productId, CancellationToken cancellationToken)
    {
        await _showcaseService.RemoveItemAsync(GetUserId(), productId, cancellationToken);
        return NoContent();
    }

    [HttpGet("available-products")]
    public async Task<ActionResult<List<AvailableProductDto>>> GetAvailableProducts([FromQuery] string? search, CancellationToken cancellationToken) =>
        Ok(await _showcaseService.GetAvailableProductsAsync(GetUserId(), search, cancellationToken));

    /// <summary>Miniatura de produto do proprio usuario (o front carrega via axios, com refresh do token).</summary>
    [HttpGet("products/{productId:guid}/photo")]
    public async Task<IActionResult> GetProductPhoto(Guid productId, CancellationToken cancellationToken)
    {
        var image = await _showcaseService.GetProductPhotoAsync(GetUserId(), productId, cancellationToken);
        return ImageResults.From(this, image, "private, max-age=300");
    }

    private Guid GetUserId() =>
        Guid.Parse(User.FindFirstValue(ClaimTypes.NameIdentifier) ?? User.FindFirstValue("sub")!);
}
