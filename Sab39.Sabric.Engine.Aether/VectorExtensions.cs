using System.Numerics;

using Sab39.Core.Components;

using AetherVector2 = nkast.Aether.Physics2D.Common.Vector2;

namespace Sab39.Sabric.Engine.Aether;

/// <summary>
/// Conversions between the system's <see cref="Vector2"/> and Aether's, which are the same two
/// floats but unrelated types - Aether declares no conversions to System.Numerics at all.
/// </summary>
/// <remarks>
/// These would be implicit conversion operators if C# had extension operators. Until it does, a
/// conversion operator has to be declared inside one of the two types it converts between, and
/// both of these belong to somebody else.
/// </remarks>
[SyncConversion]
public static class VectorExtensions
{
    extension(Vector2 vector)
    {
        [SyncConversion]
        public AetherVector2 AsAether() => new(vector.X, vector.Y);
    }

    extension(AetherVector2 vector)
    {
        [SyncConversion]
        public Vector2 AsSystem() => new(vector.X, vector.Y);
    }
}
