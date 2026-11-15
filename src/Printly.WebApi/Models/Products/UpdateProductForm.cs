namespace Printly.WebApi.Models.Products;

public class UpdateProductForm
{
    public string ProductName { get; set; } = default!;
    public IFormFile? ProductPhoto { get; set; }
    public decimal TotalHours { get; set; }
    public int Quantity { get; set; } = 1;
    public int PiecesPerPrint { get; set; } = 1;
    public decimal? SalePriceOverride { get; set; }
    public List<ProductFilamentFormItem> Filaments { get; set; } = new();
}
