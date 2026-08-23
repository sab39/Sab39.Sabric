using System.Runtime.InteropServices.JavaScript;

namespace Sab39.Sabric.UI.BlazorSVG;

/// <summary>
/// Direct access to browser globals that Blazor doesn't surface itself.
/// </summary>
public static partial class BrowserEnvironment
{
    /// <summary>
    /// Schedules <paramref name="callback"/> for the next animation frame, passing it the
    /// frame's timestamp in milliseconds. One-shot: reschedule from inside the callback to
    /// keep a loop running.
    /// </summary>
    [JSImport("globalThis.requestAnimationFrame")]
    public static partial void RequestAnimationFrame(
        [JSMarshalAs<JSType.Function<JSType.Number>>] Action<double> callback);
}
