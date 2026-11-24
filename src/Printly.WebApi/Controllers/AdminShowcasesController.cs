using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Printly.Application.DTOs.Showcases;
using Printly.Application.Interfaces;

namespace Printly.WebApi.Controllers;

/// <summary>Moderacao das vitrines: listar e bloquear (kill-switch da pagina publica).</summary>
[ApiController]
[Route("admin/showcases")]
[Authorize(Roles = "Admin")]
public class AdminShowcasesController : ControllerBase
{
    private readonly IShowcaseService _showcaseService;

    public AdminShowcasesController(IShowcaseService showcaseService)
    {
        _showcaseService = showcaseService;
    }

    [HttpGet]
    public async Task<ActionResult<List<AdminShowcaseDto>>> GetAll(CancellationToken cancellationToken) =>
        Ok(await _showcaseService.GetAllForAdminAsync(cancellationToken));

    [HttpPatch("{id:guid}/block")]
    public async Task<ActionResult<AdminShowcaseDto>> SetBlocked(Guid id, SetShowcaseBlockedRequest request, CancellationToken cancellationToken) =>
        Ok(await _showcaseService.SetBlockedAsync(id, request.IsBlocked, cancellationToken));
}
