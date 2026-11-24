namespace Printly.Domain.Entities;

/// <summary>Produto exposto na vitrine. Criado so por <see cref="Showcase.AddItems"/>.</summary>
public class ShowcaseItem : BaseEntity
{
    public Guid ShowcaseId { get; private set; }
    public Guid ProductId { get; private set; }
    public int SortOrder { get; private set; }
    public bool IsVisible { get; private set; }

    protected ShowcaseItem()
    {
    }

    internal ShowcaseItem(Guid showcaseId, Guid productId, int sortOrder)
    {
        ShowcaseId = showcaseId;
        ProductId = productId;
        SortOrder = sortOrder;
        IsVisible = true;
    }

    public void SetVisible(bool isVisible)
    {
        if (IsVisible == isVisible)
            return;

        IsVisible = isVisible;
        MarkAsUpdated();
    }

    internal void SetSortOrder(int sortOrder)
    {
        if (SortOrder == sortOrder)
            return;

        SortOrder = sortOrder;
        MarkAsUpdated();
    }
}
