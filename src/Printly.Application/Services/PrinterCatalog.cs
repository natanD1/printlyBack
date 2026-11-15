using Printly.Domain.Entities;
using Printly.Domain.Interfaces;

namespace Printly.Application.Services;

/// <summary>
/// Catalogo de modelos e marcas carregado de uma vez (tabelas pequenas, mantidas pelo admin),
/// usado pra montar o nome exibido das impressoras do usuario.
/// </summary>
internal sealed class PrinterCatalog
{
    public required IReadOnlyDictionary<Guid, PrinterModel> Models { get; init; }
    public required IReadOnlyDictionary<Guid, string> BrandNames { get; init; }

    public static async Task<PrinterCatalog> LoadAsync(
        IPrinterModelRepository printerModelRepository,
        IPrinterBrandRepository printerBrandRepository,
        CancellationToken cancellationToken)
    {
        var models = await printerModelRepository.GetAllAsync(cancellationToken);
        var brands = await printerBrandRepository.GetAllAsync(cancellationToken);

        return new PrinterCatalog
        {
            Models = models.ToDictionary(m => m.Id),
            BrandNames = brands.ToDictionary(b => b.Id, b => b.Name),
        };
    }

    /// <summary>Apelido, se houver; senao "Marca Modelo".</summary>
    public string DisplayName(Printer printer)
    {
        if (!string.IsNullOrWhiteSpace(printer.Nickname))
            return printer.Nickname;

        if (!Models.TryGetValue(printer.PrinterModelId, out var model))
            return string.Empty;

        return $"{BrandNames.GetValueOrDefault(model.BrandId, string.Empty)} {model.Name}".Trim();
    }
}
