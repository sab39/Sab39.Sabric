using System.Numerics;

using Sab39.Sabric.Core;

namespace Sab39.Sabric.Engine;

/// <summary>
/// Collects input sources and reduces them to a single direction. What that direction then does to
/// the space is game-specific, and is left to the derived effect's Update.
/// </summary>
/// <remarks>
/// The engine-agnostic twin of <c>AetherInputControllerBase</c>, which stays where it is for as long
/// as anything is still built on Aether's own controllers.
/// </remarks>
public abstract class InputEffectBase : GameEffectBase
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
