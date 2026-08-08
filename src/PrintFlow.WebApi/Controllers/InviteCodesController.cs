using System.Security.Claims;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using PrintFlow.Application.DTOs.InviteCodes;
using PrintFlow.Application.Interfaces;

namespace PrintFlow.WebApi.Controllers;

[ApiController]
[Route("invite-codes")]
[Authorize(Roles = "Admin")]
public class InviteCodesController : ControllerBase
{
    private readonly IInviteCodeService _inviteCodeService;

    public InviteCodesController(IInviteCodeService inviteCodeService)
    {
        _inviteCodeService = inviteCodeService;
    }

    [HttpGet]
    public async Task<ActionResult<List<InviteCodeDto>>> GetAll(CancellationToken cancellationToken)
    {
        var inviteCodes = await _inviteCodeService.GetAllAsync(cancellationToken);
        return Ok(inviteCodes);
    }

    [HttpPost]
    public async Task<ActionResult<InviteCodeDto>> Generate(GenerateInviteCodeRequest request, CancellationToken cancellationToken)
    {
        var userId = Guid.Parse(User.FindFirstValue(ClaimTypes.NameIdentifier) ?? User.FindFirstValue("sub")!);
        var inviteCode = await _inviteCodeService.GenerateAsync(request, userId, cancellationToken);
        return CreatedAtAction(nameof(GetAll), new { }, inviteCode);
    }

    [HttpDelete("{id:guid}")]
    public async Task<IActionResult> Revoke(Guid id, CancellationToken cancellationToken)
    {
        await _inviteCodeService.RevokeAsync(id, cancellationToken);
        return NoContent();
    }
}
