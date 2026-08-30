using System.Numerics;

using Sab39.Core.Components;

namespace Sab39.Sabric.Engine.Rectro;

/// <summary>
/// A game object that is an axis-aligned rectangle, centred on its position.
/// </summary>
/// <remarks>
/// Nothing is built when this attaches to a space, because there is nothing to build: the object's
/// own Position and Velocity are what the space moves, so a Rectro object is its own physics state.
/// That is what stands in for <c>AetherObjectBase.Body</c>, and it is why there is no sync sweep on
/// this side - there is no second copy of anything to keep in step.
/// </remarks>
public abstract class RectroObjectBase : GameObjectBase
{
    // Cast rather than a second backing field, as on the Aether side: provably safe from the space
    // that attached it, and free once the JIT has folded it.
    public override RectroSpace Space => (RectroSpace)base.Space;

    public RectroBodyType BodyType { get; init; } = RectroBodyType.Dynamic;

    /// <summary>
    /// The full width and height of the rectangle, centred on
    /// <see cref="GameObjectBase.Position"/>.
    /// </summary>
    /// <remarks>
    /// Notifying like Position and Velocity, because it is renderable state and a game is free to
    /// change it - a rectangle that grows is a perfectly ordinary thing for a game to want.
    /// </remarks>
    public Vector2 Size { get; set => this.SetProperty(ref field, value); }

    /// <summary>
    /// The rectangle's two corners: <see cref="Min"/> on each axis and <see cref="Max"/> on each.
    /// </summary>
    /// <remarks>
    /// Named for the axes rather than for the screen, because which corner is "top left" depends on
    /// which way the caller has decided Y points.
    /// </remarks>
    public Vector2 Min => Position - (Size / 2);

    /// <inheritdoc cref="Min"/>
    public Vector2 Max => Position + (Size / 2);
}
