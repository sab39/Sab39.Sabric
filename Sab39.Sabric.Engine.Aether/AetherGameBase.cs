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
public abstract class AetherGameBase : GameBase
{
    public World World { get; } = new()
    {
        Gravity = default,
    };

    public override void Init()
    {
        base.Init();
        World.Enabled = true;
    }

    public override void Tick(long tickStamp)
    {
        base.Tick(tickStamp);
        World.Step(TimeSpan.FromMilliseconds(Delta));
    }
}
