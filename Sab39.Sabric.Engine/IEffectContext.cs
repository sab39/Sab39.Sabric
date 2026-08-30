using System.Numerics;

namespace Sab39.Sabric.Engine;

/// <summary>
/// What an effect is handed for the tick it is being run for: what is true right now, and where to
/// put a push.
/// </summary>
/// <remarks>
/// The whole seam between an effect and whatever engine is running it, in both directions. The read
/// side exists because an effect can run in the middle of an engine's own step, where an object's
/// own properties are still those of the last sync while the physics has already moved on - harmless
/// for something that only pushes, wrong for anything that steers. The write side is here rather
/// than on the object so that <see cref="GameObjectBase"/> doesn't grow public methods that are only
/// meaningful mid-tick.
///
/// Acceleration and delta-v and nothing else: both are kinematics and belong to any engine, where
/// force and impulse presuppose mass. An engine with physics adds those on its own interface.
///
/// Whether an effect sees an earlier effect's push within the same tick is not contractual. See
/// Docs/WIP/effects-and-rectro.md.
/// </remarks>
public interface IEffectContext
{
    Vector2 GetPosition(GameObjectBase obj);
    Vector2 GetVelocity(GameObjectBase obj);

    /// <remarks>
    /// Read-only for now, where position and velocity have a push each. Rotation is a concept the
    /// generic layer carries, so an effect that steers by it needs to be able to see it; nothing has
    /// yet wanted the angular twin of <see cref="ApplyDeltaV"/>, so there isn't one.
    /// </remarks>
    float GetRotation(GameObjectBase obj);

    /// <inheritdoc cref="GetRotation"/>
    float GetAngularVelocity(GameObjectBase obj);

    /// <summary>
    /// Pushes <paramref name="obj"/> at <paramref name="acceleration"/> units per second squared,
    /// for the length of this tick.
    /// </summary>
    void ApplyAcceleration(GameObjectBase obj, Vector2 acceleration);

    /// <summary>
    /// Changes <paramref name="obj"/>'s velocity by <paramref name="deltaV"/> outright, however long
    /// this tick is.
    /// </summary>
    /// <remarks>
    /// The counterpart to an impulse for an engine with no masses to weigh one by. Setting a
    /// velocity is spelled as a delta-v against <see cref="GetVelocity"/> rather than offered as a
    /// second method: it is the read side doing the job it is there for, and it keeps every write
    /// through this seam a push.
    /// </remarks>
    void ApplyDeltaV(GameObjectBase obj, Vector2 deltaV);
}
