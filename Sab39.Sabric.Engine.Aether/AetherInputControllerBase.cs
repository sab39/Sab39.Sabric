using System.Numerics;

using nkast.Aether.Physics2D.Controllers;

using Sab39.Sabric.Core;

namespace Sab39.Sabric.Engine.Aether;

/// <summary>
/// Collects input sources and reduces them to a single direction. What that direction then does
/// to the world is game-specific, and is left to the derived controller's Update.
/// </summary>
public abstract class AetherInputControllerBase : Controller
{
    private readonly List<IPlayerInputSource> inputSources = [];

    public IReadOnlyList<IPlayerInputSource> InputSources => this.inputSources;

    public void AddInputSource(IPlayerInputSource inputSource) => this.inputSources.Add(inputSource);

    /// <summary>
    /// Where the player is currently trying to go, of magnitude at most 1. Sources are summed
    /// before clamping, so holding two keys at once gives a diagonal rather than more speed.
    /// </summary>
    public Vector2 MovementDirection => InputSources.Sum(input => input.MovementDirection).Clamped();
}
