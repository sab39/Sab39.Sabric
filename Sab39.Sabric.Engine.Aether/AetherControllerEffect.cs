using System.Diagnostics;

using nkast.Aether.Physics2D.Controllers;

namespace Sab39.Sabric.Engine.Aether;

/// <summary>
/// One of Aether's own controllers, listed as an effect so that a space has one list of ongoing
/// things rather than two.
/// </summary>
/// <remarks>
/// The only kind of effect that puts a real Aether controller into the world rather than a
/// forwarder, and an Aether-specific category for a reason: it exists only because Aether ships
/// pre-built controllers we didn't write. Rectro ships none, so every Rectro effect has a real
/// Update. See Docs/WIP/effects-and-rectro.md.
/// </remarks>
public sealed class AetherControllerEffect(Controller controller) : GameEffectBase
{
    public Controller Controller { get; } = controller;

    /// <remarks>
    /// The contract of Update is that the engine calls it and the game implements it, and game code
    /// has no context object to pass - so game code cannot call this at all. What is in the world is
    /// the controller, which Aether updates directly, so nothing is ever going to. Reaching here
    /// means the engine broke its own contract, which is what makes it unreachable rather than
    /// unsupported (a caller asked for something we don't do) or unimplemented (a gap to come back
    /// and fill).
    /// </remarks>
    public override void Update(long delta, IEffectContext context) => throw new UnreachableException();
}
