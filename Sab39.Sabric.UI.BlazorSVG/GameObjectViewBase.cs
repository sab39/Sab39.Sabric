using Sab39.Sabric.Engine;

using Microsoft.AspNetCore.Components;

namespace Sab39.Sabric.UI.BlazorSVG;

/// <summary>
/// Base class for a Blazor component that renders one game object.
/// </summary>
/// <remarks>
/// What a view renders is declared by its type argument, and the object arrives as an ordinary
/// typed parameter rather than a string-keyed one. Picking the right view for a given object is
/// the rendering seam - see <see cref="ComponentView{TComponent, TObject}"/> and
/// <see cref="GameObjectViewResolver"/>.
/// </remarks>
public abstract class GameObjectViewBase<TObject> : SubscribingViewBase<TObject>
    where TObject : GameObjectBase
{
    [Parameter]
    [EditorRequired]
    public TObject GameObject { get; set; } = default!;

    protected override TObject? Source => GameObject;

    protected override void Subscribe(TObject source) => source.PropertyChanged += HandlePropertyChanged;
    protected override void Unsubscribe(TObject source) => source.PropertyChanged -= HandlePropertyChanged;

    // The property name is ignored: the answer to any change is the same re-render, so gating on
    // which property moved would cost more than it saves.
    private void HandlePropertyChanged(object? sender, string? propertyName) => Invalidate();
}
