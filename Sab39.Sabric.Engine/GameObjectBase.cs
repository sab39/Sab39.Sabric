using System.Numerics;

using Sab39.Core.Components;

namespace Sab39.Sabric.Engine;

/// <summary>
/// Anything that exists in a game space, with a position and a velocity in it.
/// </summary>
/// <remarks>
/// A game object is constructed inert - no space, and none of whatever a physics implementation
/// builds for it - and becomes live when a space attaches it. Attaching is the moment a derived
/// type can rely on: it runs after the constructor by definition, so nothing has to be deferred
/// behind a flag, and an object can exist before any space does.
///
/// Position and Velocity are the authoritative copy, which is what lets a detached object still
/// mean something. A physics implementation loads its own state from them before it steps.
/// </remarks>
public abstract class GameObjectBase : IPropertyChange, IChangeNotifier
{
    public Guid GameObjectId { get; } = Guid.NewGuid();

    private GameSpaceBase? space;

    /// <remarks>
    /// Typed non-null although it genuinely is null before a space attaches this object and again
    /// after one detaches it. The window is short and no ordinary use of an object falls inside it,
    /// so declaring the exception would tax every call site to describe a state none of them are in.
    /// Nullable reference types are advisory, so code with a real reason to doubt can still ask -
    /// <see cref="IsAttached"/> is that question, and it reads the field rather than this.
    ///
    /// Narrowed by covariant override rather than by a second field, so a derived type sees its own
    /// space type without paying for the storage.
    /// </remarks>
    public virtual GameSpaceBase Space => this.space!;

    public bool IsAttached => this.space is not null;

    public Vector2 Position { get; set => this.SetProperty(ref field, value); }
    public Vector2 Velocity { get; set => this.SetProperty(ref field, value); }

    /// <summary>
    /// Which way this object is facing, in radians.
    /// </summary>
    /// <remarks>
    /// Here rather than on the physics side, because the boundary between this layer and an engine's
    /// is the existence of physics and rotation exists perfectly well without it - a top-down car
    /// racing game rotates its cars and has no solver anywhere.
    /// </remarks>
    public float Rotation { get; set => this.SetProperty(ref field, value); }

    /// <inheritdoc cref="Rotation"/>
    public float AngularVelocity { get; set => this.SetProperty(ref field, value); }

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

    internal void Attach(GameSpaceBase space)
    {
        this.space = space;
        OnAttached();
    }

    internal void Detach()
    {
        OnDetached();
        this.space = null;
    }

    /// <remarks>
    /// <see cref="Space"/> is set before OnAttached runs and still set while OnDetached does, so
    /// both can reach whatever the space provides. Between them is the object's whole live period.
    /// </remarks>
    protected virtual void OnAttached() { }
    protected virtual void OnDetached() { }

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
