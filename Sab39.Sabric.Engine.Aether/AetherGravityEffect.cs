using nkast.Aether.Physics2D.Dynamics;

namespace Sab39.Sabric.Engine.Aether;

/// <summary>
/// Mutual gravitational attraction between the objects registered with it, falling off with the
/// square of the distance between them.
/// </summary>
/// <remarks>
/// Ours rather than Aether's GravityController, because that one does not do what its own
/// GravityType names say. Measured, by driving a space headlessly and reading back the velocity one
/// tick gives a body at rest:
///
/// - GravityType.DistanceSquared accelerates a body at Strength*M/r. Inverse-linear, not
///   inverse-square: a*r held constant to four decimal places across a sixteenfold sweep of distance.
/// - GravityType.Linear accelerates it at Strength*M, the same at every distance.
///
/// Each law comes out one power of r weaker than its name, which is what multiplying by an
/// unnormalised direction vector would do. No choice of Strength absorbs the difference, and a 1/r
/// force is not a weaker gravity but a different universe: its potential is logarithmic, so nothing
/// can ever reach escape velocity and every orbit is bound.
///
/// Objects register themselves rather than being swept up wholesale, so a space can go on holding
/// things that mass has no opinion about - a goal, a boundary, a marker.
/// </remarks>
public sealed class AetherGravityEffect(float strength) : AetherEffectBase
{
    /// <summary>
    /// The gravitational constant: two masses a distance apart pull on each other with a force of
    /// Strength*M*m/r^2.
    /// </summary>
    public float Strength { get; } = strength;

    private readonly List<AetherObjectBase> objects = [];

    public IReadOnlyList<AetherObjectBase> Objects => this.objects;

    public void Add(AetherObjectBase obj) => this.objects.Add(obj);

    public bool Remove(AetherObjectBase obj) => this.objects.Remove(obj);

    /// <remarks>
    /// Each pair once, and the force applied to both ends of it. Newton's third law rather than a
    /// central body everything else falls towards: a body that should not move says so by being
    /// static, and nothing gets to be immovable by accident.
    /// </remarks>
    protected override void Update(long delta, IAetherEffectContext context)
    {
        for (var i = 0; i < this.objects.Count; i++)
        {
            for (var j = i + 1; j < this.objects.Count; j++)
            {
                Attract(context, this.objects[i], this.objects[j]);
            }
        }
    }

    /// <remarks>
    /// Positions come from the context rather than from the objects because this runs inside
    /// World.Step, where the body is the live copy and an object's own Position is still the one the
    /// last sync left on it.
    ///
    /// Coincident bodies are skipped rather than clamped: the force is undefined there, and clamping
    /// would invent a direction to point it in.
    /// </remarks>
    private void Attract(IAetherEffectContext context, AetherObjectBase first, AetherObjectBase second)
    {
        if (!IsMovable(first) && !IsMovable(second)) return;

        var offset = context.GetPosition(second) - context.GetPosition(first);
        var distanceSquared = offset.LengthSquared();

        if (distanceSquared < Epsilon) return;

        // Cubed because offset is the whole displacement rather than a direction: two powers are the
        // falloff, and the third is what normalises it. Getting this wrong is the Aether bug above.
        var falloff = distanceSquared * float.Sqrt(distanceSquared);
        var force = offset * (Strength * first.Mass * second.Mass / falloff);

        if (IsMovable(first)) context.ApplyForce(first, force);
        if (IsMovable(second)) context.ApplyForce(second, -force);
    }

    private const float Epsilon = 1e-6f;

    // A kinematic body ignores a force exactly as a static one does, so neither is worth the arithmetic.
    private static bool IsMovable(AetherObjectBase obj) => obj.BodyType is BodyType.Dynamic;
}
