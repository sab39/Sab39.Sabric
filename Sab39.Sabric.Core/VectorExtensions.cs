using System.Numerics;

namespace Sab39.Sabric.Core;

/// <summary>
/// The vector operations Sabric wants that <see cref="Vector2"/> doesn't already have, plus
/// mutating/non-mutating pairs for the ones where it only offers a static.
/// </summary>
public static class VectorExtensions
{
    extension(IEnumerable<Vector2> vectors)
    {
        public Vector2 Sum() => vectors.Aggregate(Vector2.Zero, (a, b) => a + b);
    }

    extension<T>(IEnumerable<T> values)
    {
        public Vector2 Sum(Func<T, Vector2> getVector2) => values.Aggregate(Vector2.Zero, (a, v) => a + getVector2(v));
    }

    extension(ref Vector2 vector)
    {
        public void Normalize()
        {
            // Vector2.Normalize divides by the length without checking it, so a zero vector comes
            // back as NaN. Leaving it alone is the more useful answer everywhere we call this.
            if (vector.LengthSquared() is not 0) vector = Vector2.Normalize(vector);
        }

        public void Clamp(float limit = 1)
        {
            if (vector.LengthSquared() > limit * limit)
            {
                vector.Normalize();
                vector *= limit;
            }
        }
    }

    extension(Vector2 vector)
    {
        public void Deconstruct(out float x, out float y) => (x, y) = (vector.X, vector.Y);

        public Vector2 Normalized()
        {
            vector.Normalize();
            return vector;
        }

        public Vector2 Clamped(float limit = 1)
        {
            vector.Clamp(limit);
            return vector;
        }
    }

    extension(Vector2)
    {
        public static Vector2 North => new(0, -1);
        public static Vector2 South => new(0, 1);
        public static Vector2 East => new(1, 0);
        public static Vector2 West => new(-1, 0);
    }
}
