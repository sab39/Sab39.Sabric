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
public abstract class GameObjectBase(GameBase game, Vector2 initialPosition = default) : IPropertyChange, IChangeNotifier
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

    /// <remarks>
    /// The property name SetProperty supplies is dropped here rather than passed on. Every consumer
    /// so far answers any change with the same re-render, and being a plain IChangeNotifier is what
    /// lets a view subscribe to a game object with the same machinery it uses for anything else.
    /// If a consumer ever does want to know which property moved, IPropertyValueChange is the
    /// finer-grained interface to raise alongside this rather than in place of it.
    /// </remarks>
    public event EventHandler? Changed;
    void IPropertyChange.OnPropertyChanged(string? propertyName) => Changed?.Invoke(this, EventArgs.Empty);
}
