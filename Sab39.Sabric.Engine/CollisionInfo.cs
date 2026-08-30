using System.Numerics;

namespace Sab39.Sabric.Engine;

/// <summary>
/// One contact between two game objects, as it stood when the physics reported it.
/// </summary>
/// <remarks>
/// Everything here is something any physics implementation can produce: a point, a normal, and how
/// fast the two were closing along it. Approach speed in particular is relative velocity along the
/// normal and needs no solver, which is why it is here rather than alongside impulse - impulse
/// presupposes momentum being exchanged, and an implementation that merely resolves overlaps has
/// none to report. See Docs/architecture.md.
///
/// A separation carries the two objects and nothing else. The contact is over by the time one is
/// raised, so there is no point, normal or speed left to describe.
/// </remarks>
public record CollisionInfo
{
    public required GameObjectBase First { get; init; }
    public required GameObjectBase Second { get; init; }

    /// <summary>
    /// Where the two touched, in world coordinates.
    /// </summary>
    /// <remarks>
    /// One representative point, even where the physics found several - two circles only ever have
    /// one, and a rule asking "where did this happen" wants a place rather than a set. An
    /// implementation with more to say adds it on its own subclass.
    /// </remarks>
    public Vector2 Point { get; init; }

    /// <summary>
    /// The contact normal, pointing from <see cref="First"/> towards <see cref="Second"/>.
    /// </summary>
    public Vector2 Normal { get; init; }

    /// <summary>
    /// How fast the two were closing along <see cref="Normal"/> when they met.
    /// </summary>
    /// <remarks>
    /// Kinematic and mass-free, so it says how fast rather than how hard. Where a physics
    /// implementation offers a solved impulse that is the better measure of force, and it belongs
    /// on that implementation's own subclass.
    /// </remarks>
    public float ApproachSpeed { get; init; }

    public bool Involves(GameObjectBase obj) => First == obj || Second == obj;

    /// <summary>
    /// True when this contact is between exactly these two objects, in either order.
    /// </summary>
    /// <remarks>
    /// The shape almost every rule wants, because a rule is about a pair - the puck hits the
    /// player's planet, the puck enters a goal - and the order the physics reported them in is
    /// arbitrary.
    /// </remarks>
    public bool Involves(GameObjectBase first, GameObjectBase second)
        => (First == first && Second == second) || (First == second && Second == first);

    /// <summary>
    /// The object <paramref name="obj"/> collided with, or null if it was not part of this contact.
    /// </summary>
    public GameObjectBase? Other(GameObjectBase obj)
        => First == obj ? Second : Second == obj ? First : null;
}
