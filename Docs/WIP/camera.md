# Camera

> **Maintaining this doc.** Record decisions and measured facts — not transient status, not verification
> status, not history. It's re-read by every future agent before any work starts, so everything here
> has to earn that: "we considered X and rejected it" only does when it stops *every* future reader
> asking again.

Covers both repos: `Camera`, its behaviours and `CameraLayer` are Sabric's; the starfield, the
minimap and level bounds are Sporbits'. Read alongside Sabric's `Docs/architecture.md`, whose
settled sections this does not repeat. When the minimap and mouse control land, the Sabric half
belongs in that document and the Sporbits half in `Sab39.Sporbits\Docs\WIP\sporbits-revival.md`.

Everything below is agreed. What is genuinely open is in the last three sections, and is marked.
The first pass — camera, follow behaviour, layers and the starfield — is built; the minimap and
mouse control are not.

## It is called a Camera, not a Viewport

SVG already uses *viewport* as a spec term for the `<svg>` element's own rect — which under the split
below is the **other** thing, the static half. `Camera` is unambiguous about which half it is and
leaves the word free for its SVG meaning, the same way *controller* was left to Aether.

## Two halves, spelled separately

- **`viewBox` stays static.** It says how big a window is, in world units, and `preserveAspectRatio`
  letterboxes it. Nothing has to measure the element, so no JS interop.
- **The camera is a `<g transform>` inside it.** It says where the window is pointed.

## Where the pieces live

- **`Sabric.UI`** — `Camera`, `CameraBehaviourBase`, `FollowBehaviour`. The state, the transform
  derivable from it, and what moves it. No Blazor, no SVG; a `Sabric.UI.Headless` would still have
  one. This is the first thing to live in the project, which already referenced `Sabric.Engine`, so
  a camera can hold a `GameObjectBase` target directly.
- **`Sabric.UI.BlazorSVG`** — `CameraLayer`, which emits the `<g transform>`.
- **`Sporbits.UI.BlazorSVG`** — `Starfield`, and the minimap when it exists.

```csharp
// Sabric.UI
public sealed class Camera(GameSessionBase session) : IChangeNotifier, IPropertyChange, IDisposable
{
    public Vector2 Position { get; set => this.SetProperty(ref field, value); }
    public float Rotation { get; set => this.SetProperty(ref field, value); }
    public float Zoom { get; set => this.SetProperty(ref field, value); } = 1;

    public Vector2 Extent { get; init; }   // world units visible at Zoom 1; the viewBox's size

    public CameraBehaviourBase? Behaviour { get; set; }
    public void Update(long delta);        // gives the behaviour its turn

    public Vector2 ToWorld(Vector2 screen);
    public Vector2 ToScreen(Vector2 world);
}
```

**Rotation is in.** It is nearly free in an SVG transform, there is no up or down in space, and
disorienting the player by rolling the camera is a gameplay mechanic worth having available.

**`Extent` is the source of truth for the `viewBox`**, rather than the two being written
independently, so the window the markup describes and the window the camera thinks it is looking
through cannot drift apart.

## The camera is an `IChangeNotifier`, and that is what keeps the root still

The root's `ShouldRender` is a flat `false` "for good", and a moving camera on the root `<svg>`'s
`viewBox` would eat that. Putting the camera on an inner `<g>` instead makes `CameraLayer` an
ordinary `ChangeSubscribingViewBase` over the camera — the same machinery a planet view uses on its
planet. Per frame, one small component re-renders and one attribute changes. Nothing above it renders
again, and no new invalidation machinery is needed.

`ChildContent` is the one place a view's markup is allowed not to be a function of its source, and it
costs nothing: what's inside is components that invalidate themselves, so the group not re-rendering
never stops them.

## One `CameraLayer`, not a camera view and a parallax view

Parallax scales **translation only** — rotation and zoom apply in full at every depth. So a world
layer is exactly a parallax layer with a factor of 1, and two components would have been two names
for one behaviour.

```razor
<CameraLayer Camera="camera">                              @* the world *@
<CameraLayer Camera="camera" Parallax="0.04f" Wrap="230">  @* something distant *@
```

## Lifetime: owned by the component that renders a space

Not by the space. A space is engine-side, and split screen is two cameras over one. The component
rendering a space owns its camera, keyed on the space, so a level transition gets a fresh one. This is
the "render tree reflects lifetimes" rule applying cleanly for once, because a camera genuinely *is* a
UI thing.

## What drives it: push, off `Ticked`

