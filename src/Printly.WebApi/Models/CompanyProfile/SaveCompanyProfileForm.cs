namespace Printly.WebApi.Models.CompanyProfile;

public class SaveCompanyProfileForm
{
    public string Name { get; set; } = default!;
    public string? Phone { get; set; }
    public string? Email { get; set; }
    public string? WhatsApp { get; set; }
    public string? Instagram { get; set; }
    public string? Website { get; set; }
    public IFormFile? Logo { get; set; }
    public bool RemoveLogo { get; set; }
}
