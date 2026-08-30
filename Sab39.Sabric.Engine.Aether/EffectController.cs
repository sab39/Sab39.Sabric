using nkast.Aether.Physics2D.Controllers;

namespace Sab39.Sabric.Engine.Aether;

/// <summary>
/// The Aether controller that runs one Sabric effect, put into the world by the space for every
/// effect that isn't already carrying a controller of its own.
/// </summary>
/// <remarks>
/// One of these per effect rather than one for the whole space, so that effects and Aether's own
/// controllers keep the order they were added in.
/// </remarks>
internal sealed class EffectController(AetherSpace space, GameEffectBase effect) : Controller
{
    private readonly AetherSpace space = space;
    private readonly GameEffectBase effect = effect;

    /// <remarks>
    /// Aether's dt is the step in seconds, and is ignored: the space still holds the exact
    /// millisecond value it was asked to advance by, so there is nothing to convert back and no
    /// rounding question to answer.
    /// </remarks>
    public override void Update(float dt) => this.effect.Update(this.space.CurrentDelta, this.space.EffectContext);
}
