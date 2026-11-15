using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Printly.Application.DTOs.PrinterModels;
using Printly.Application.Interfaces;
using Printly.WebApi.Models.PrinterModels;

namespace Printly.WebApi.Controllers;

/// <summary>Catalogo de modelos: leitura pra qualquer usuario logado (escolher a impressora), escrita so admin.</summary>
[ApiController]
[Route("printer-models")]
[Authorize]
public class PrinterModelsController : ControllerBase
{
    private readonly IPrinterModelService _printerModelService;

    public PrinterModelsController(IPrinterModelService printerModelService)
    {
        _printerModelService = printerModelService;
    }

    [HttpGet]
    public async Task<ActionResult<List<PrinterModelDto>>> GetAll(CancellationToken cancellationToken)
    {
        var printerModels = await _printerModelService.GetAllAsync(cancellationToken);
        return Ok(printerModels);
    }

    [HttpPost]
    [Authorize(Roles = "Admin")]
    public async Task<ActionResult<PrinterModelDto>> Create([FromForm] SavePrinterModelForm form, CancellationToken cancellationToken)
    {
        var printerModel = await _printerModelService.CreateAsync(await ToRequestAsync(form, cancellationToken), cancellationToken);
        return CreatedAtAction(nameof(GetAll), new { }, printerModel);
    }

    [HttpPut("{id:guid}")]
    [Authorize(Roles = "Admin")]
    public async Task<ActionResult<PrinterModelDto>> Update(Guid id, [FromForm] SavePrinterModelForm form, CancellationToken cancellationToken)
    {
        var printerModel = await _printerModelService.UpdateAsync(id, await ToRequestAsync(form, cancellationToken), cancellationToken);
        return Ok(printerModel);
    }

    private static async Task<SavePrinterModelRequest> ToRequestAsync(SavePrinterModelForm form, CancellationToken cancellationToken)
    {
        string? imageBase64 = null;

        if (form.Image is not null)
        {
            using var memoryStream = new MemoryStream();
            await form.Image.CopyToAsync(memoryStream, cancellationToken);
            imageBase64 = Convert.ToBase64String(memoryStream.ToArray());
        }

        return new SavePrinterModelRequest(form.Name, form.BrandId, imageBase64, form.IsActive);
    }
}
