namespace Sab39.Sabric.Engine.Aether;

/// <summary>
/// An effect that knows about Aether's physics concepts: the analogue of
/// <see cref="AetherObjectBase"/> and <see cref="AetherSpace"/> on the effect side.
/// </summary>
/// <remarks>
/// Wraps rather than inherits, like every effect does. Aether's Controller is a class rather than an
/// interface, so nothing on the Sabric side can be both it and a <see cref="GameEffectBase"/> - the
/// space puts a forwarder into the world and that forwarder calls this. See
/// Docs/WIP/effects-and-rectro.md.
/// </remarks>
public abstract class AetherEffectBase : GameEffectBase
{
    // Cast rather than a second backing field, as on the object side: provably safe from the space
    // that attached it, and free once the JIT has folded it.
    public override AetherSpace Space => (AetherSpace)base.Space;

    /// <inheritdoc cref="GameEffectBase.Update"/>
    protected abstract void Update(long delta, IAetherEffectContext context);

    /// <remarks>
    /// C# cannot narrow a parameter in an override, so the narrowing is a sealed hop and a cast -
    /// the same shape as <see cref="AetherObjectBase.Space"/>, and safe for the same reason: the
    /// only caller is the space that installed this, and what it hands over is its own context.
    ///
    /// The two Updates are overloads, so what stops this recursing is overload resolution picking
    /// the more specific one for an argument already typed as the narrow interface. Sealed so a
    /// derived type cannot get in between and make that untrue.
    /// </remarks>
    public sealed override void Update(long delta, IEffectContext context)
        => Update(delta, (IAetherEffectContext)context);
}
