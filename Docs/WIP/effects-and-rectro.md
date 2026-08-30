# Effects, and a second engine to check them against

> **Maintaining this doc.** Record decisions and measured facts — not transient status, not verification
> status, not history. It's re-read by every future agent before any work starts, so everything here
> has to earn that: "we considered X and rejected it" only does when it stops *every* future reader
> asking again.

This is a working doc for the controller-unification work and the Rectro / SabbyBird proofs of
concept. It replaces the **Controllers** open question in `Docs/architecture.md`, and gets folded
into that doc once it's implemented.

**The shared layer and the Rectro half are built; the Aether half is not.** Everything below about
`GameEffectBase`, `IEffectContext`, Rectro and SabbyBird describes code. The three kinds of effect,
the Aether context, and everything under Open are still design.

`Sabric.Engine` carries these concepts on the strength of two implementations to check them against
rather than one. The recorded objection to a physics-agnostic layer is that it has "no second
candidate to check the abstraction against", and **Rectro is that second candidate** — which is what
Rectro is for.

---

## Effects — the naming

**Sabric's concept is an *effect*, not a controller.** `GameEffectBase`, `AetherEffectBase`,
`AddEffect`. This pairs with events on the axis that actually distinguishes them: **an event is
something that happens once, discretely; an effect is something ongoing, applying every tick.**

It also frees the word *controller* to mean Aether's `Controller` and nothing else, which matters
because all three kinds below traffic in Aether controllers. `GameController` was the first
candidate and was dropped: a game controller is a gamepad to almost everyone, and the input
abstraction is a live open question that will land in the same files.

*Effect* collides mildly with visual effects; it agrees with *status effect*, which is the same
concept. Judged worth it.

## The three kinds of effect in an Aether space

Every effect in a space is a `GameEffectBase` and the space's list means one thing. The Aether layer
knows three ways to get one into the `World`:

1. **A wrapper around a native Aether controller** — `AetherControllerEffect(GravityController)`.
   On being added, it puts the underlying Aether controller straight into the `World`. Its own
   `Update` is never called, and throws `UnreachableException`.
2. **An effect that has never heard of Aether.** On being added, the *space* puts a forwarding
   Aether `Controller` into the world; that forwarder's `Update` builds a context and calls the
   effect's own update.
3. **An `AetherEffectBase` subclass** — the analogue of `AetherObjectBase` and `AetherSpace`: a
   native Sabric effect that knows about Aether's physics concepts. Same forwarding mechanism as
   kind 2, but handed an `IAetherEffectContext` rather than a plain one.

**Nothing on the Sabric side inherits from Aether's `Controller`.** It is a class rather than an
interface, so a Sabric base cannot derive from both it and `GameEffectBase`. That is why all three
kinds wrap rather than inherit — kind 3 included, despite being the one that knows about Aether.

### Kind 1's throwing `Update` is correct, not a hole

The contract of `Update` is **the engine calls it; the game implements it**. Game code has no
context object to pass, so it cannot call `Update` at all. When the thing in the world is a native
Aether controller rather than a forwarder, nothing is ever going to call it. `UnreachableException`
is an accurate statement of that — reaching it means the engine broke its own contract, which is
what distinguishes it from `NotSupportedException` (a caller asked for something unsupported) and
from `NotImplementedException` (a gap someone should come back and fill).

This is not a Liskov problem: the only caller is the engine, and the engine knows by construction
not to make that call.

**Kind 1 is an Aether-specific category.** It exists only because Aether ships pre-built controllers
we didn't write. Rectro ships none, so every Rectro effect has a real `Update` — which is also why
`Update` stays on the base rather than on an intermediate "updating effect" type. Rectro's space
walks its effect list and calls them, having no foreign `World` to delegate to; splitting `Update`
off the base would make the engine that needs it filter, to spare the engine that never calls it.

## The effect context

