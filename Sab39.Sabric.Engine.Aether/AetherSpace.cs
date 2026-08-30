using System.Collections.Immutable;
using System.Numerics;

using nkast.Aether.Physics2D.Controllers;
using nkast.Aether.Physics2D.Dynamics;
using nkast.Aether.Physics2D.Dynamics.Contacts;

namespace Sab39.Sabric.Engine.Aether;

/// <summary>
/// A space whose physics is an Aether world, stepped once per advance.
/// </summary>
/// <remarks>
/// The World is handed out directly, so anything above this layer is free to use Aether's own
/// concepts where it wants them - a controller Aether shipped goes in as an effect like anything
/// else, and reaching through to a Body is still how a game asks for something this seam doesn't
/// carry.
///
/// There is no Aether-flavoured session to go with this. The space owns the physics, so a session
/// never needs to know which physics implementation is underneath it.
/// </remarks>
public abstract class AetherSpace : GameSpaceBase<AetherObjectBase>
{
    public World World { get; } = new()
    {
        Gravity = default,
        Enabled = true,
    };

    /// <remarks>
    /// One hookup for the whole world, taken here rather than per object: there is nothing to do
    /// when an object attaches and nothing to unwind when it detaches. Which body belongs to which
    /// game object is answered by <see cref="Body.Tag"/>, and never leaves this layer.
    ///
    /// These are fields on <see cref="ContactManager"/> rather than events - assignment, not
    /// subscription - so the space is the single owner of each. A game that wants its own hook
    /// takes the per-Body or per-Fixture ones instead.
    /// </remarks>
    protected AetherSpace(GameSessionBase session)
        : base(session)
    {
        World.ContactManager.BeginContact = HandleBeginContact;
        World.ContactManager.EndContact = HandleEndContact;
        World.ContactManager.PostSolve = HandlePostSolve;
    }

    /// <summary>
    /// Adds one of Aether's own controllers to the space, to run inside every step.
    /// </summary>
    /// <remarks>
    /// A convenience over the wrapper, so that a game reaching for a controller Aether shipped
    /// doesn't have to spell out that it is an effect. There is no RemoveController to match: taking
    /// one out again means holding the wrapper and calling
    /// <see cref="GameSpaceBase.RemoveEffect"/>, and inventing a lookup for a case nothing has
    /// wanted would be worse than saying so.
    /// </remarks>
    public void AddController(Controller controller) => AddEffect(new AetherControllerEffect(controller));

    /// <summary>
    /// What went into the world on each effect's behalf.
    /// </summary>
    /// <remarks>
    /// The two kinds put different things there - a wrapped controller is its own, everything else
    /// gets a forwarder - and remembering which is what makes taking it out again the same operation
    /// either way.
    /// </remarks>
    private readonly Dictionary<GameEffectBase, Controller> controllers = [];

    /// <remarks>
    /// A type test rather than a hook on the effect: an effect that has never heard of Aether has no
    /// Aether-side base to hang a virtual on, so the space needs this branch whatever else it has,
    /// at which point the virtual would buy nothing.
    /// </remarks>
    protected override void OnAddEffect(GameEffectBase effect)
    {
        var controller = effect is AetherControllerEffect wrapper
            ? wrapper.Controller
            : new EffectController(this, effect);

        this.controllers.Add(effect, controller);
        World.Add(controller);
    }

    protected override void OnRemoveEffect(GameEffectBase effect)
    {
        if (this.controllers.Remove(effect, out var controller)) World.Remove(controller);
    }

    /// <summary>
    /// How long the advance currently being run is, in milliseconds.
    /// </summary>
    /// <remarks>
    /// Stashed for the forwarders, which run inside <see cref="World.Step"/> and are handed only
    /// Aether's own seconds.
    /// </remarks>
    internal long CurrentDelta { get; private set; }

    internal AetherEffectContext EffectContext { get; } = new();

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
        CurrentDelta = delta;

        SyncToWorld();
        World.Step(TimeSpan.FromMilliseconds(delta));
        SyncFromWorld();

