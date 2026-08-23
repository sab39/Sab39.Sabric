using System.Numerics;

namespace Sab39.Sabric.Engine;

/// <summary>
/// Anything that exists in the game world, with a position and a velocity in it.
/// </summary>
/// <remarks>
/// Initialization is deferred rather than done in the constructor: a derived type's Initialize
/// needs the fully-constructed object. Adding the object to a game triggers it, and a derived
/// type is free to trigger it earlier.
/// </remarks>
public abstract class GameObjectBase(GameBase game)
{
    public Guid GameObjectId { get; } = Guid.NewGuid();

    public GameBase Game { get; } = game;

    public abstract Vector2 Position { get; set; }
    public abstract Vector2 Velocity { get; set; }

    private bool isInitialized;

    public void EnsureInitialized()
    {
        if (this.isInitialized) return;

        // Set before initializing, not after: Initialize reaches members whose getters come
        // straight back here.
        this.isInitialized = true;
        Initialize();
    }

    protected abstract void Initialize();
}
