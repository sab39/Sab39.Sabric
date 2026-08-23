using nkast.Aether.Physics2D.Common;

namespace Sab39.Sabric.Engine.Aether;

/// <summary>
/// One contribution to where the player is currently trying to go. Sources are summed, so a
/// direction of any length is meaningful and zero means "nothing from me".
/// </summary>
public interface IPlayerInputSource
{
    Vector2 MovementDirection { get; }
}
