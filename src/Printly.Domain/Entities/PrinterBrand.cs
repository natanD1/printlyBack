using Printly.Domain.Exceptions;

namespace Printly.Domain.Entities;

public class PrinterBrand : BaseEntity
{
    public string Name { get; private set; } = default!;
    public bool IsActive { get; private set; } = true;

    protected PrinterBrand()
    {
    }

    public PrinterBrand(string name)
    {
        Validate(name);

        Name = name.Trim();
    }

    public void Update(string name, bool isActive)
    {
        Validate(name);

        Name = name.Trim();
        IsActive = isActive;
        MarkAsUpdated();
    }

    private static void Validate(string name)
    {
        if (string.IsNullOrWhiteSpace(name))
            throw new DomainException("Nome da marca de impressora e obrigatorio.");
    }
}