**The context is the whole seam, in both directions.** It answers reads with live state, and it is
where pushes go — `context.ApplyAcceleration(obj, v)` rather than `obj.ApplyAcceleration(v)`. Two
things that buys: `GameObjectBase` doesn't grow public methods that are only meaningful mid-tick,
and "what is true right now" and "where does the push land" are one seam instead of two.

The read side exists because an effect runs *inside* Aether's step, where the object's own
properties are still those of the last `SyncFromWorld` while the body has already moved. Harmless
for something that only pushes; wrong for anything that steers.

```
IEffectContext          GetPosition / GetVelocity / ...; ApplyAcceleration / ApplyDeltaV
  IAetherEffectContext  + ApplyForce / ApplyImpulse, and Aether's physics concepts
```

Acceleration and delta-v are kinematics and belong to any engine; force and impulse presuppose mass
and belong to the physics side. The Aether implementation multiplies by mass on the way through.

**One context object, handed out typed differently.** `IAetherEffectContext` derives from
`IEffectContext`, so a single instance serves both; kind 2 sees the narrow interface and kind 3 the
wide one.

### Ordering visibility is engine-defined, deliberately

Whether an effect sees an earlier effect's push within the same tick is **not contractual**. Aether
cannot do otherwise than make it visible — a delta-v is an impulse, which moves `Body.LinearVelocity`
immediately — and Rectro's simplest workable implementation applies immediately too, so the two agree
in practice today. Leaving it unstated is a choice not to tie the abstraction's hands to a behaviour
nothing yet depends on. It can be made contractual later if something wants it.

## What the shared layer is

- **`Update(long delta, IEffectContext context)`.** Milliseconds, matching `GameSpaceBase.Advance`,
  rather than whatever unit the engine underneath steps in. Delta is a parameter rather than
  something the context carries, so that it reads like `OnAdvance(long delta)`.
- **The effect list is on the non-generic `GameSpaceBase`**, and is a plain `List` where
  `GameObjects` is a `NotifyingList`: an effect is never rendered, so there is nothing for a view to
  subscribe to.
- **`AddEffect` / `RemoveEffect` mirror `Add` / `Remove`** — attach, hook, list on the way in; list,
  hook, detach on the way out — and `GameEffectBase` has the same `Space` / `IsAttached` /
  `OnAttached` / `OnDetached` lifecycle a game object has. That hook is what an engine which has to
  install an effect somewhere has to work with.
- **`InputEffectBase`** is the engine-agnostic twin of `AetherInputControllerBase`, which stays put
  for as long as anything is still built on Aether's own controllers.

## Where effects sit in the tick

**Abstractly: during the tick, before events.** Anything finer is engine-dependent — Aether's
answer is "inside `World.Step`, before the solver", which is not a phase Rectro has — and the
context is what papers over the difference.

## What moves in the object model

- **`Rotation` and `AngularVelocity` move up to `GameObjectBase`.** This reverses the previous
  position that rotation isn't a concept the abstract layer carries. The reason it moves: **the
  boundary between the generic layer and the Aether layer is the existence of physics**, and
  rotation exists perfectly well without physics — a top-down car racing game rotates its cars and
  has no solver anywhere.
- **`Mass` is added to `AetherObjectBase`**, and stays out of `GameObjectBase`: acceleration and
  delta-v are meaningful in a game with no physics engine, and mass is not.

  It is **independent of density and radius**, because computing it from those doesn't generalize
  past planets. `PlanetBase` sets it once from its radius and density; after that it changes only if
  something sets it. Exact mechanism undecided.

---

## Rectro

`Sabric.Engine.Rectro` — a purely X/Y, axis-aligned-rectangle engine, existing to exercise the
`Engine` / `Aether` seam from the other side.

**It resolves collisions without physics.** Two rectangles that meet have the velocity component on
the collision axis set to zero, are positioned adjacent rather than overlapping, and the general
collision event fires. You collide, you stop dead on that axis. No momentum is exchanged.

That is what makes it a real test of the collision split: Rectro **cannot** produce an impulse, so
`CollisionInfo` carrying point / normal / approach speed and `AetherCollisionInfo` adding impulse is
exactly the line it needs to be. Approach speed is relative velocity along the normal and needs no
solver, so Rectro computes it fine.