        // Staged during the step, queued only now. A record is not complete until the solver has
        // run, and the queue is drained after this method returns, so nothing is gained by handing
        // events over any earlier.
        foreach (var pending in this.staged)
        {
            if (pending.IsSeparation) QueueSeparation(pending.Info); else QueueCollision(pending.Info);
        }

        this.staged.Clear();
    }

    /// <remarks>
    /// Contacts are staged here rather than queued on the space directly because the impulse
    /// arrives from a second hook: <see cref="ContactManager.BeginContact"/> fires in the collide
    /// phase, before the solver, so the impulse is zero there and
    /// <see cref="ContactManager.PostSolve"/> replaces the record with a complete one later in the
    /// same step. The list preserves the order the two hooks reported things in.
    /// </remarks>
    private readonly List<StagedEvent> staged = [];

    private readonly record struct StagedEvent(Contact Contact, AetherCollisionInfo Info, bool IsSeparation);

    /// <remarks>
    /// Always true. Whether a contact happens at all is a physics decision that has to be made
    /// mid-step on pre-solve state, which is the opposite of what this seam is for; a game wanting
    /// a veto takes Body.OnCollision or the per-Fixture delegates, the same way it reaches through
    /// to Body.ApplyForce. Collision categories and Fixture.IsSensor cover most of the rest.
    /// </remarks>
    private bool HandleBeginContact(Contact contact)
    {
        if (Describe(contact) is { } info) this.staged.Add(new(contact, info, IsSeparation: false));
        return true;
    }

    private void HandleEndContact(Contact contact)
    {
        // Not Describe: the contact is over, so there is no manifold left to read.
        if (Identify(contact) is { } info) this.staged.Add(new(contact, info, IsSeparation: true));
    }

    private void HandlePostSolve(Contact contact, ContactVelocityConstraint impulse)
    {
        for (var i = 0; i < this.staged.Count; i++)
        {
            var pending = this.staged[i];
            if (pending.IsSeparation || pending.Contact != contact) continue;

            var solved = impulse.points.Take(contact.Manifold.PointCount).ToArray();
            this.staged[i] = pending with
            {
                Info = pending.Info with
                {
                    NormalImpulse = solved.Sum(point => point.normalImpulse),
                    TangentImpulse = solved.Sum(point => point.tangentImpulse),
                },
            };

            return;
        }
    }

    /// <summary>
    /// The two game objects a contact is between, or null if either body is not one of ours.
    /// </summary>
    private static AetherCollisionInfo? Identify(Contact contact)
    {
        if (contact.FixtureA.Body.Tag is not AetherObjectBase first) return null;
        if (contact.FixtureB.Body.Tag is not AetherObjectBase second) return null;

        return new()
        {
            First = first,
            Second = second,
            Friction = contact.Friction,
            Restitution = contact.Restitution,
        };
    }

    /// <remarks>
    /// Every value is copied out here rather than the <see cref="Contact"/> being kept. The solver
    /// does write its impulses back into this contact's own manifold, so reading them at dispatch
    /// time would work - but Aether pools contacts, so one that begins and ends inside a single
    /// step could be recycled first, and the failure would be a quietly wrong number rather than a
    /// throw.
    /// </remarks>
    private static AetherCollisionInfo? Describe(Contact contact)
    {
        if (Identify(contact) is not { } info) return null;

        contact.GetWorldManifold(out var normal, out var manifoldPoints);

        ImmutableArray<Vector2> points = contact.Manifold.PointCount switch
        {
            0 => [],
            1 => [manifoldPoints[0].AsSystem()],
            _ => [manifoldPoints[0].AsSystem(), manifoldPoints[1].AsSystem()],
        };

        // Closing speed is positive, so the sign is flipped: the normal points from A towards B,
        // and B moving that way is the two separating rather than meeting.
        var contactNormal = normal.AsSystem();
        var approach = (contact.FixtureB.Body.LinearVelocity - contact.FixtureA.Body.LinearVelocity).AsSystem();

        return info with
        {
            Points = points,
            Point = points.Length > 1 ? (points[0] + points[1]) / 2 : points.FirstOrDefault(),
            Normal = contactNormal,
            ApproachSpeed = -Vector2.Dot(approach, contactNormal),
        };
    }
}
