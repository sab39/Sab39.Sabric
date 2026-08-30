using System.Numerics;

using Sab39.Sabric.Core;

namespace Sab39.Sabric.Engine;

/// <summary>
/// Where the player is currently trying to go, collected from however many sources are reporting it.
/// </summary>
/// <remarks>
/// Held rather than inherited from. Input used to be a base class an effect derived from, which tied
/// it to being exactly one effect and made that effect the only route anything had to the input; as
/// a plain object, a space can hand the same input to several effects and a UI can wire its sources
/// up without going through an effect at all.
///
/// What the input abstraction should eventually be is an open question - see Docs/architecture.md.
/// Being out here rather than tangled into the effect hierarchy is what leaves room to answer it.
/// </remarks>
public sealed class PlayerInput
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
