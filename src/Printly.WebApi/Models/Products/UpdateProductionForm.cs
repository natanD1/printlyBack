namespace Printly.WebApi.Models.Products;

public class UpdateProductionForm
{
    /// <summary>Verdadeiro desconta o filamento do estoque; falso devolve (volta a ser orcamento).</summary>
    public bool IsProduced { get; set; }
}