## SabbyBird

`Sab39.SabbyBird` — a new repo, already scaffolded with its Engine project. Like Sporbits but
minimal, as the smallest real consumer of Rectro.

```
Sab39.SabbyBird.Engine
Sab39.SabbyBird.UI.BlazorSVG
Sab39.SabbyBird.UI.BlazorSVG.Web
Sab39.SabbyBird.UI.BlazorSVG.Web.Client
```

Deliberately **no `.Core` and no plain `.UI`** — whether those layers pull their weight is an open
question in `architecture.md`, and SabbyBird is a chance to find out by going without them.

**It is playable.** Gravity is one effect, the flap a second, and a third keeps a stream of pipes
coming; the bird dies on touching a pipe or on leaving the world through the top or the bottom.
Between them those exercise `ApplyAcceleration`, `ApplyDeltaV` and the read side — the flap is
spelled as a delta-v computed from the current velocity precisely so that it sets an upward speed
rather than stacking, which is the read half earning itself in an engine where nothing is ever
stale. There is no score yet.

`DiagnosticsView` is an on-screen readout of where the pipe gaps are coming from, and is the one
thing in the tree that re-renders per tick.

---

## Notes for implementation

Not decisions — traps and mechanical consequences worth not rediscovering.

- **The Aether forwarder should use the space's own `delta`** rather than converting Aether's
  `float dt` in seconds back to milliseconds. `AetherSpace.OnAdvance` passes
  `TimeSpan.FromMilliseconds(delta)` into `World.Step`, so it still holds the exact value and there
  is no rounding question.
- **An effect that adds or removes *effects* during the sweep has no answer.** Rectro walks
  `Effects` with a plain `foreach`, so doing it throws. Adding and removing game *objects* mid-sweep
  is fine, and SabbyBird's pipes depend on it.
- **Setting `Mass` has an ordering trap with fixtures.** Aether recomputes a body's mass from its
  fixtures in `ResetMassData()`, called on `CreateFixture` — Box2D behaviour inherited by the port,
  worth confirming against 2.2.0 before relying on it. If so, `PlanetBase` must set mass *after*
  `InitializeBody` creates the circle fixture, or it is silently clobbered.
- **Rectro's resting contact sits exactly on the overlap test's boundary.** Resolving to adjacency
  means a zero gap, which is a natural way to get begin- and end-contact firing on alternate ticks
  forever. It won't bite in SabbyBird, where contact is death and nothing rests, but it will in the
  first Rectro game with a floor.
- **`AetherSpace` keeps a raw `AddController(Controller)`** as a convenience over
  `AddEffect(new AetherControllerEffect(controller))`. A one-liner or extension method; nothing about
  the design turns on whether it exists.

## Open

- **Does `Effect` absorb the *rule* concept?** `architecture.md` leaves open whether a rule is wanted
  alongside events — a per-space thing updating every tick after the sweep, the mirror of an effect
  on the far side of the step. Under a definition of *effect* that is about being ongoing rather than
  about which side of the step it runs on, whether rule survives as a separate category is unsettled.
- **How kind 3 receives the narrowed context.** C# cannot narrow a parameter in an override, so
  `AetherEffectBase` cannot simply override an `Update(IEffectContext)` with
  `Update(IAetherEffectContext)`. Unresolved; Claude offered a sealed-hook-plus-cast (the same shape
  as `AetherObjectBase.Space => (AetherSpace)base.Space`) and a `GameEffectBase<TContext>` mirroring
  `GameSpaceBase<TObject>`, and neither has been chosen.
- **What installs an effect into an Aether world, and what removes it.** Kinds 2 and 3 are put there
  by the space; kind 1 puts itself there. Whether that asymmetry is expressed as an attach hook on
  the effect, a type test in the space, or something else is undecided, as is the removal half.
- **A better name than `GameEffect`**, if one turns up. Nothing is wrong with it; it just hasn't been
  lived with yet.
