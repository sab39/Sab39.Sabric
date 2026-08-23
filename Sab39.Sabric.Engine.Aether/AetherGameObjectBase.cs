using System.Numerics;

using nkast.Aether.Physics2D.Dynamics;

namespace Sab39.Sabric.Engine.Aether;

/// <summary>
/// A game object whose position and velocity are those of an Aether body.
/// </summary>
/// <remarks>
/// Body is what makes the deferred initialization work: a derived type's InitializeBody needs
/// the fully-constructed object, and merely touching Body is enough to trigger it.
/// </remarks>
public abstract class AetherGameObjectBase(AetherGameBase game, Vector2 initialPosition = default, float initialRotation = 0, BodyType bodyType = BodyType.Dynamic)
    : GameObjectBase(game)
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

    public override Vector2 Position
    {
        get => Body.Position.AsSystem();
        set => Body.Position = value.AsAether();
    }
    public override Vector2 Velocity
    {
        get => Body.LinearVelocity.AsSystem();
        set => Body.LinearVelocity = value.AsAether();
    }

    public float Rotation { get => Body.Rotation; set => Body.Rotation = value; }
    public float AngularVelocity { get => Body.AngularVelocity; set => Body.AngularVelocity = value; }

    protected override void Initialize() => InitializeBody();

    protected abstract void InitializeBody();
}
