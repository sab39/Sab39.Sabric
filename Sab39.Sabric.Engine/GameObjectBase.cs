using System.Numerics;
using System.Runtime.CompilerServices;

using Sab39.Core.Components;

namespace Sab39.Sabric.Engine;

/// <summary>
/// Anything that exists in the game world, with a position and a velocity in it.
/// </summary>
/// <remarks>
/// Initialization is deferred rather than done in the constructor: a derived type's Initialize
/// needs the fully-constructed object. Adding the object to a game triggers it, and a derived
/// type is free to trigger it earlier.
///
/// Position is seeded here rather than read back out of whatever a derived type initialized
/// from it. These properties are the authoritative copy - a physics implementation loads its
/// own state from them before it steps - so they have to be right from construction, before
/// Initialize has run and before the first tick.
/// </remarks>
public abstract class GameObjectBase(GameBase game, Vector2 initialPosition = default) : IPropertyChange
{
    public Guid GameObjectId { get; } = Guid.NewGuid();

    public virtual GameBase Game { get; } = game;

    public Vector2 Position { get; set => this.SetProperty(ref field, value); } = initialPosition;
    public Vector2 Velocity { get; set => this.SetProperty(ref field, value); }

    /// <summary>
    /// Hands this object to <paramref name="visitor"/> with its own type as the type argument.
    /// </summary>
    /// <remarks>
    /// The body is always <c>visitor.Visit(this)</c>: inside a class body <c>this</c> is
    /// statically the concrete type, so that one call is the whole trick. Abstract here and
    /// deliberately not overridden in intermediate abstract classes, which makes the override
    /// compiler-enforced on every concrete game object.
    /// </remarks>
    public abstract TResult Accept<TResult>(IGameObjectVisitor<TResult> visitor);

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

    public event EventHandler<string?>? PropertyChanged;
    void IPropertyChange.OnPropertyChanged(string? propertyName) => PropertyChanged?.Invoke(this, propertyName);
}
