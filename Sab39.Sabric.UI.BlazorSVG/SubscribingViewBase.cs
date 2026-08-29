using Microsoft.AspNetCore.Components;

namespace Sab39.Sabric.UI.BlazorSVG;

/// <summary>
/// A Blazor component that watches one thing and invalidates itself when that thing changes.
/// </summary>
/// <remarks>
/// The point of the rendering seam: nothing above a view re-renders per frame, so each view has to
/// know for itself when it has gone stale. All that varies between views is what they watch and
/// how to attach to it, so those are the abstract members and everything fiddly - the cached
/// subscription, moving it when the source changes, releasing it on disposal - lives here.
///
/// A view's markup has to be a function of its source. Renders are suppressed unless the source
/// raised something or the source itself changed, so a parameter that affects the output without
/// going through the source will silently fail to appear.
/// </remarks>
public abstract class SubscribingViewBase<TSource> : ComponentBase, IDisposable
    where TSource : class
{
    /// <summary>
    /// The thing being watched, normally straight from a parameter.
    /// </summary>
    protected abstract TSource? Source { get; }

    protected abstract void Subscribe(TSource source);
    protected abstract void Unsubscribe(TSource source);

    private TSource? subscribed;
    private bool isStale = true;

    /// <remarks>
    /// Here rather than in OnInitialized, and defensively: by design a view watches one source for
    /// its whole lifetime and SetKey reinforces that, but if the parameter is re-set anyway this
    /// moves the subscription across and carries on rather than throwing. Unsubscribing from the
    /// cached field rather than from Source is what makes that reliable - including for a
    /// component disposed before it was ever given a parameter.
    /// </remarks>
    protected override void OnParametersSet()
    {
        var source = Source;
        if (object.ReferenceEquals(source, this.subscribed)) return;

        if (this.subscribed is not null) Unsubscribe(this.subscribed);
        this.subscribed = source;
        if (source is not null) Subscribe(source);

        this.isStale = true;
    }

    /// <summary>
    /// Marks this view stale and queues a re-render. What every subscription ends up calling.
    /// </summary>
    protected void Invalidate()
    {
        this.isStale = true;

        // InvokeAsync rather than the bare call: identical on single-threaded WASM today, not
        // identical if anything ever renders server-side.
        InvokeAsync(StateHasChanged);
    }

    /// <remarks>
    /// This is what makes a view immune to being re-rendered for someone else's reasons - a parent
    /// that re-rendered for its own sake hands down new parameters, and without this every child
    /// would render along with it.
    /// </remarks>
    protected override bool ShouldRender()
    {
        if (!this.isStale) return false;

        this.isStale = false;
        return true;
    }

    /// <remarks>
    /// Non-virtual, with OnDisposing as the hook, so a derived view physically cannot skip the
    /// unsubscribe and forgetting base.OnDisposing() costs it only its own cleanup. It also means
    /// the reflexive Blazor idiom - @implements IDisposable plus a public Dispose - is CS0108 on a
    /// derived view, and so a build failure, rather than an override that silently never runs.
    ///
    /// No Dispose(bool), no finalizer, no disposed flag: there are no unmanaged resources, Blazor's
    /// call is deterministic, and -= against a handler that isn't attached is a no-op.
    /// </remarks>
    public void Dispose()
    {
        if (this.subscribed is not null) Unsubscribe(this.subscribed);
        this.subscribed = null;

        OnDisposing();
    }

    protected virtual void OnDisposing() { }
}
