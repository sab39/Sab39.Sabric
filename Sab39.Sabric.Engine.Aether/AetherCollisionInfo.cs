using System.Collections.Immutable;
using System.Numerics;

namespace Sab39.Sabric.Engine.Aether;

/// <summary>
/// A contact as Aether saw it, with what only a solver can say about it.
/// </summary>
/// <remarks>
/// The line between this and <see cref="CollisionInfo"/> is what a physics implementation must be
/// able to produce, not what Aether happens to have. Impulse presupposes a solver, so it cannot be
/// promised by the abstract layer even though it is the better answer to "how hard" - it is
/// momentum actually exchanged, so a heavy body drifting in outweighs a light one arriving fast.
///
/// The impulses arrive from a second hook. Aether reports a contact beginning before it solves it,
/// so they are zero at that moment and are filled in later in the same step; see
/// <see cref="AetherSpace"/>.
/// </remarks>
public sealed record AetherCollisionInfo : CollisionInfo
{
    /// <summary>
    /// The manifold points, in world coordinates - one for two circles, up to two in general.
    /// </summary>
    /// <remarks>
    /// Initialized rather than left to default: a default <see cref="ImmutableArray{T}"/> throws
    /// on use, and a separation carries no points at all.
    /// </remarks>
    public ImmutableArray<Vector2> Points { get; init; } = [];

    public float NormalImpulse { get; init; }
    public float TangentImpulse { get; init; }

    public float Friction { get; init; }
    public float Restitution { get; init; }
}
