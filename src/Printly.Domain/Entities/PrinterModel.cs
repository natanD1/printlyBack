using Printly.Domain.Exceptions;

namespace Printly.Domain.Entities;

public class PrinterModel : BaseEntity
{
    public string Name { get; private set; } = default!;
    public Guid BrandId { get; private set; }
    public string? Image { get; private set; }
    public bool IsActive { get; private set; } = true;

    protected PrinterModel()
    {
    }

    public PrinterModel(string name, Guid brandId, string? image)
    {
        Validate(name);

        Name = name.Trim();
        BrandId = brandId;
        Image = image;
    }

    /// <summary>Imagem nula mantem a atual (upload e opcional na edicao).</summary>
    public void Update(string name, Guid brandId, string? image, bool isActive)
    {
        Validate(name);

        Name = name.Trim();
        BrandId = brandId;
        Image = image ?? Image;
        IsActive = isActive;
        MarkAsUpdated();
    }

    private static void Validate(string name)
    {
        if (string.IsNullOrWhiteSpace(name))
            throw new DomainException("Nome do modelo de impressora e obrigatorio.");
    }
}
