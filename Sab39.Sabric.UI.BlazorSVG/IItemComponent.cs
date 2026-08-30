using Microsoft.AspNetCore.Components;

namespace Sab39.Sabric.UI.BlazorSVG;

/// <summary>
/// A Blazor component that renders one item of type <typeparamref name="TItem"/>.
/// </summary>
/// <remarks>
/// An interface rather than a base class so that an item view is free to subscribe to its item or
/// not. The list view answers for the collection gaining and losing items; whether an individual
/// item announces its own changes is that item's business, and a view for something that cannot
/// change should not have to carry the machinery for it.
///
/// What it cannot promise is the <c>[Parameter]</c> attribute. Blazor discovers parameters by
/// reflecting over the component class, and an attribute on an interface member does not reach the
/// implementation - so a view implementing this from scratch and forgetting the attribute compiles
/// and then throws the first time it renders. <see cref="ItemViewBase{TItem}"/> and
/// <see cref="SubscribingItemViewBase{TItem}"/> both get it right; deriving from one of them is
/// the way to have it checked.
///
/// Set-only, which is what allows the contravariance. A getter would force
/// <typeparamref name="TItem"/> invariant, and nothing reads the property through this interface
/// anyway - <see cref="ComponentView{TComponent, TItem}"/> needs only the name.
/// </remarks>
public interface IItemComponent<in TItem> : IComponent
{
    TItem Item { set; }
}
