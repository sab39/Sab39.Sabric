using Sab39.Sabric.Engine;

using Microsoft.AspNetCore.Components;

namespace Sab39.Sabric.UI.BlazorSVG;

/// <summary>
/// Renders a game object whose type the caller doesn't know.
/// </summary>
public interface IGameObjectView
{
    RenderFragment Render(GameObjectBase obj);
}

/// <summary>
/// Renders a game object of a known type.
/// </summary>
public interface IGameObjectView<in TObject> : IGameObjectView
    where TObject : GameObjectBase
{
    RenderFragment Render(TObject obj);

    // The one cast in the whole seam. Safe by construction: an implementation is only ever
    // registered against the object type it was closed over.
    RenderFragment IGameObjectView.Render(GameObjectBase obj) => Render((TObject)obj);
}
