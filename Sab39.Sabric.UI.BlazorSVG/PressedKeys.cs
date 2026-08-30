using Sab39.Core.Components;

namespace Sab39.Sabric.UI.BlazorSVG;

/// <summary>
/// The browser key codes currently held down, and notification when that changes.
/// </summary>
/// <remarks>
/// A real object rather than a bare set on whichever component captures the key events. Blazor
/// invalidates the component that *owns* an event handler, so leaving the state there means every
/// keypress re-renders that component and everything under it. Owning it here lets the view that
/// displays it subscribe like any other view, and lets the component holding the handlers render
/// once and then hold still.
/// </remarks>
public sealed class PressedKeys : IChangeNotifier
{
    private readonly SortedSet<string> keys = [];

    public IReadOnlySet<string> Keys => this.keys;

    public event EventHandler? Changed;

    /// <remarks>
    /// Only raises when the set actually changed. keydown auto-repeats for as long as a key is
    /// held, so without that check a held key would be a notification per repeat.
    /// </remarks>
    public bool Add(string key)
    {
        if (!this.keys.Add(key)) return false;

        Changed?.Invoke(this, EventArgs.Empty);
        return true;
    }

    public bool Remove(string key)
    {
        if (!this.keys.Remove(key)) return false;

        Changed?.Invoke(this, EventArgs.Empty);
        return true;
    }
}
