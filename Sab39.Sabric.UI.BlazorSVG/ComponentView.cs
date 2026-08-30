using Microsoft.AspNetCore.Components;

namespace Sab39.Sabric.UI.BlazorSVG;

/// <summary>
/// The view that renders <typeparamref name="TItem"/> using the <typeparamref name="TComponent"/>
/// Blazor component.
/// </summary>
/// <remarks>
/// <c>OpenComponent&lt;TComponent&gt;</c> is the whole point. A <see cref="RenderFragment"/> is an
/// ordinary delegate, so it can be built here by an object DI resolved - and because that object is
/// generic, the component type is statically known at the moment the render tree is written. No
/// <c>DynamicComponent</c>, no string-keyed parameter dictionary: the parameter name is a
/// <c>nameof</c> on a real property, and the constraint means the compiler has already guaranteed
/// the component accepts the item.
/// </remarks>
public sealed class ComponentView<TComponent, TItem> : IItemView<TItem>
    where TComponent : IItemComponent<TItem>
{
    public RenderFragment Render(TItem item, object key) => builder =>
    {
        builder.OpenComponent<TComponent>(0);

        // SetKey applies to the frame that's currently open, so it has to come after
        // OpenComponent. Before it, it would key whatever element the fragment was rendered
        // inside - quietly wrong rather than an error.
        builder.SetKey(key);

        builder.AddComponentParameter(1, nameof(IItemComponent<TItem>.Item), item);
        builder.CloseComponent();
    };
}
