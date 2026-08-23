using nkast.Aether.Physics2D.Common;
using nkast.Aether.Physics2D.Dynamics;

namespace Sab39.Sabric.Engine.Aether;

/// <summary>
/// Anything that exists in the game world, wrapping the Aether body that gives it a position.
/// </summary>
/// <remarks>
/// Initialization is deferred rather than done in the constructor: a derived type's
/// InitializeBody needs the fully-constructed object, and touching Body is what triggers it.
/// </remarks>
public abstract class GameObjectBase(GameBase game, Vector2 initialPosition = default, float initialRotation = 0, BodyType bodyType = BodyType.Dynamic)
{
    public Guid GameObjectId { get; } = Guid.NewGuid();

    public GameBase Game { get; } = game;
    public World World => Game.World;

    public Body Body
    {
        get
        {
            EnsureInitialized();
            return field;
        }
    } = game.World.CreateBody(initialPosition, initialRotation, bodyType);

    public Vector2 Position { get => Body.Position; set => Body.Position = value; }
    public Vector2 Velocity { get => Body.LinearVelocity; set => Body.LinearVelocity = value; }

    public float Rotation { get => Body.Rotation; set => Body.Rotation = value; }
    public float AngularVelocity { get => Body.AngularVelocity; set => Body.AngularVelocity = value; }

    private bool isInitialized;

    public void EnsureInitialized()
    {
        if (this.isInitialized) return;

        // Set before initializing, not after: Initialize reaches Body, whose getter comes
        // straight back here.
        this.isInitialized = true;
        Initialize();
    }

    protected virtual void Initialize() => InitializeBody();

    protected abstract void InitializeBody();
}
