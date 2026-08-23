using nkast.Aether.Physics2D.Dynamics;

namespace Sab39.Sabric.Engine.Aether;

/// <summary>
/// A running game: the physics world, the objects in it, and the tick loop that advances both.
/// </summary>
/// <remarks>
/// This lives in the Aether layer rather than in Sabric.Engine because it hands out Aether's
/// World directly. Which half of it is physics-agnostic is undesigned - see
/// Docs/WIP/sporbits-revival.md in the Sporbits repo.
/// </remarks>
public abstract class GameBase
{
    public World World { get; } = new()
    {
        Gravity = default,
    };

    public int Ticks { get; private set; }
    public long FirstTickStamp { get; private set; }
    public long LastTickStamp { get; private set; }
    public long Delta { get; private set; }
    public long TotalMillis => LastTickStamp - FirstTickStamp;

    private readonly List<GameObjectBase> gameObjects = [];

    public IReadOnlyList<GameObjectBase> GameObjects => this.gameObjects;

    public virtual void Init()
    {
        Ticks = 0;
        World.Enabled = true;
    }

    public virtual void AddGameObject(GameObjectBase obj)
    {
        obj.EnsureInitialized();
        this.gameObjects.Add(obj);
    }

    public void Tick(long tickStamp)
    {
        if (Ticks == 0)
        {
            FirstTickStamp = tickStamp;
            LastTickStamp = tickStamp;
        }

        Ticks++;
        Delta = tickStamp - LastTickStamp;
        LastTickStamp = tickStamp;

        World.Step(TimeSpan.FromMilliseconds(Delta));
    }
}
