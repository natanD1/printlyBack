namespace Printly.WebApi.Models.FilamentBrands;

public class CreateFilamentBrandForm
{
    public string NameBrand { get; set; } = default!;
    public IFormFile? ImageBrand { get; set; }
}
