using Printly.Domain.Exceptions;

namespace Printly.Domain.Entities;

/// <summary>
/// Vitrine publica do usuario (1:1 com <see cref="User"/>). Os itens apontam para produtos: nome, preco e foto
/// sao sempre lidos do <see cref="Product"/>.
/// </summary>
public class Showcase : BaseEntity
{
    private readonly List<ShowcaseItem> _items = new();

    public Guid UserId { get; private set; }
    public string Slug { get; private set; } = default!;
    public bool IsPublished { get; private set; }

    /// <summary>Bloqueio do admin: derruba a pagina publica independentemente de <see cref="IsPublished"/>.</summary>
    public bool IsBlocked { get; private set; }

    public IReadOnlyCollection<ShowcaseItem> Items => _items.AsReadOnly();

    protected Showcase()
    {
    }

    /// <summary>Nasce publicada; o slug ja deve ter passado pela checagem de reservado/unicidade no servico.</summary>
    public Showcase(Guid userId, string slug)
    {
        if (userId == Guid.Empty)
            throw new DomainException("Usuario e obrigatorio.");

        UserId = userId;
        Slug = ValidateSlug(slug);
        IsPublished = true;
        IsBlocked = false;
    }

    public void ChangeSlug(string slug)
    {
        Slug = ValidateSlug(slug);
        MarkAsUpdated();
    }

    public void SetPublished(bool isPublished)
    {
        if (IsPublished == isPublished)
            return;

        IsPublished = isPublished;
        MarkAsUpdated();
    }

    public void SetBlocked(bool isBlocked)
    {
        if (IsBlocked == isBlocked)
            return;

        IsBlocked = isBlocked;
        MarkAsUpdated();
    }

    public ShowcaseItem? GetItem(Guid productId) => _items.FirstOrDefault(i => i.ProductId == productId);

    /// <summary>Acrescenta no fim (visiveis), na ordem informada. Produtos ja presentes sao ignorados.</summary>
    public IReadOnlyList<ShowcaseItem> AddItems(IEnumerable<Guid> productIds)
    {
        var added = new List<ShowcaseItem>();
        var nextOrder = _items.Count == 0 ? 0 : _items.Max(i => i.SortOrder) + 1;

        foreach (var productId in productIds)
        {
            if (productId == Guid.Empty)
                throw new DomainException("Produto invalido.");

            if (GetItem(productId) is not null)
                continue;

            var item = new ShowcaseItem(Id, productId, nextOrder++);
            _items.Add(item);
            added.Add(item);
        }

        if (added.Count > 0)
            MarkAsUpdated();

        return added;
    }

    public bool RemoveItem(Guid productId)
    {
        var item = GetItem(productId);

        if (item is null)
            return false;

        _items.Remove(item);
        MarkAsUpdated();
        return true;
    }

    /// <summary>A lista deve conter exatamente os produtos atuais (sem repetir); SortOrder passa a ser o indice.</summary>
    public void Reorder(IReadOnlyList<Guid> productIds)
    {
        var current = _items.Select(i => i.ProductId).ToHashSet();

        if (productIds.Count != current.Count
            || productIds.Distinct().Count() != productIds.Count
            || !productIds.All(current.Contains))
            throw new DomainException("A lista deve conter exatamente os produtos da vitrine.");

        for (var index = 0; index < productIds.Count; index++)
            GetItem(productIds[index])!.SetSortOrder(index);

        MarkAsUpdated();
    }

    private static string ValidateSlug(string slug)
    {
        if (!ShowcaseSlug.IsValid(slug))
            throw new DomainException(ShowcaseSlug.InvalidMessage);

        if (ShowcaseSlug.IsReserved(slug))
            throw new DomainException(ShowcaseSlug.ReservedMessage);

        return slug;
    }
}
