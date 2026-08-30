using Sab39.Sabric.Engine;

namespace Sab39.Sabric.UI.BlazorSVG;

/// <summary>
/// Base class for a Blazor component that renders one game object.
/// </summary>
/// <remarks>
/// What it renders is declared by its type argument, and the object arrives as an ordinary typed
/// parameter rather than a string-keyed one. Picking the right view for a given object is the
/// rendering seam - see <see cref="ComponentView{TComponent, TItem}"/> and
/// <see cref="GameObjectViewResolver"/>.
///
/// Over <see cref="SubscribingItemViewBase{TItem}"/> it adds a name and nothing else: game-side
/// markup reads better saying GameObject than Item. It is also the type the view generator matches
/// on, so a view that derives from it is registered without anyone saying so anywhere.
/// </remarks>
public abstract class GameObjectViewBase<TObject> : SubscribingItemViewBase<TObject>
    where TObject : GameObjectBase
{
    protected TObject GameObject => Item;
}
