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
    public void Advance(long delta) => OnAdvance(delta);
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
