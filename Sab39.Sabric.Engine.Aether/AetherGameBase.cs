using nkast.Aether.Physics2D.Dynamics;

namespace Sab39.Sabric.Engine.Aether;

/// <summary>
/// A game whose world is an Aether physics world, stepped once per tick.
/// </summary>
/// <remarks>
/// The World is handed out directly, so anything above this layer is free to use Aether's own
/// concepts - controllers especially, which Sabric has no abstraction for. See the open
/// questions in Docs/WIP/sporbits-revival.md in the Sporbits repo.
/// </remarks>
public abstract class AetherGameBase : GameBase<AetherGameObjectBase>
{
    public World World { get; } = new()
    {
        Gravity = default,
    };

    protected override void OnInit() => World.Enabled = true;

    protected virtual void SyncToWorld()
    {
        foreach (var obj in GameObjects) obj.SyncToBody();
    }
    protected virtual void SyncFromWorld()
    {
        foreach (var obj in GameObjects) obj.SyncFromBody();
    }

    protected override void OnTick(long tickStamp)
    {
        SyncToWorld();
        World.Step(TimeSpan.FromMilliseconds(Delta));
        SyncFromWorld();
    }
}
