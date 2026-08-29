using System.Numerics;

using Sab39.Core.Components;

using nkast.Aether.Physics2D.Dynamics;

namespace Sab39.Sabric.Engine.Aether;

/// <summary>
/// A game object whose position and velocity are those of an Aether body.
/// </summary>
/// <remarks>
/// Body is what makes the deferred initialization work: a derived type's InitializeBody needs
/// the fully-constructed object, and merely touching Body is enough to trigger it.
///
/// Position and Velocity are declared by GameObjectBase, which has no business knowing Aether
/// exists, so they're mirrored from here by the type-level form of the attribute. Rotation and
/// AngularVelocity are declared here and say so themselves.
/// </remarks>
[SyncProperty(nameof(Position), nameof(Body))]
[SyncProperty(nameof(Velocity), nameof(Body), nameof(Body.LinearVelocity))]
public abstract partial class AetherGameObjectBase(AetherGameBase game, Vector2 initialPosition = default, float initialRotation = 0, BodyType bodyType = BodyType.Dynamic)
    : GameObjectBase(game, initialPosition)
{
    public new AetherGameBase Game { get; } = game;
    public World World => Game.World;

    public Body Body
    {
        get
        {
            EnsureInitialized();
            return field;
        }
    } = game.World.CreateBody(initialPosition.AsAether(), initialRotation, bodyType);

    // Written out rather than partial only because it needs an initializer, which the generated
    // half has no way to know about.
    [SyncWith(nameof(Body))]
    public float Rotation { get; set => this.SetProperty(ref field, value); } = initialRotation;

    [SyncWith(nameof(Body))]
    public partial float AngularVelocity { get; set; }

    protected override void Initialize() => InitializeBody();

    protected abstract void InitializeBody();
}
