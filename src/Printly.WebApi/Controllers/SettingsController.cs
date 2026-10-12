using System.Security.Claims;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Printly.Application.DTOs.Settings;
using Printly.Application.Interfaces;

namespace Printly.WebApi.Controllers;

[ApiController]
[Route("settings")]
[Authorize]
public class SettingsController : ControllerBase
{
    private readonly ISettingsService _settingsService;

    public SettingsController(ISettingsService settingsService)
    {
        _settingsService = settingsService;
    }

    [HttpGet]
    public async Task<ActionResult<SettingsDto>> Get(CancellationToken cancellationToken)
    {
        var settings = await _settingsService.GetAsync(GetUserId(), cancellationToken);
        return Ok(settings);
    }

    [HttpPut]
    public async Task<ActionResult<SettingsDto>> Update(UpdateSettingsRequest request, CancellationToken cancellationToken)
    {
        var settings = await _settingsService.UpdateAsync(GetUserId(), request, cancellationToken);
        return Ok(settings);
    }

    private Guid GetUserId() =>
        Guid.Parse(User.FindFirstValue(ClaimTypes.NameIdentifier) ?? User.FindFirstValue("sub")!);
}
