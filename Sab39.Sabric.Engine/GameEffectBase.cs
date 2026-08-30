namespace Sab39.Sabric.Engine;

/// <summary>
/// Something ongoing in a space, applied every tick: gravity, a thruster, a status effect.
/// </summary>
/// <remarks>
/// The pair to an event, on the axis that actually distinguishes them - an event is something that
/// happens once, discretely; an effect is something ongoing, applying every tick. Abstractly it runs
/// during the advance, before events; anything finer is engine-dependent, and
/// <see cref="IEffectContext"/> is what papers over the difference.
///
/// An effect is not a game object: it has no position and is never rendered, which is why a space
/// lists the two separately. See Docs/WIP/effects-and-rectro.md.
/// </remarks>
public abstract class GameEffectBase
{
    private GameSpaceBase? space;

    /// <remarks>
    /// Typed non-null, narrowed by covariant override, and null outside the effect's live period,
    /// exactly as <see cref="GameObjectBase.Space"/> is and for the same reasons.
    /// </remarks>
    public virtual GameSpaceBase Space => this.space!;

    public bool IsAttached => this.space is not null;

    /// <summary>
    /// Applies this effect for a tick of <paramref name="delta"/> milliseconds.
    /// </summary>
    /// <remarks>
    /// Milliseconds, matching <see cref="GameSpaceBase.Advance"/>, rather than whatever unit the
    /// engine underneath happens to step in.
    ///
    /// The contract is that the engine calls this and the game implements it. Game code has no
    /// context to pass, so it cannot call this at all - which is what makes it legitimate for an
    /// effect that the engine is known never to call to throw here.
    /// </remarks>
    public abstract void Update(long delta, IEffectContext context);

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
    /// both can reach whatever the space provides. Between them is the effect's whole live period.
    /// </remarks>
    protected virtual void OnAttached() { }
    protected virtual void OnDetached() { }
}
