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

    public abstract IReadOnlyList<GameObjectBase> GameObjects { get; }

    protected virtual void OnInit() { }
    public void Init()
    {
        Ticks = 0;
        OnInit();
    }

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
    }
}

public abstract class GameBase<TGameObject> : GameBase
    where TGameObject : GameObjectBase
{
    private readonly List<TGameObject> gameObjects = [];
    public sealed override IReadOnlyList<TGameObject> GameObjects => this.gameObjects;

    protected void AddGameObject(TGameObject obj)
    {
        obj.EnsureInitialized();
        this.gameObjects.Add(obj);
        OnAddGameObject(obj);
    }
    protected virtual void OnAddGameObject(TGameObject obj) { }
}
