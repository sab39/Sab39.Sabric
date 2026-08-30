using Microsoft.AspNetCore.Components;

namespace Sab39.Sabric.UI.BlazorSVG;

/// <summary>
/// Produces the markup for one item, under a key identifying it among its siblings.
/// </summary>
/// <remarks>
/// One interface for both halves of the seam. <see cref="ComponentView{TComponent, TItem}"/>
/// implements it for an item type whose component was named at the registration site;
/// <see cref="GameObjectViewResolver"/> implements it for a base type whose concrete views are
/// only known once there is an object in hand. A list view cannot tell which one it has, and has
/// no reason to want to.
///
/// The key is a parameter rather than something the view works out, because it belongs to the
/// parent: it identifies this item among the others in its list, and only the list knows what
/// makes them distinguishable.
/// </remarks>
public interface IItemView<in TItem>
{
    RenderFragment Render(TItem item, object key);
}
