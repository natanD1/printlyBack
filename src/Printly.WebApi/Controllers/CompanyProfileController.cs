using System.Security.Claims;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Printly.Application.DTOs.CompanyProfile;
using Printly.Application.Interfaces;
using Printly.WebApi.Models.CompanyProfile;

namespace Printly.WebApi.Controllers;

[ApiController]
[Route("company-profile")]
[Authorize]
public class CompanyProfileController : ControllerBase
{
    private readonly ICompanyProfileService _companyProfileService;

    public CompanyProfileController(ICompanyProfileService companyProfileService)
    {
        _companyProfileService = companyProfileService;
    }

    /// <summary>200 com o perfil ou 200 com corpo <c>null</c> (sem perfil). JsonResult evita o 204 padrao do ASP.NET pra null.</summary>
    [HttpGet]
    public async Task<IActionResult> Get(CancellationToken cancellationToken)
    {
        var companyProfile = await _companyProfileService.GetAsync(GetUserId(), cancellationToken);
        return new JsonResult(companyProfile);
    }

    [HttpPut]
    public async Task<ActionResult<CompanyProfileDto>> Save([FromForm] SaveCompanyProfileForm form, CancellationToken cancellationToken)
    {
        var companyProfile = await _companyProfileService.SaveAsync(GetUserId(), await ToRequestAsync(form, cancellationToken), cancellationToken);
        return Ok(companyProfile);
    }

    private static async Task<SaveCompanyProfileRequest> ToRequestAsync(SaveCompanyProfileForm form, CancellationToken cancellationToken)
    {
        string? logoBase64 = null;

        if (form.Logo is not null)
        {
            using var memoryStream = new MemoryStream();
            await form.Logo.CopyToAsync(memoryStream, cancellationToken);
            logoBase64 = Convert.ToBase64String(memoryStream.ToArray());
        }

        return new SaveCompanyProfileRequest(
            form.Name,
            logoBase64,
            form.RemoveLogo,
            form.Phone,
            form.Email,
            form.WhatsApp,
            form.Instagram,
            form.Website);
    }

    private Guid GetUserId() =>
        Guid.Parse(User.FindFirstValue(ClaimTypes.NameIdentifier) ?? User.FindFirstValue("sub")!);
}
