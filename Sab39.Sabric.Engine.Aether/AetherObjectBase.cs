using Sab39.Core.Components;

using nkast.Aether.Physics2D.Dynamics;

namespace Sab39.Sabric.Engine.Aether;

/// <summary>
/// A game object whose position and velocity are those of an Aether body.
/// </summary>
/// <remarks>
/// The body belongs to the space, so it is created on attach and destroyed on detach. Nothing
/// about the object survives that except the state it holds itself, which is the point: the same
/// object can be attached to a different space later, and a detached one is still worth reading.
///
/// Position and Velocity are declared by GameObjectBase, which has no business knowing Aether
/// exists, so they're mirrored from here by the type-level form of the attribute. Rotation and
/// AngularVelocity are declared here and say so themselves.
/// </remarks>
[SyncProperty(nameof(Position), nameof(Body))]
[SyncProperty(nameof(Velocity), nameof(Body), nameof(Body.LinearVelocity))]
public abstract partial class AetherObjectBase : GameObjectBase
{
    // Cast rather than a second backing field: the cast is provably safe from the space that
    // attached it, and costs nothing a JIT can't fold.
    public override AetherSpace Space => (AetherSpace)base.Space;

    public BodyType BodyType { get; init; } = BodyType.Dynamic;

    private Body? body;

    /// <summary>
    /// The Aether body backing this object while it is attached to a space.
    /// </summary>
    /// <remarks>
    /// Non-nullable to callers, and throwing when there is no body, rather than nullable
    /// everywhere: game code holding an object that isn't in a space is already doing something
    /// wrong, and a nullable Body would push attachment into the generated sync code, which has no
    /// business knowing about it. <see cref="GameObjectBase.IsAttached"/> is the question to ask
    /// where the answer is genuinely in doubt.
    /// </remarks>
    public Body Body => this.body ?? throw NotAttached();

    public World World => Space?.World ?? throw NotAttached();

    private InvalidOperationException NotAttached()
        => new($"{GetType().Name} is not attached to a space.");

    // Written out rather than partial only because it needs an initializer, which the generated
    // half has no way to know about.
    [SyncWith(nameof(Body))]
    public float Rotation { get; set => this.SetProperty(ref field, value); }

    [SyncWith(nameof(Body))]
    public partial float AngularVelocity { get; set; }

    /// <remarks>
    /// The Tag is how <see cref="AetherSpace"/> gets from a body back to the object that owns it,
    /// which is what lets collisions be reported for the whole world in one hookup rather than
    /// per object. It goes no further than that layer.
    /// </remarks>
    protected override void OnAttached()
    {
        this.body = World.CreateBody(Position.AsAether(), Rotation, BodyType);
        this.body.Tag = this;
        InitializeBody();
    }

    /// <remarks>
    /// The counterpart to the body created on attach. Nothing else takes it out of the world, so
    /// without this a removed object would keep colliding.
    /// </remarks>
    protected override void OnDetached()
    {
        World.Remove(this.body);
        this.body = null;
    }

    protected abstract void InitializeBody();
}
