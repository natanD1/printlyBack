using System.Security.Claims;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Printly.Application.DTOs.Printers;
using Printly.Application.Interfaces;

namespace Printly.WebApi.Controllers;

[ApiController]
[Route("printers")]
[Authorize]
public class PrintersController : ControllerBase
{
    private readonly IPrinterService _printerService;

    public PrintersController(IPrinterService printerService)
    {
        _printerService = printerService;
    }

    [HttpGet]
    public async Task<ActionResult<List<PrinterDto>>> GetAll(CancellationToken cancellationToken)
    {
        var printers = await _printerService.GetAllAsync(GetUserId(), cancellationToken);
        return Ok(printers);
    }

    [HttpPost]
    public async Task<ActionResult<PrinterDto>> Create(SavePrinterRequest request, CancellationToken cancellationToken)
    {
        var printer = await _printerService.CreateAsync(request, GetUserId(), cancellationToken);
        return CreatedAtAction(nameof(GetAll), new { }, printer);
    }

    [HttpPut("{id:guid}")]
    public async Task<ActionResult<PrinterDto>> Update(Guid id, SavePrinterRequest request, CancellationToken cancellationToken)
    {
        var printer = await _printerService.UpdateAsync(id, request, GetUserId(), cancellationToken);
        return Ok(printer);
    }

    [HttpDelete("{id:guid}")]
    public async Task<IActionResult> Delete(Guid id, CancellationToken cancellationToken)
    {
        await _printerService.DeleteAsync(id, GetUserId(), cancellationToken);
        return NoContent();
    }

    private Guid GetUserId() =>
        Guid.Parse(User.FindFirstValue(ClaimTypes.NameIdentifier) ?? User.FindFirstValue("sub")!);
}
