using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Printly.Application.DTOs.Users;
using Printly.Application.Interfaces;

namespace Printly.WebApi.Controllers;

[ApiController]
[Route("users")]
[Authorize(Roles = "Admin")]
public class UsersController : ControllerBase
{
    private readonly IUserManagementService _userManagementService;
    private readonly IPlanService _planService;

    public UsersController(IUserManagementService userManagementService, IPlanService planService)
    {
        _userManagementService = userManagementService;
        _planService = planService;
    }

    [HttpGet]
    public async Task<ActionResult<List<ManagedUserDto>>> GetAll(CancellationToken cancellationToken)
    {
        var users = await _userManagementService.GetAllAsync(cancellationToken);
        return Ok(users);
    }

    /// <summary>Troca de plano passa sempre por IPlanService.ChangeUserPlanAsync (valida plano existente e ativo).</summary>
    [HttpPatch("{id:guid}/plan")]
    public async Task<ActionResult<ManagedUserDto>> ChangePlan(Guid id, ChangeUserPlanRequest request, CancellationToken cancellationToken)
    {
        var user = await _planService.ChangeUserPlanAsync(id, request.PlanId, cancellationToken);
        return Ok(user);
    }
}
