using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Printly.Application.DTOs.FilamentBrands;
using Printly.Application.Interfaces;
using Printly.WebApi.Models.FilamentBrands;

namespace Printly.WebApi.Controllers;

/// <summary>Leitura pra qualquer usuario logado (cadastro de filamento usa a lista), escrita so admin.</summary>
[ApiController]
[Route("filaments-brands")]
[Authorize]
public class FilamentBrandsController : ControllerBase
{
    private readonly IFilamentBrandService _filamentBrandService;

    public FilamentBrandsController(IFilamentBrandService filamentBrandService)
    {
        _filamentBrandService = filamentBrandService;
    }

    [HttpGet]
    public async Task<ActionResult<List<FilamentBrandDto>>> GetAll(CancellationToken cancellationToken)
    {
        var filamentBrands = await _filamentBrandService.GetAllAsync(cancellationToken);
        return Ok(filamentBrands);
    }

    [HttpPost]
    [Authorize(Roles = "Admin")]
    public async Task<ActionResult<FilamentBrandDto>> Create([FromForm] CreateFilamentBrandForm form, CancellationToken cancellationToken)
    {
        var imageBrandBase64 = await ToBase64Async(form.ImageBrand, cancellationToken);

        var request = new CreateFilamentBrandRequest(form.NameBrand, imageBrandBase64);

        var filamentBrand = await _filamentBrandService.CreateAsync(request, cancellationToken);
        return CreatedAtAction(nameof(GetAll), new { }, filamentBrand);
    }

    [HttpPut("{id:guid}")]
    [Authorize(Roles = "Admin")]
    public async Task<ActionResult<FilamentBrandDto>> Update(Guid id, [FromForm] UpdateFilamentBrandForm form, CancellationToken cancellationToken)
    {
        var imageBrandBase64 = await ToBase64Async(form.ImageBrand, cancellationToken);

        var request = new UpdateFilamentBrandRequest(form.NameBrand, imageBrandBase64, form.IsActive);

        var filamentBrand = await _filamentBrandService.UpdateAsync(id, request, cancellationToken);
        return Ok(filamentBrand);
    }

    private static async Task<string?> ToBase64Async(IFormFile? file, CancellationToken cancellationToken)
    {
        if (file is null)
            return null;

        using var memoryStream = new MemoryStream();
        await file.CopyToAsync(memoryStream, cancellationToken);
        return Convert.ToBase64String(memoryStream.ToArray());
    }
}
