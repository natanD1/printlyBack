using System.Security.Claims;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Printly.Application.DTOs.Products;
using Printly.Application.Interfaces;
using Printly.WebApi.Models.Products;

namespace Printly.WebApi.Controllers;

[ApiController]
[Route("products")]
[Authorize]
public class ProductsController : ControllerBase
{
    private readonly IProductService _productService;

    public ProductsController(IProductService productService)
    {
        _productService = productService;
    }

    [HttpGet]
    public async Task<ActionResult<List<ProductDto>>> GetAll(CancellationToken cancellationToken)
    {
        var products = await _productService.GetAllAsync(GetUserId(), cancellationToken);
        return Ok(products);
    }

    [HttpGet("{id:guid}")]
    public async Task<ActionResult<ProductDto>> GetById(Guid id, CancellationToken cancellationToken)
    {
        var product = await _productService.GetByIdAsync(id, GetUserId(), cancellationToken);
        return Ok(product);
    }

    [HttpPost]
    public async Task<ActionResult<ProductDto>> Create([FromForm] CreateProductForm form, CancellationToken cancellationToken)
    {
        var productPhotoBase64 = await ToBase64Async(form.ProductPhoto, cancellationToken);

        var request = new CreateProductRequest(
            form.ProductName,
            productPhotoBase64,
            form.TotalHours,
            form.SalePriceOverride,
            ToFilamentRequests(form.Filaments));

        var product = await _productService.CreateAsync(request, GetUserId(), cancellationToken);
        return CreatedAtAction(nameof(GetById), new { id = product.Id }, product);
    }

    [HttpPut("{id:guid}")]
    public async Task<ActionResult<ProductDto>> Update(Guid id, [FromForm] UpdateProductForm form, CancellationToken cancellationToken)
    {
        var productPhotoBase64 = await ToBase64Async(form.ProductPhoto, cancellationToken);

        var hasFilamentsField = Request.Form.Keys.Any(key => key.StartsWith("Filaments", StringComparison.Ordinal));
        var filaments = hasFilamentsField ? ToFilamentRequests(form.Filaments) : null;

        var request = new UpdateProductRequest(
            form.ProductName,
            productPhotoBase64,
            form.TotalHours,
            form.SalePriceOverride,
            filaments);

        var product = await _productService.UpdateAsync(id, request, GetUserId(), cancellationToken);
        return Ok(product);
    }

    [HttpDelete("{id:guid}")]
    public async Task<IActionResult> Delete(Guid id, CancellationToken cancellationToken)
    {
        await _productService.DeleteAsync(id, GetUserId(), cancellationToken);
        return NoContent();
    }

    private Guid GetUserId() =>
        Guid.Parse(User.FindFirstValue(ClaimTypes.NameIdentifier) ?? User.FindFirstValue("sub")!);

    private static async Task<string?> ToBase64Async(IFormFile? file, CancellationToken cancellationToken)
    {
        if (file is null)
            return null;

        using var memoryStream = new MemoryStream();
        await file.CopyToAsync(memoryStream, cancellationToken);
        return Convert.ToBase64String(memoryStream.ToArray());
    }

    private static List<ProductFilamentRequest> ToFilamentRequests(List<ProductFilamentFormItem> items) =>
        items.Select(item => new ProductFilamentRequest(item.FilamentId, item.GramsUsed)).ToList();
}
