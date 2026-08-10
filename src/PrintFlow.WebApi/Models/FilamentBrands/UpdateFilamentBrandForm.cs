namespace PrintFlow.WebApi.Models.FilamentBrands;

public class UpdateFilamentBrandForm
{
    public string NameBrand { get; set; } = default!;
    public IFormFile? ImageBrand { get; set; }
    public bool IsActive { get; set; }
}
