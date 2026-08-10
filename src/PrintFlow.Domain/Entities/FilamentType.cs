using PrintFlow.Domain.Exceptions;

namespace PrintFlow.Domain.Entities;

public class FilamentType : BaseEntity
{
    public string Name { get; private set; } = default!;
    public bool IsActive { get; private set; } = true;

    protected FilamentType()
    {
    }

    public FilamentType(string name)
    {
        Validate(name);

        Name = name.Trim();
    }

    public void Update(string name)
    {
        Validate(name);

        Name = name.Trim();
        MarkAsUpdated();
    }

    public void Activate()
    {
        IsActive = true;
        MarkAsUpdated();
    }

    public void Deactivate()
    {
        IsActive = false;
        MarkAsUpdated();
    }

    private static void Validate(string name)
    {
        if (string.IsNullOrWhiteSpace(name))
            throw new DomainException("Nome do tipo de filamento e obrigatorio.");
    }
}
