using System.Numerics;

using nkast.Aether.Physics2D.Dynamics;

using Sab39.Core.Components;

namespace Sab39.Sabric.Engine.Aether;

/// <summary>
/// A game object whose position and velocity are those of an Aether body.
/// </summary>
/// <remarks>
/// Body is what makes the deferred initialization work: a derived type's InitializeBody needs
/// the fully-constructed object, and merely touching Body is enough to trigger it.
/// </remarks>
public abstract class AetherGameObjectBase(AetherGameBase game, Vector2 initialPosition = default, float initialRotation = 0, BodyType bodyType = BodyType.Dynamic)
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

    public float Rotation { get; set => this.SetProperty(ref field, value); } = initialRotation;
    public float AngularVelocity { get; set => this.SetProperty(ref field, value); }

    protected override void Initialize() => InitializeBody();

    protected abstract void InitializeBody();

    protected internal virtual void SyncToBody()
    {
        Body.Position = Position.AsAether();
        Body.LinearVelocity = Velocity.AsAether();
        Body.Rotation = Rotation;
        Body.AngularVelocity = AngularVelocity;
    }
    protected internal virtual void SyncFromBody()
    {
        Position = Body.Position.AsSystem();
        Velocity = Body.LinearVelocity.AsSystem();
        Rotation = Body.Rotation;
        AngularVelocity = Body.AngularVelocity;
    }
}
