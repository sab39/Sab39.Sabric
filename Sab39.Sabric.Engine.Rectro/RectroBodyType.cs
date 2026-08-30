namespace Sab39.Sabric.Engine.Rectro;

/// <summary>
/// How a rectangle takes part in movement and in collision resolution.
/// </summary>
/// <remarks>
/// The same three cases Aether's own BodyType has, kept because Rectro needs all three: something
/// that never moves at all, something that moves and is never pushed back by what runs into it,
/// and something that is.
/// </remarks>
public enum RectroBodyType { Static, Kinematic, Dynamic }
