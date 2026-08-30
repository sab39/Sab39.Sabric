using Sab39.Core.Components;

namespace Sab39.Sabric.Engine;

/// <summary>
/// One populated space: the objects in it, and whatever advances them.
/// </summary>
/// <remarks>
/// The space owns the object list rather than the session, because a space is the unit a level is
/// built into and torn down with. See Docs/architecture.md.
/// </remarks>
public abstract class GameSpaceBase(GameSessionBase session)
{
    public GameSessionBase Session { get; } = session;

    public abstract IReadOnlyNotifyingList<GameObjectBase> GameObjects { get; }

    protected virtual void OnAdvance(long delta) { }

    /// <remarks>
    /// Non-virtual, with the dispatch after the hook, so a derived space cannot put itself on the
    /// wrong side of it: an override of <see cref="OnAdvance"/> runs while the physics is still
    /// mid-advance, and anything wanting settled state handles an event instead.
    /// </remarks>
    public void Advance(long delta)
    {
        OnAdvance(delta);
        DispatchEvents();
    }

    private readonly Queue<PendingEvent> pendingEvents = [];

    private readonly record struct PendingEvent(CollisionInfo Collision, bool IsSeparation);

    /// <summary>
    /// Records a contact to be raised once the advance is over.
    /// </summary>
    /// <remarks>
    /// Queued rather than raised, because a physics implementation learns about a contact from
    /// inside its own step, where the objects' own state is still that of the previous advance and
    /// the world may be locked against the spawning and despawning a handler will want to do.
    /// </remarks>
    protected void QueueCollision(CollisionInfo collision)
        => this.pendingEvents.Enqueue(new(collision, IsSeparation: false));

    protected void QueueSeparation(CollisionInfo collision)
        => this.pendingEvents.Enqueue(new(collision, IsSeparation: true));

    /// <remarks>
    /// Named for events rather than for collisions: this is the general after-advance moment -
    /// settled state, physics no longer mid-step - and collisions are its first tenant rather than
    /// its only conceivable one.
    ///
    /// Drained rather than snapshotted, so anything a handler queues is delivered in the same
    /// advance. Nothing but the physics queues today, and the physics is not running here.
    /// </remarks>
    private void DispatchEvents()
    {
        while (this.pendingEvents.TryDequeue(out var pending))
        {
            if (pending.IsSeparation) OnSeparation(pending.Collision); else OnCollision(pending.Collision);
        }
    }

    /// <summary>
    /// Called once per advance for each contact that began during it, on settled state.
    /// </summary>
    /// <remarks>
    /// On the space rather than on the objects, because the rules that want collisions are pairwise
    /// and a pair is a fact about the space - an object-level handler would make every such rule
    /// pick one of the two arbitrarily. An object-level API is wanted as well and is not built yet;
    /// see Docs/architecture.md.
    /// </remarks>
    protected virtual void OnCollision(CollisionInfo collision) { }

    protected virtual void OnSeparation(CollisionInfo collision) { }
}

public abstract class GameSpaceBase<TObject>(GameSessionBase session) : GameSpaceBase(session)
    where TObject : GameObjectBase
{
    private readonly NotifyingList<TObject> gameObjects = [];
    public sealed override IReadOnlyNotifyingList<TObject> GameObjects => this.gameObjects;

    /// <remarks>
    /// Attach, then the hook, then the list. A subscriber that reacts by rendering the list must
    /// never see an object that is listed but not yet live in the space - or one that has been torn
    /// down and is still listed. The list is announced only when the space agrees with it.
    /// </remarks>
    public void Add(TObject obj)
    {
        obj.Attach(this);
        OnAdd(obj);
        this.gameObjects.Add(obj);
    }
    protected virtual void OnAdd(TObject obj) { }

    /// <remarks>
    /// Detaching goes last for the same reason attaching goes first: the hook has to be able to
    /// reach whatever attaching built, the physics body in particular.
    /// </remarks>
    public bool Remove(TObject obj)
    {
        if (!this.gameObjects.Remove(obj)) return false;

        OnRemove(obj);
        obj.Detach();
        return true;
    }
    protected virtual void OnRemove(TObject obj) { }
}
