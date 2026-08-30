using System.Numerics;

using nkast.Aether.Physics2D.Dynamics;

namespace Sab39.Sabric.Engine.Aether;

/// <summary>
/// The effect seam for an Aether space: reads that go to the body rather than to the object, and
/// pushes that go through Aether's own force and impulse.
/// </summary>
/// <remarks>
/// The read side is the whole reason <see cref="IEffectContext"/> has one. An effect runs inside
/// <see cref="World.Step"/>, where the object's own Position and Velocity are still those of the
/// last SyncFromWorld while the body has already moved - so the body is the live copy and the object
/// is not.
///
/// Nothing here needs to know how long the tick is: a force applied before the solver is integrated
/// over the step by Aether itself, which is exactly what ApplyAcceleration promises. The Rectro
/// context carries a delta only because it has no solver to hand the question to.
///
/// One instance per space, reused every advance, rather than an allocation a tick.
/// </remarks>
internal sealed class AetherEffectContext : IAetherEffectContext
{
    public Vector2 GetPosition(GameObjectBase obj) => GetBody(obj).Position.AsSystem();
    public Vector2 GetVelocity(GameObjectBase obj) => GetBody(obj).LinearVelocity.AsSystem();

    public float GetRotation(GameObjectBase obj) => GetBody(obj).Rotation;
    public float GetAngularVelocity(GameObjectBase obj) => GetBody(obj).AngularVelocity;

    /// <remarks>
    /// Mass is what turns each of these into its physical twin, and the body is what holds the
    /// authoritative value mid-step.
    /// </remarks>
    public void ApplyAcceleration(GameObjectBase obj, Vector2 acceleration)
        => ApplyForce(obj, acceleration * GetBody(obj).Mass);

    /// <inheritdoc cref="ApplyAcceleration"/>
    public void ApplyDeltaV(GameObjectBase obj, Vector2 deltaV)
        => ApplyImpulse(obj, deltaV * GetBody(obj).Mass);

    public void ApplyForce(GameObjectBase obj, Vector2 force) => GetBody(obj).ApplyForce(force.AsAether());

    public void ApplyImpulse(GameObjectBase obj, Vector2 impulse)
        => GetBody(obj).ApplyLinearImpulse(impulse.AsAether());

    /// <remarks>
    /// Rotational inertia is to the angular pair what mass is to the linear one, and the conversion
    /// is the same shape.
    /// </remarks>
    public void ApplyAngularAcceleration(GameObjectBase obj, float acceleration)
        => ApplyTorque(obj, acceleration * GetBody(obj).Inertia);

    /// <inheritdoc cref="ApplyAngularAcceleration"/>
    public void ApplyAngularDeltaV(GameObjectBase obj, float deltaV)
        => ApplyAngularImpulse(obj, deltaV * GetBody(obj).Inertia);

    public void ApplyTorque(GameObjectBase obj, float torque) => GetBody(obj).ApplyTorque(torque);

    public void ApplyAngularImpulse(GameObjectBase obj, float impulse)
        => GetBody(obj).ApplyAngularImpulse(impulse);

    /// <remarks>
    /// Cast rather than tested: everything in an <see cref="AetherSpace"/> is one of these by
    /// construction, which is the same argument that lets <see cref="AetherObjectBase.Space"/> cast.
    /// </remarks>
    private static Body GetBody(GameObjectBase obj) => ((AetherObjectBase)obj).Body;
}
