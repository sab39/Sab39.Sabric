using System.Numerics;

namespace Sab39.Sabric.Engine.Rectro;

/// <summary>
/// A space of axis-aligned rectangles, moved and pushed apart once per advance.
/// </summary>
/// <remarks>
/// There is no foreign world to delegate to, so this is the whole engine: move everything by its
/// velocity, find the pairs that overlap, and separate them. Collisions are resolved without
/// physics - a pair stops dead on the collision axis and no momentum is exchanged - which is what
/// makes Rectro a real test of the CollisionInfo / AetherCollisionInfo split, since there is no
/// impulse here to report. See Docs/WIP/effects-and-rectro.md.
/// </remarks>
public abstract class RectroSpace(GameSessionBase session) : GameSpaceBase<RectroObjectBase>(session)
{
    protected override void OnAdvance(long delta)
    {
        // Effects run here, before anything moves. Nothing implements them yet - see
        // Docs/WIP/effects-and-rectro.md.

        Move(delta);
        Collide();
    }

    /// <remarks>
    /// Milliseconds in, seconds out: <see cref="GameSpaceBase.Advance"/> deals in milliseconds and
    /// a velocity is per second.
    /// </remarks>
    private void Move(long delta)
    {
        var seconds = delta / 1000f;

        foreach (var obj in GameObjects)
        {
            if (obj.BodyType is not RectroBodyType.Static) obj.Position += obj.Velocity * seconds;
        }
    }

    /// <remarks>
    /// Edges only, like the Aether side: the pairs touching now, against the pairs that were
    /// touching last advance. Two sets swapped at the end rather than one and an allocation a tick.
    /// </remarks>
    private HashSet<ContactPair> contacts = [];
    private HashSet<ContactPair> touching = [];

    private void Collide()
    {
        var objects = GameObjects;

        for (var i = 0; i < objects.Count; i++)
        {
            for (var j = i + 1; j < objects.Count; j++)
            {
                var first = objects[i];
                var second = objects[j];

                if (FindContact(first, second) is not { } contact) continue;

                ContactPair pair = new(first, second);
                this.touching.Add(pair);

                // Described before resolving rather than after: approach speed says how fast the
                // two were closing, and resolving is about to zero the velocities it reads.
                if (!this.contacts.Contains(pair)) QueueCollision(Describe(first, second, contact));

                Resolve(first, second, contact);
            }
        }

        foreach (var pair in this.contacts)
        {
            if (!this.touching.Contains(pair)) QueueSeparation(new() { First = pair.First, Second = pair.Second });
        }

        (this.contacts, this.touching) = (this.touching, this.contacts);
        this.touching.Clear();
    }

    /// <summary>
    /// The contact between two rectangles, or null if they do not overlap.
    /// </summary>
    /// <remarks>
    /// The collision axis is whichever they overlap least on: the shortest way out is the way they
    /// came in, for anything not moving fast enough to pass most of the way through in one advance.
    /// The normal points from <paramref name="first"/> towards <paramref name="second"/>, as
    /// <see cref="CollisionInfo"/> promises, and the point is the middle of the overlapping region.
    /// </remarks>
    private static Contact? FindContact(RectroObjectBase first, RectroObjectBase second)
    {
        var offset = second.Position - first.Position;
        var overlap = ((first.Size + second.Size) / 2) - Vector2.Abs(offset);

        if (overlap.X <= 0 || overlap.Y <= 0) return null;

        var alongX = overlap.X < overlap.Y;
        Vector2 normal = alongX ? new(float.CopySign(1, offset.X), 0) : new(0, float.CopySign(1, offset.Y));
        var point = (Vector2.Max(first.Min, second.Min) + Vector2.Min(first.Max, second.Max)) / 2;

        return new(normal, alongX ? overlap.X : overlap.Y, point);
    }

    /// <remarks>
    /// A plain <see cref="CollisionInfo"/> and no Rectro subclass of it. Everything the general
    /// record carries is something an engine with no solver can say, which is the line it was drawn
    /// on, and Rectro has nothing to add beyond it.
    /// </remarks>
    private static CollisionInfo Describe(RectroObjectBase first, RectroObjectBase second, Contact contact)
        => new()
        {
            First = first,
            Second = second,
            Point = contact.Point,
            Normal = contact.Normal,

            // Closing speed is positive, so the sign is flipped: the normal points from first
            // towards second, and second moving that way is the two separating rather than meeting.
            ApproachSpeed = -Vector2.Dot(second.Velocity - first.Velocity, contact.Normal),
        };

    /// <remarks>
    /// Nothing is exchanged. Each dynamic rectangle is pushed back out along the normal and loses
    /// its velocity along that axis outright; where both can move they take half the depth each,
    /// which is the only split available with no masses to weigh them by.
    /// </remarks>
    private static void Resolve(RectroObjectBase first, RectroObjectBase second, Contact contact)
    {
        var firstMoves = first.BodyType is RectroBodyType.Dynamic;
        var secondMoves = second.BodyType is RectroBodyType.Dynamic;
        var depth = (firstMoves && secondMoves) ? contact.Depth / 2 : contact.Depth;

        if (firstMoves)
        {
            first.Position -= contact.Normal * depth;
            first.Velocity = Stopped(first.Velocity, contact.Normal);
        }

        if (secondMoves)
        {
            second.Position += contact.Normal * depth;
            second.Velocity = Stopped(second.Velocity, contact.Normal);
        }
    }

    /// <summary>
    /// <paramref name="velocity"/> with its component along <paramref name="normal"/> removed.
    /// </summary>
    private static Vector2 Stopped(Vector2 velocity, Vector2 normal)
        => velocity - (normal * Vector2.Dot(velocity, normal));

    private readonly record struct Contact(Vector2 Normal, float Depth, Vector2 Point);

    /// <remarks>
    /// Order-insensitive, because which of the two the sweep saw first is an artifact of list order
    /// and a contact is a fact about the pair.
    /// </remarks>
    private readonly record struct ContactPair(RectroObjectBase First, RectroObjectBase Second)
    {
        public bool Equals(ContactPair other)
            => (First == other.First && Second == other.Second)
                || (First == other.Second && Second == other.First);

        public override int GetHashCode() => First.GetHashCode() ^ Second.GetHashCode();
    }
}
