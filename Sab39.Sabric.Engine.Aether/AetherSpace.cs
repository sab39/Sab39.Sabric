using nkast.Aether.Physics2D.Dynamics;

namespace Sab39.Sabric.Engine.Aether;

/// <summary>
/// A space whose physics is an Aether world, stepped once per advance.
/// </summary>
/// <remarks>
/// The World is handed out directly, so anything above this layer is free to use Aether's own
/// concepts - controllers especially, which Sabric has no abstraction for yet. See the open
/// questions in Docs/architecture.md.
///
/// There is no Aether-flavoured session to go with this. The space owns the physics, so a session
/// never needs to know which physics implementation is underneath it.
/// </remarks>
public abstract class AetherSpace(GameSessionBase session) : GameSpaceBase<AetherObjectBase>(session)
{
    public World World { get; } = new()
    {
        Gravity = default,
        Enabled = true,
    };

    /// <remarks>
    /// Guarded on IsAttached rather than trusting the list. Everything in it is attached by
    /// construction, so this is a cheap safeguard rather than a case that is known to arise.
    /// </remarks>
    protected virtual void SyncToWorld()
    {
        foreach (var obj in GameObjects)
        {
            if (obj.IsAttached) obj.SyncToBody();
        }
    }

    protected virtual void SyncFromWorld()
    {
        foreach (var obj in GameObjects)
        {
            if (obj.IsAttached) obj.SyncFromBody();
        }
    }

    protected override void OnAdvance(long delta)
    {
        SyncToWorld();
        World.Step(TimeSpan.FromMilliseconds(delta));
        SyncFromWorld();
    }
}
