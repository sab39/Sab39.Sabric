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

    /// <remarks>
    /// The counterpart to the body a game object creates for itself in its constructor. Nothing
    /// else takes it out of the world, so without this a removed object would keep colliding.
    /// </remarks>
    protected override void OnRemoveGameObject(AetherGameObjectBase obj) => World.Remove(obj.Body);

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
