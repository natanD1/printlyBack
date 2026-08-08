using System.Security.Claims;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using PrintFlow.Application.DTOs.Filaments;
using PrintFlow.Application.Interfaces;

namespace PrintFlow.WebApi.Controllers;

[ApiController]
[Route("filaments")]
[Authorize]
public class FilamentsController : ControllerBase
{
    private readonly IFilamentService _filamentService;

    public FilamentsController(IFilamentService filamentService)
    {
        _filamentService = filamentService;
    }

    [HttpGet]
    public async Task<ActionResult<List<FilamentDto>>> GetAll(CancellationToken cancellationToken)
    {
        var filaments = await _filamentService.GetAllAsync(GetUserId(), cancellationToken);
        return Ok(filaments);
    }

    [HttpGet("{id:guid}")]
    public async Task<ActionResult<FilamentDto>> GetById(Guid id, CancellationToken cancellationToken)
    {
        var filament = await _filamentService.GetByIdAsync(id, GetUserId(), cancellationToken);
        return Ok(filament);
    }

    [HttpPost]
    public async Task<ActionResult<FilamentDto>> Create(CreateFilamentRequest request, CancellationToken cancellationToken)
    {
        var filament = await _filamentService.CreateAsync(request, GetUserId(), cancellationToken);
        return CreatedAtAction(nameof(GetById), new { id = filament.Id }, filament);
    }

    [HttpPut("{id:guid}")]
    public async Task<ActionResult<FilamentDto>> Update(Guid id, UpdateFilamentRequest request, CancellationToken cancellationToken)
    {
        var filament = await _filamentService.UpdateAsync(id, request, GetUserId(), cancellationToken);
        return Ok(filament);
    }

    [HttpDelete("{id:guid}")]
    public async Task<IActionResult> Delete(Guid id, CancellationToken cancellationToken)
    {
        await _filamentService.DeleteAsync(id, GetUserId(), cancellationToken);
        return NoContent();
    }

    private Guid GetUserId() =>
        Guid.Parse(User.FindFirstValue(ClaimTypes.NameIdentifier) ?? User.FindFirstValue("sub")!);
}
