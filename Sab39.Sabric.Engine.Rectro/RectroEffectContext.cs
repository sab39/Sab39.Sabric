using System.Numerics;

namespace Sab39.Sabric.Engine.Rectro;

/// <summary>
/// The effect seam for a Rectro space: reads that are the object's own state, and pushes that land
/// on its velocity there and then.
/// </summary>
/// <remarks>
/// The read side is a straight pass-through, because a Rectro object *is* its own physics state -
/// there is no second copy to be out of date. That is the whole reason the interface has one: an
/// engine that keeps its state elsewhere needs the indirection, and this one is what it costs when
/// nothing does.
///
/// One instance, reused every advance with <see cref="Seconds"/> replaced, rather than an allocation
/// a tick.
/// </remarks>
internal sealed class RectroEffectContext : IEffectContext
{
    /// <summary>
    /// How long the advance currently being run is, in seconds.
    /// </summary>
    public float Seconds { get; set; }

    public Vector2 GetPosition(GameObjectBase obj) => obj.Position;
    public Vector2 GetVelocity(GameObjectBase obj) => obj.Velocity;

    public float GetRotation(GameObjectBase obj) => obj.Rotation;
    public float GetAngularVelocity(GameObjectBase obj) => obj.AngularVelocity;

    public void ApplyAcceleration(GameObjectBase obj, Vector2 acceleration)
        => obj.Velocity += acceleration * Seconds;

    public void ApplyDeltaV(GameObjectBase obj, Vector2 deltaV) => obj.Velocity += deltaV;

    /// <remarks>
    /// Rectro's rectangles are axis-aligned - it is the first syllable of the name - so a turn has
    /// nothing here to land on, and no version of this engine will have one. The reads still answer:
    /// a Rectro object has a <see cref="GameObjectBase.Rotation"/> like any other game object, and
    /// something may be drawing by it. What is refused is making one change.
    /// </remarks>
    public void ApplyAngularAcceleration(GameObjectBase obj, float acceleration) => throw DoesNotRotate();

    /// <inheritdoc cref="ApplyAngularAcceleration"/>
    public void ApplyAngularDeltaV(GameObjectBase obj, float deltaV) => throw DoesNotRotate();

    private static NotSupportedException DoesNotRotate()
        => new("Rectro is axis-aligned, and turns nothing.");
}
