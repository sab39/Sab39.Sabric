using Sab39.Core.Components;

using Microsoft.AspNetCore.Components;

namespace Sab39.Sabric.UI.BlazorSVG;

/// <summary>
/// Renders a list of items, one component each, and invalidates itself when the list changes.
/// </summary>
/// <remarks>
/// The other half of per-view invalidation. Each item view watches its own item, so nothing above
/// them re-renders per frame - which leaves the collection itself gaining and losing items as the
/// one thing no item view can see. This is the component that sees it, and being a component is
/// the point: the containing page holds still even when the list moves, so a spawn costs a list's
/// worth of fragments rather than the whole tree.
///
/// Re-rendering the list does not re-render the items in it. Each one is handed the same item it
/// already had, and a view whose source hasn't changed suppresses its own render - so the cost of
/// a spawn is a fragment and a parameter set per item, and a DOM change only where the keys say
/// something actually moved.
///
/// Abstract, which a .razor file cannot declare for itself - the modifier lives on this half and
/// applies to the whole partial class. GetKey is why it is abstract: what makes items
/// distinguishable is the list's business, not the framework's.
/// </remarks>
public abstract partial class ItemListViewBase<TItem>
{
    [Parameter]
    [EditorRequired]
    public IReadOnlyNotifyingList<TItem> Items { get; set; } = default!;

    /// <remarks>
    /// Whether this resolves to a single component's view or to something that dispatches per item
    /// is invisible from here, and deliberately so - see <see cref="IItemView{TItem}"/>.
    /// </remarks>
    [Inject]
    private IItemView<TItem> view { get; set; } = default!;

    protected sealed override IReadOnlyNotifyingList<TItem>? Source => Items;

    /// <summary>
    /// What tells this item apart from the others in the list, for as long as it is in it.
    /// </summary>
    /// <remarks>
    /// The key is set on the item's component frame, so it decides which component instance
    /// survives a change to the list. Getting it wrong is not a rendering error - it costs the
    /// wrong components their state and their subscriptions.
    /// </remarks>
    protected abstract object GetKey(TItem item);
}
