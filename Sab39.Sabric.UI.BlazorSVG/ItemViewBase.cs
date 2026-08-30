using Microsoft.AspNetCore.Components;

namespace Sab39.Sabric.UI.BlazorSVG;

/// <summary>
/// A component that renders one item and never invalidates itself.
/// </summary>
/// <remarks>
/// For an item that cannot change once it is in the list. Anything that can wants
/// <see cref="SubscribingItemViewBase{TItem}"/> instead - a view with no subscription has no way of
/// noticing that what it drew has gone stale, and nothing above it re-renders on a timer to cover
/// for it.
/// </remarks>
public abstract class ItemViewBase<TItem> : ComponentBase, IItemComponent<TItem>
{
    [Parameter]
    [EditorRequired]
    public TItem Item { get; set; } = default!;
}
