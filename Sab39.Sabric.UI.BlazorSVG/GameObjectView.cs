using Sab39.Sabric.Engine;

using Microsoft.AspNetCore.Components;

namespace Sab39.Sabric.UI.BlazorSVG;

/// <summary>
/// Base class for a Blazor component that renders one game object.
/// </summary>
/// <remarks>
/// What a view renders is declared by its type argument, and the object arrives as an ordinary
/// typed parameter rather than a string-keyed one. Picking the right view for a given object is
/// the rendering seam, which doesn't exist yet - see Docs/WIP/sporbits-revival.md in the
/// Sporbits repo.
/// </remarks>
public abstract class GameObjectView<TObject> : ComponentBase
    where TObject : GameObjectBase
{
    [Parameter]
    [EditorRequired]
    public TObject GameObject { get; set; } = default!;
}
