using System.Security.Claims;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Printly.Application.DTOs.Plans;
using Printly.Application.Interfaces;

namespace Printly.WebApi.Controllers;

/// <summary>Plano do usuario logado (qualquer usuario). Separado de <see cref="PlansController"/>, que e admin-only.</summary>
[ApiController]
[Route("plans/me")]
[Authorize]
public class MyPlanController : ControllerBase
{
    private readonly IEntitlementService _entitlementService;

    public MyPlanController(IEntitlementService entitlementService)
    {
        _entitlementService = entitlementService;
    }

    [HttpGet]
    public async Task<ActionResult<MyPlanDto>> Get(CancellationToken cancellationToken)
    {
        var myPlan = await _entitlementService.GetMyPlanAsync(GetUserId(), cancellationToken);
        return Ok(myPlan);
    }

    private Guid GetUserId() =>
        Guid.Parse(User.FindFirstValue(ClaimTypes.NameIdentifier) ?? User.FindFirstValue("sub")!);
}
