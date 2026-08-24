using Sab39.Sabric.Engine;

using Microsoft.AspNetCore.Components;

namespace Sab39.Sabric.UI.BlazorSVG;

/// <summary>
/// The view that renders <typeparamref name="TObject"/> using the <typeparamref name="TComponent"/>
/// Blazor component.
/// </summary>
/// <remarks>
/// <c>OpenComponent&lt;TComponent&gt;</c> is the whole point. A <see cref="RenderFragment"/> is an
/// ordinary delegate, so it can be built here by an object DI resolved - and because that object is
/// generic, the component type is statically known at the moment the render tree is written. No
/// <c>DynamicComponent</c>, no string-keyed parameter dictionary: the parameter name is a
/// <c>nameof</c> on the real property, and the constraint means the compiler has already guaranteed
/// the component accepts the object.
/// </remarks>
public sealed class ComponentView<TComponent, TObject> : IGameObjectView<TObject>
    where TComponent : GameObjectView<TObject>
    where TObject : GameObjectBase
{
    public RenderFragment Render(TObject obj) => builder =>
    {
        builder.OpenComponent<TComponent>(0);
        builder.AddComponentParameter(1, nameof(GameObjectView<TObject>.GameObject), obj);
        builder.CloseComponent();
    };
}
