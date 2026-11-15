using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Printly.Application.DTOs.FilamentTypes;
using Printly.Application.Interfaces;

namespace Printly.WebApi.Controllers;

/// <summary>Leitura pra qualquer usuario logado (cadastro de filamento usa a lista), escrita so admin.</summary>
[ApiController]
[Route("filaments-type")]
[Authorize]
public class FilamentTypesController : ControllerBase
{
    private readonly IFilamentTypeService _filamentTypeService;

    public FilamentTypesController(IFilamentTypeService filamentTypeService)
    {
        _filamentTypeService = filamentTypeService;
    }

    [HttpGet]
    public async Task<ActionResult<List<FilamentTypeDto>>> GetAll(CancellationToken cancellationToken)
    {
        var filamentTypes = await _filamentTypeService.GetAllAsync(cancellationToken);
        return Ok(filamentTypes);
    }

    [HttpPost]
    [Authorize(Roles = "Admin")]
    public async Task<ActionResult<FilamentTypeDto>> Create(CreateFilamentTypeRequest request, CancellationToken cancellationToken)
    {
        var filamentType = await _filamentTypeService.CreateAsync(request, cancellationToken);
        return CreatedAtAction(nameof(GetAll), new { }, filamentType);
    }

    [HttpPut("{id:guid}")]
    [Authorize(Roles = "Admin")]
    public async Task<ActionResult<FilamentTypeDto>> Update(Guid id, UpdateFilamentTypeRequest request, CancellationToken cancellationToken)
    {
        var filamentType = await _filamentTypeService.UpdateAsync(id, request, cancellationToken);
        return Ok(filamentType);
    }
}
