using System.Numerics;

namespace Sab39.Sabric.Engine.Aether;

/// <summary>
/// What an effect that knows about Aether is handed for the tick it is being run for: everything
/// <see cref="IEffectContext"/> offers, plus the pushes that only mean anything where there is mass
/// to weigh them by.
/// </summary>
/// <remarks>
/// Acceleration and delta-v are kinematics and belong to any engine, so they stay on the narrow
/// interface; force and impulse presuppose mass, so they are here. This is the same line
/// <see cref="AetherObjectBase.Mass"/> sits on.
///
/// One context object, handed out typed differently. An effect that has never heard of Aether sees
/// <see cref="IEffectContext"/> and an <see cref="AetherEffectBase"/> sees this, and the two are the
/// same instance. See Docs/WIP/effects-and-rectro.md.
/// </remarks>
public interface IAetherEffectContext : IEffectContext
{
    /// <summary>
    /// Pushes <paramref name="obj"/> with <paramref name="force"/> for the length of this tick.
    /// </summary>
    void ApplyForce(GameObjectBase obj, Vector2 force);

    /// <summary>
    /// Changes <paramref name="obj"/>'s momentum by <paramref name="impulse"/> outright, however
    /// long this tick is.
    /// </summary>
    void ApplyImpulse(GameObjectBase obj, Vector2 impulse);
}
