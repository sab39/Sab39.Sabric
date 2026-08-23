using nkast.Aether.Physics2D.Common;

namespace Sab39.Sabric.Engine.Aether;

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
