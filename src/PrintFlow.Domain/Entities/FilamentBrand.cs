using PrintFlow.Domain.Exceptions;

namespace PrintFlow.Domain.Entities;

public class FilamentBrand : BaseEntity
{
    public string NameBrand { get; private set; } = default!;
    public string? ImageBrand { get; private set; }
    public bool IsActive { get; private set; } = true;

    protected FilamentBrand()
    {
    }

    public FilamentBrand(string nameBrand, string? imageBrand)
    {
        Validate(nameBrand);

        NameBrand = nameBrand.Trim();
        ImageBrand = imageBrand;
    }

    public void Update(string nameBrand, string? imageBrand)
    {
        Validate(nameBrand);

        NameBrand = nameBrand.Trim();
        ImageBrand = imageBrand ?? ImageBrand;
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

    private static void Validate(string nameBrand)
    {
        if (string.IsNullOrWhiteSpace(nameBrand))
            throw new DomainException("Nome da marca de filamento e obrigatorio.");
    }
}
