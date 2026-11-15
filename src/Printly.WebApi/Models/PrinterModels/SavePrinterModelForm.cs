namespace Printly.WebApi.Models.PrinterModels;

public class SavePrinterModelForm
{
    public string Name { get; set; } = default!;
    public Guid BrandId { get; set; }
    public IFormFile? Image { get; set; }
    public bool IsActive { get; set; } = true;
}
