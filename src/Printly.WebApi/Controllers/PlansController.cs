using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Printly.Application.DTOs.Plans;
using Printly.Application.Interfaces;

namespace Printly.WebApi.Controllers;

/// <summary>
/// Administracao de planos (admin-only). O GET /plans/me, aberto a qualquer logado, fica em
/// <see cref="MyPlanController"/>: [Authorize] no metodo nao relaxa o Roles da classe, e [AllowAnonymous]
/// desligaria a autenticacao inteira.
/// </summary>
[ApiController]
[Route("plans")]
[Authorize(Roles = "Admin")]
public class PlansController : ControllerBase
{
    private readonly IPlanService _planService;

    public PlansController(IPlanService planService)
    {
        _planService = planService;
    }

    [HttpGet("entitlement-catalog")]
    public ActionResult<List<EntitlementDefinitionDto>> GetCatalog() => Ok(_planService.GetCatalog());

    [HttpGet]
    public async Task<ActionResult<List<PlanDto>>> GetAll(CancellationToken cancellationToken)
    {
        var plans = await _planService.GetAllAsync(cancellationToken);
        return Ok(plans);
    }

    [HttpGet("{id:guid}")]
    public async Task<ActionResult<PlanDto>> GetById(Guid id, CancellationToken cancellationToken)
    {
        var plan = await _planService.GetByIdAsync(id, cancellationToken);
        return Ok(plan);
    }

    [HttpPost]
    public async Task<ActionResult<PlanDto>> Create(SavePlanRequest request, CancellationToken cancellationToken)
    {
        var plan = await _planService.CreateAsync(request, cancellationToken);
        return CreatedAtAction(nameof(GetById), new { id = plan.Id }, plan);
    }

    [HttpPut("{id:guid}")]
    public async Task<ActionResult<PlanDto>> Update(Guid id, SavePlanRequest request, CancellationToken cancellationToken)
    {
        var plan = await _planService.UpdateAsync(id, request, cancellationToken);
        return Ok(plan);
    }

    [HttpDelete("{id:guid}")]
    public async Task<IActionResult> Delete(Guid id, CancellationToken cancellationToken)
    {
        await _planService.DeleteAsync(id, cancellationToken);
        return NoContent();
    }
}
