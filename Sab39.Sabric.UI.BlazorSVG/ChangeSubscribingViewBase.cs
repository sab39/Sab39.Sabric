using Sab39.Core.Components;

namespace Sab39.Sabric.UI.BlazorSVG;

/// <summary>
/// A view whose source announces itself through <see cref="IChangeNotifier"/>.
/// </summary>
/// <remarks>
/// For a source that raises the plain coarse event there is nothing left to vary: attaching,
/// detaching and what to do about it are the same every time. Subscribe and Unsubscribe are sealed
/// here so a derived view supplies nothing but its Source.
///
/// A source with a richer event - <see cref="Sab39.Sabric.Engine.GameBase.Ticked"/>, say - derives
/// from <see cref="SubscribingViewBase{TSource}"/> directly and says how to attach to it.
/// </remarks>
public abstract class ChangeSubscribingViewBase<TSource> : SubscribingViewBase<TSource>
    where TSource : class, IChangeNotifier
{
    protected sealed override void Subscribe(TSource source) => source.Changed += HandleChanged;
    protected sealed override void Unsubscribe(TSource source) => source.Changed -= HandleChanged;

    private void HandleChanged(object? sender, EventArgs args) => Invalidate();
}
