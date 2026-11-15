using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Printly.Application.DTOs.PrinterBrands;
using Printly.Application.Interfaces;

namespace Printly.WebApi.Controllers;

/// <summary>Catalogo de marcas: leitura pra qualquer usuario logado (escolher a impressora), escrita so admin.</summary>
[ApiController]
[Route("printer-brands")]
[Authorize]
public class PrinterBrandsController : ControllerBase
{
    private readonly IPrinterBrandService _printerBrandService;

    public PrinterBrandsController(IPrinterBrandService printerBrandService)
    {
        _printerBrandService = printerBrandService;
    }

    [HttpGet]
    public async Task<ActionResult<List<PrinterBrandDto>>> GetAll(CancellationToken cancellationToken)
    {
        var printerBrands = await _printerBrandService.GetAllAsync(cancellationToken);
        return Ok(printerBrands);
    }

    [HttpPost]
    [Authorize(Roles = "Admin")]
    public async Task<ActionResult<PrinterBrandDto>> Create(SavePrinterBrandRequest request, CancellationToken cancellationToken)
    {
        var printerBrand = await _printerBrandService.CreateAsync(request, cancellationToken);
        return CreatedAtAction(nameof(GetAll), new { }, printerBrand);
    }

    [HttpPut("{id:guid}")]
    [Authorize(Roles = "Admin")]
    public async Task<ActionResult<PrinterBrandDto>> Update(Guid id, SavePrinterBrandRequest request, CancellationToken cancellationToken)
    {
        var printerBrand = await _printerBrandService.UpdateAsync(id, request, cancellationToken);
        return Ok(printerBrand);
    }
}
