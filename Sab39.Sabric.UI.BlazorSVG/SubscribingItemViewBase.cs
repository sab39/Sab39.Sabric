using Sab39.Core.Components;

using Microsoft.AspNetCore.Components;

namespace Sab39.Sabric.UI.BlazorSVG;

/// <summary>
/// A component that renders one item and invalidates itself when that item changes.
/// </summary>
/// <remarks>
/// The Item parameter is declared again rather than inherited from
/// <see cref="ItemViewBase{TItem}"/>: the two have different bases - one plain, one carrying the
/// subscription machinery - and C# has only one base to give.
/// <see cref="IItemComponent{TItem}"/> is what lets
/// <see cref="ComponentView{TComponent, TItem}"/> accept either.
/// </remarks>
public abstract class SubscribingItemViewBase<TItem> : ChangeSubscribingViewBase<TItem>, IItemComponent<TItem>
    where TItem : class, IChangeNotifier
{
    [Parameter]
    [EditorRequired]
    public TItem Item { get; set; } = default!;

    protected sealed override TItem? Source => Item;
}
