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

    private readonly List<GameObjectBase> gameObjects = [];

    public IReadOnlyList<GameObjectBase> GameObjects => this.gameObjects;

    public virtual void Init() => Ticks = 0;

    public virtual void AddGameObject(GameObjectBase obj)
    {
        obj.EnsureInitialized();
        this.gameObjects.Add(obj);
    }

    public virtual void Tick(long tickStamp)
    {
        if (Ticks == 0)
        {
            FirstTickStamp = tickStamp;
            LastTickStamp = tickStamp;
        }

        Ticks++;
        Delta = tickStamp - LastTickStamp;
        LastTickStamp = tickStamp;
    }
}
