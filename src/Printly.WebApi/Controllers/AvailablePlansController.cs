using System.Security.Claims;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Printly.Application.DTOs.Plans;
using Printly.Application.Interfaces;

namespace Printly.WebApi.Controllers;

/// <summary>
/// Planos disponiveis para o usuario logado (qualquer usuario). Separado de <see cref="PlansController"/>, que e
/// admin-only, pelo mesmo motivo do <see cref="MyPlanController"/>.
/// </summary>
[ApiController]
[Route("plans/available")]
[Authorize]
public class AvailablePlansController : ControllerBase
{
    private readonly IPlanService _planService;

    public AvailablePlansController(IPlanService planService)
    {
        _planService = planService;
    }

    [HttpGet]
    public async Task<ActionResult<AvailablePlansDto>> Get(CancellationToken cancellationToken)
    {
        var available = await _planService.GetAvailablePlansAsync(GetUserId(), cancellationToken);
        return Ok(available);
    }

    private Guid GetUserId() =>
        Guid.Parse(User.FindFirstValue(ClaimTypes.NameIdentifier) ?? User.FindFirstValue("sub")!);
}