**A camera is state, and a `CameraBehaviourBase` decides what that state should be this tick.** Push,
not pull — explicitly chosen, not merely implied by where it is driven from. The pull alternative
(the camera subscribes to its target's `Changed` and recomputes) is cheaper and can express nothing
but a hard lock; deadzone follow, lookahead along velocity, smoothing, shake and zoom-to-fit are all
the push shape.

**The camera owns its own subscription to `GameSessionBase.Ticked`**, taking the session in its
constructor and being `IDisposable` for it. That event already fires after the sync sweep, so the
camera never lags its target by a frame — which would show up as the player planet jittering against
the starfield. Putting the subscription inside the camera is what stops every game rewriting the same
wiring and having to remember that constraint for itself. Whatever owns the camera disposes it;
in Blazor that is the component that renders the space.

**`Camera.Behaviour` is one slot, not a list.** Whether behaviours should stack — a shake over a
follow, a zoom over both — is a real question about how they would compose, and one slot declines to
answer it rather than answering it by accident.

This deliberately does **not** settle the open *rules* question in `architecture.md`. A per-tick
thing on the far side of the step is that category's shape, and a camera behaviour would be its
second instance and its first UI-side one. Evidence for it; not a design of it.

## Mechanics worth not rediscovering

- **Transform order.** For a viewBox centred on the origin, pointing the camera at world position
  `P` is `scale(z) rotate(-θ) translate(-Pₓ, -P_y)`. Wrong order or wrong sign is the classic time
  sink here.
- **Parallax under rotation is not uniform across the transform components.** A distant layer
  *translates* by `Factor × camera position` but **rotates by the full camera angle** — infinitely
  distant stars swing right around the viewer when the viewer rolls. Scaling a distant layer's
  rotation by the parallax factor looks subtly wrong rather than obviously broken.
- **A `<pattern>` fill gives an infinite starfield for O(1) DOM nodes** — one `<rect>` per layer,
  and the browser does the tiling. Three layers whose tile sizes don't divide into each other beat
  against one another enough that the eye stops finding the grid.
- **A parallax offset grows without bound**, so a tiled backdrop of any finite size eventually runs
  out from under the view. `CameraLayer.Wrap` takes the offset modulo the tile, which is visually
  identical and bounded — so the content only has to cover one tile beyond the view rather than the
  whole reachable world. The rects are sized for that plus the rotated view's diagonal.
- **Strokes scale inside a scaled `<g>`.** `vector-effect="non-scaling-stroke"` is the out, for
  anything that wants a constant on-screen width under zoom.
- **SVG attribute values are not localised, and Blazor WASM takes its culture from the browser**, so
  anything building one from a float formats it invariantly. Pre-existing exposure: the planet views
  interpolate `cx`/`cy`/`r` directly, which would break under a comma-decimal culture.

## Sporbits: the starfield is deliberately barely-there

An aesthetic decision, and a firm one. **Even the nearest star is an absurdly long way away** on the
scale of a game about nudging planets and solar systems around, so anything that reads as a painted
backdrop being dragged past is wrong.

`Starfield` is three layers. **The faintest sits at `Parallax="0"` — fixed against the sky**, which
under a translation-only parallax means it never slides but still swings when the camera rolls: the
fixed stars, an absolute celestial frame. The two nearer, brighter layers move at 0.015 and 0.04,
which is enough to notice and not enough to look like scenery.

Brightness carries depth alongside size, so the fixed layer is also the faintest.

### And that leaves nothing on screen that shows you are moving

The subtlety is right and it has a consequence that has to be solved rather than traded away:
**a background that correctly barely moves is a background that gives the player no sense of their
own movement.** With everything distant effectively fixed, the only things on screen that move are
the planets, and flying around looks like standing still. What the background should actually be is
open.

`MotionGrid` is the stopgap in the meantime — a faint grey grid at `Parallax="1"`, so it moves,
turns and scales with the world exactly as the planets do. It is meant to be replaced, not kept.

## The minimap bypasses the view seam, and that is a wart

The minimap is a second camera over the same space that wants **different views for the same
objects** — dots, not planets. Views resolve from DI by object type alone, so there is no way to ask
for a different rendering of the same object.

**First pass draws circles from `Position` and `Radius` directly, bypassing the seam entirely.** This
is a knowing bypass, not the design, and it is wanted revisited sooner rather than later: having built
a seam whose whole point is independence between a view and its game object, the first thing built on
it needs to vary along a second axis the seam has no way to express. Whatever answers it — a view role
or channel carried through registration and resolution — is undesigned.

## Sporbits: minimap scale and level bounds

**The minimap scales itself to fit** the player, the puck, and a level-defined initial area. This is
what avoids needing a global notion of how big a space is.

Open, and Stuart's own idea rather than a settled plan: whether levels get a **bounding ellipse** that
the player and puck may not leave, losing on contact. It would give the minimap a fixed frame, and the
same mechanism would serve the shrinking-circle survival objective. The level idea it sits least well
with is a steady stream of planets flying in from off-screen.

`Sabric.Engine` has no concept of a space's extent, and nothing here has established that it should.

## Mouse input is downstream of this

"Accelerate toward the cursor while LMB held" needs screen→world, which is `Camera.ToWorld`. That is
one fact bearing on the open input question in `architecture.md`: *"the player is trying to go this
way"* and *"the pointer is here"* are not the same kind of thing, because the second is meaningless
until a camera interprets it. Whether that means the input abstraction and the camera should land in
`Sabric.UI` together is not decided.

## What is left

The minimap, mouse control, and any camera behaviour beyond `FollowBehaviour`. All three sit on top
of what exists without changing it.

`FollowBehaviour` is a hard lock with no lag, which is the plainest thing that works and is very
likely not what the game eventually wants — a camera welded to the player makes the world look like
the thing that is moving. A deadzone or a smoothed follow is a different behaviour rather than an
option on that one.
