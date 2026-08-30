using Sab39.Core.Components;

namespace Sab39.Sabric.Engine;

/// <summary>
/// A running game: the objects in it, and the tick loop that advances them.
/// </summary>
/// <remarks>
/// Ticks are driven from outside and carry the caller's timestamp, so the game measures no time
/// of its own - whatever is scheduling frames decides what a tick is worth.
/// </remarks>
public abstract class GameBase
{
    public int Ticks { get; private set; }
    public long FirstTickStamp { get; private set; }
    public long LastTickStamp { get; private set; }
    public long Delta { get; private set; }
    public long TotalMillis => LastTickStamp - FirstTickStamp;

    public abstract IReadOnlyNotifyingList<GameObjectBase> GameObjects { get; }

    protected virtual void OnInit() { }
    public void Init()
    {
        Ticks = 0;
        OnInit();
    }

    /// <summary>
    /// Raised once per tick, after the tick has been fully applied.
    /// </summary>
    /// <remarks>
    /// Deliberately not raised by an OnTicked method. The OnXyz convention means "the protected
    /// virtual that raises event Xyz", and OnTick is a plain lifecycle hook with no event behind
    /// it; leaving the raiser out keeps the two from being confused for each other.
    /// </remarks>
    public event EventHandler? Ticked;

    protected abstract void OnTick(long tickStamp);
    public void Tick(long tickStamp)
    {
        if (Ticks == 0)
        {
            FirstTickStamp = tickStamp;
            LastTickStamp = tickStamp;
        }
        Delta = tickStamp - LastTickStamp;

        OnTick(tickStamp);

        Ticks++;
        LastTickStamp = tickStamp;

        Ticked?.Invoke(this, EventArgs.Empty);
    }
}

public abstract class GameBase<TGameObject> : GameBase
    where TGameObject : GameObjectBase
{
    private readonly NotifyingList<TGameObject> gameObjects = [];
    public sealed override IReadOnlyNotifyingList<TGameObject> GameObjects => this.gameObjects;

    /// <remarks>
    /// The hook runs before the object joins the list, and after it leaves. Adding and removing are
    /// both announced, and a subscriber that reacts by rendering the list must never see an object
    /// that is listed but whose registration with the world hasn't happened - or one that has been
    /// torn down but is still listed. The list is announced only when the world agrees with it.
    /// </remarks>
    protected void AddGameObject(TGameObject obj)
    {
        obj.EnsureInitialized();
        OnAddGameObject(obj);
        this.gameObjects.Add(obj);
    }
    protected virtual void OnAddGameObject(TGameObject obj) { }

    protected bool RemoveGameObject(TGameObject obj)
    {
        if (!this.gameObjects.Remove(obj)) return false;

        OnRemoveGameObject(obj);
        return true;
    }
    protected virtual void OnRemoveGameObject(TGameObject obj) { }
}
