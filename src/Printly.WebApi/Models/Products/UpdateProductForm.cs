namespace Printly.WebApi.Models.Products;

public class UpdateProductForm
{
    public string ProductName { get; set; } = default!;
    public IFormFile? ProductPhoto { get; set; }
    public decimal TotalHours { get; set; }
    public decimal? SalePriceOverride { get; set; }
    public List<ProductFilamentFormItem> Filaments { get; set; } = new();
}
