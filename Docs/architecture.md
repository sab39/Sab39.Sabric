# Sabric — architecture

Sabric is a small game framework: a tick loop, game objects, a physics seam, and a rendering seam
that turns a heterogeneous list of game objects into strongly-typed Blazor components. It is built
for the enjoyment of building it. Reinventing this particular wheel is the point.

**This doc knows about Sporbits.** `Sab39.Sporbits` is Sabric's only consumer, now or planned, so
the abstraction is theoretical — kept clean because a clean split is the fun part, not because a
second consumer is coming. Rather than maintain a pretence of ignorance, this doc names Sporbits
freely when a concrete example makes something clearer, and says which side of the line each piece
belongs on. The *code* keeps the split strictly; the prose doesn't have to.

## How to read this

Sections marked **settled** were worked through deliberately and shouldn't be relitigated without a
concrete reason — and if a reason turns up, it belongs in the doc. Everything under
[Open design questions](#open-design-questions) is genuinely open.

> **Maintaining this doc.** Record decisions and measured facts — not transient status, not verification
> status, not history. It's re-read by every future agent before any work starts, so everything here
> has to earn that: "we considered X and rejected it" only does when it stops *every* future reader
> asking again.

---

## Layers

Three repos. `Sab39.Core` holds infrastructure with nothing to do with games at all — the
house-style analyzer and source generators, and `Sab39.Core.Components` (change notification, see
[Property change lives in `Sab39.Core`](#property-change-lives-in-sab39core-not-in-sabric)).
`Sab39.Sabric` holds the game-agnostic framework. `Sab39.Sporbits` holds the game.

```
Sab39.Core.Components         change-notification plumbing, usable by anything

Sab39.Sabric.Core
Sab39.Sabric.Engine           engine abstractions (fully abstract)
Sab39.Sabric.Engine.Aether    Aether.Physics2D implementation of those abstractions
Sab39.Sabric.CodeGen                 generator: the Accept overrides
Sab39.Sabric.UI               frontend abstractions
Sab39.Sabric.UI.BlazorSVG     non-game-specific parts of a Blazor+SVG frontend
Sab39.Sabric.UI.BlazorSVG.CodeGen    generator: the view registrations

Sab39.Sporbits.Core
Sab39.Sporbits.Engine
Sab39.Sporbits.UI
Sab39.Sporbits.UI.BlazorSVG            the Sporbits renderers, as a Razor class library
Sab39.Sporbits.UI.BlazorSVG.Web        Blazor Web App host
Sab39.Sporbits.UI.BlazorSVG.Web.Client its WASM project
```

The app host is a **Blazor Web App**, not the standalone WASM template: static hosting has to stay
*possible* (the `.Client` project can publish standalone) without being locked in, since server-side
rendering may be wanted later for multiplayer. It's split from the `UI.BlazorSVG` class library
because that library is a *layer* and the host is an *application*.

### What each layer is meant to know

- **Sabric.Engine** — game objects, coordinates, collision, generic physics. Knows nothing about any
  specific game, and nothing about any specific physics library.
- **Sabric.Engine.Aether** — the Aether.Physics2D implementation of those abstractions. Exposes
  Aether's `World`, `Body` and `Controller` directly.
- **Sabric.UI** — a general rendering abstraction, of which BlazorSVG is just one possible
  implementation. Currently empty; see the open questions.
- **Sabric.UI.BlazorSVG** — how to render a game and its objects in SVG in the abstract; also feeds
  input (keyboard, mouse, etc.) back to the general-purpose layer's abstraction. Abstracts away
  everything game-specific.
- **A game's Engine project** — *specific* game objects like planets, and the *specific* physics
  that applies to them. Works without regard for how it's being rendered. It references
  `Sabric.Engine.Aether` directly: **Sabric** is the layer that must stay physics-engine-agnostic,
  and a game is under no obligation to be. Swappability lives at the framework boundary; a concrete
  game just picks one.
- **A game's UI.BlazorSVG project** — Blazor rendering implementations for its specific objects.
  Only things that are fundamentally *both* game-specific *and* Blazor/SVG-specific belong here.

### Naming conventions

- **Abstract base classes carry a `Base` suffix** — `GameObjectBase`, `GameSpaceBase`,
  `AetherInputControllerBase`. This is a standing convention across the Sab39 repos and it overrides
  any older naming that survives in comments or docs.
- **The `Aether` prefix is the marker for physics-engine specificity.** A type carrying it is tied
  to Aether, and a game is free to use it; a type without one has to stay agnostic.

---

## Core design decisions — settled

- **Lean on DI** for wiring implementations to abstractions, in place of any bespoke type-registry
  machinery. Loosely coupled interfaces and implementations is precisely the problem DI exists to
  solve, it solves it well, and the design should leverage it naturally rather than grow a parallel
  registry of its own. (How far this is actually carried through is an open question below; the
  decision that it *should* be is not.)
- **Public entry points are non-virtual; derived types override a `protected` hook.**
  `GameSessionBase.Init` and `Tick` do their own bookkeeping and call `OnInit`/`OnTick`;
  `GameSpaceBase.Add` attaches and calls `OnAdd`. A derived type cannot forget `base.X()` and cannot
  put itself on the wrong side of the base's work — a space stepping the world before `Delta` had
  been updated would be a silent bug, not a compile error. Same reasoning as the non-virtual
  `Dispose` in [View lifetime and disposal](#view-lifetime-and-disposal).
- **Spaces are generic in their game object type.** `GameSpaceBase<TObject>` owns the object list and
  `Add`/`Remove`; `GameSpaceBase` keeps an abstract `IReadOnlyNotifyingList<GameObjectBase>
  GameObjects`, covariantly overridden by the generic subclass. `AetherSpace` closes it as
  `GameSpaceBase<AetherObjectBase>`, so a game built on Aether sees `AetherObjectBase` statically and
  needs no type-test-and-throw. The UI layer enumerates `GameObjectBase`, so the rendering seam
  neither sees nor cares about the type argument.
- **The tick loop lives in the session** and counts ticks and elapsed time only; the space owns the
  `World` and steps it once per advance. Ticks are driven from outside and carry the caller's
  timestamp, so the session measures no time of its own.
- **Keep strong typing end to end.** Avoiding `DynamicComponent` — and the untyped, string-keyed
  parameter passing it forces — is an explicit goal, not a nice-to-have. It was the single most
  frustrating part of the previous attempt at this game.
- **Physics stays behind an abstraction (tentative).** `Sabric.Engine` should be fully abstract, with
  `Sabric.Engine.Aether` building on it the same way `Sabric.UI.BlazorSVG` builds on `Sabric.UI`. The
  motivation is that the abstraction ought to be able to express something like Mario or Flappy Bird,
  where a real physics engine would be silly. This is subject to working through what the API seam
  actually looks like and whether it turns out to be sensible — the counter-pressure is that a fully
  abstract layer may require straight-up duplicating a lot of what Aether already does natively.
- **Vectors are `System.Numerics.Vector2`, not a Sabric type.** Owning one was tried and dropped. The
  only thing owning it would have bought is implicit conversion to Aether's `Vector2`, and C# has no
  extension operators, so a conversion operator has to be declared inside one of the two types —
  which rules it out whichever type is ours. Measured rather than assumed: Aether.Physics2D 2.2.0
  declares *no* `System.Numerics` interop in any of its three TFMs — no conversion operators on its
  `Vector2`, and no reference to `System.Numerics.Vectors` in the assembly. So the conversion is an
  extension method either way, and the BCL type wins on SIMD, on having
  `Dot`/`Lerp`/`Distance`/`Reflect` already, and on being what every other .NET library speaks. Both
  are two `float`s, so conversion is lossless.

  What Sabric keeps is the extensions. `Sabric.Core.VectorExtensions` has `Sum`,
  `Normalize`/`Normalized`, `Clamp`/`Clamped`, `Deconstruct`, and `North`/`South`/`East`/`West` as
  static extension properties; `Sabric.Engine.Aether.VectorExtensions` has `AsAether()` and
  `AsSystem()`. Both conversions are named for the destination, which is why there's no `AsSabric` —
  Sabric doesn't own a vector type to convert *to*.

---

# The rendering seam — settled design

> This section is **decided**. It was the thing that killed the previous attempt at this game, and it
> was worked through deliberately. Don't relitigate it without a concrete reason — and if a reason
> turns up, record what it was.

## The problem

The framework owns a `@foreach` over a heterogeneous `IReadOnlyList<GameObjectBase>` and must render
the right Blazor component for each one, while knowing nothing about `PlayerPlanet` or `PuckPlanet`.
Razor markup can't help: `<Foo />` compiles to `OpenComponent<Foo>(seq)`, so anything written in a
`.razor` file needs its component types statically.

## The key insight

`DynamicComponent` is not the only way to render a component whose type isn't known at compile time —
it's just the only *ergonomic* one, and it pays for that by erasing types. `RenderFragment` is simply
`Action<RenderTreeBuilder>`, an ordinary delegate that can be constructed in C#, passed around, and
dropped into markup as `@fragment`. So the fragment can be built by an object that DI resolved, which
means that object can be generic, which means the types are statically known at the moment
`OpenComponent<T>` is called.

## The pieces

```csharp
// One interface for both halves: ComponentView implements it for an item whose component was
// named at the registration site, GameObjectViewResolver for a base type whose concrete views are
// only known once there's an object in hand. A list view can't tell which it has.
public interface IItemView<in TItem>
{
    RenderFragment Render(TItem item, object key);
}

// What a component must offer to be rendered as one. An interface rather than a base class so an
// item view is free to subscribe to its item or not.
public interface IItemComponent<in TItem> : IComponent
{
    TItem Item { set; }
}
```

**The seam has no casts in it.** A non-generic `IGameObjectView` with a default-interface-member
downcast used to exist for callers holding an object whose type they didn't know; that is now just
`IItemView<GameObjectBase>`, which the resolver implements by dispatching, so nothing anywhere casts.

**`IItemComponent` cannot promise the `[Parameter]` attribute.** Blazor finds parameters by
reflecting over the component class, and an attribute on an interface member doesn't reach the
implementation — so a view implementing the interface from scratch and omitting the attribute
compiles and then throws when first rendered. The two supplied bases get it right; deriving from one
is how the check is kept.

```csharp
// Two bases, because a subscribing and a non-subscribing one can't share a parent - the Item
// parameter is declared in both, and IItemComponent is what makes either acceptable.
public abstract class ItemViewBase<TItem> : ComponentBase, IItemComponent<TItem>;

public abstract class SubscribingItemViewBase<TItem> : ChangeSubscribingViewBase<TItem>, IItemComponent<TItem>
    where TItem : class, IChangeNotifier;

// Adds a name and nothing else - game-side markup reads better saying GameObject than Item - and
// is what the view generator matches on.
public abstract class GameObjectViewBase<TObject> : SubscribingItemViewBase<TObject>
    where TObject : GameObjectBase
{
    protected TObject GameObject => Item;
}
```

```csharp
public sealed class ComponentView<TComponent, TItem> : IItemView<TItem>
    where TComponent : IItemComponent<TItem>
{
    public RenderFragment Render(TItem item, object key) => builder =>
    {
        builder.OpenComponent<TComponent>(0);
        builder.SetKey(key);
        builder.AddComponentParameter(1, nameof(IItemComponent<TItem>.Item), item);
        builder.CloseComponent();
    };
}
```

`OpenComponent<TComponent>` is the whole point: no `DynamicComponent` in the render tree, no
`IDictionary<string, object?>`, no string-keyed parameter soup. The parameter name is a `nameof` on
the real property, and the constraint `TComponent : IItemComponent<TItem>` means the compiler
guarantees the component accepts the item.

## Dispatch: plain visitor, not CRTP

Getting from a runtime type to a static type parameter has only four doors: virtual dispatch on the
object itself; `MakeGenericType`/`MakeGenericMethod`; a cached reflective delegate; or a source
generator. Ordinary virtual dispatch is door one and needs no self-type parameter, because inside a
class body `this` is already statically that type:

```csharp
// Engine-side: learns nothing about rendering, because TResult is the visitor's
public interface IGameObjectVisitor<out TResult>
{
    TResult Visit<T>(T obj) where T : GameObjectBase;
}

public abstract class GameObjectBase
{
    public abstract TResult Accept<TResult>(IGameObjectVisitor<TResult> visitor);
}

// concrete game object
public sealed class PlayerPlanet : PlanetBase
{
    public override TResult Accept<TResult>(IGameObjectVisitor<TResult> v) => v.Visit(this);
}
```

`v.Visit(this)` infers `T = PlayerPlanet`. The UI layer implements the visitor with
`TResult = RenderFragment`, resolving `IItemView<T>` from DI. The engine learns nothing about
rendering, because the visitor is generic in its result type.

Keeping `Accept` abstract on `GameObjectBase`, and *not* overriding it in intermediate abstract
classes like `PlanetBase`, makes the override compiler-enforced on every concrete type. The only way
to get it wrong is to deliberately override at an intermediate level.

**Don't suggest CRTP.** It buys nothing over plain virtual dispatch here, and C# can't express the
`where this : TSelf` constraint that would make it pleasant — so it's `(TSelf)this` casts and
self-type ceremony through the whole hierarchy for no gain.

## Registration: closed at the call site

```csharp
public IServiceCollection AddItemView<TItem, TComponent>()          // extension on IServiceCollection
    where TComponent : IItemComponent<TItem>;

// Kept as its own overload rather than collapsed into the above: its constraint is the stronger
// check, and it's the call the view generator emits.
public IServiceCollection AddGameObjectView<TObject, TComponent>()
    where TObject : GameObjectBase
    where TComponent : GameObjectViewBase<TObject>;
```

Games add their own conveniences on top; Sporbits has

```csharp
public IServiceCollection AddPlanetView<TPlanet>()
    where TPlanet : PlanetBase
    => services.AddGameObjectView<TPlanet, PlainPlanetView<TPlanet>>();
```

**No builder type.** These hang straight off `IServiceCollection`, which is what the probe validated.
An `ISabricBuilder` was considered and isn't wanted for now.

```csharp
services.AddGameObjectViewResolver()
        .AddGameObjectView<PlayerPlanet, PlayerPlanetView>()
        .AddGameObjectView<PuckPlanet, PuckPlanetView>();
```

**The host registers the resolver itself.** Having `AddGameObjectView` quietly `TryAdd` it was tried
and rejected: this is the entry point to the whole seam, and it should be visible in the composition
root rather than arriving as a side effect of registering something else. The lifetime stays inside
`AddGameObjectViewResolver` because that's the framework layer's business, not the host's.

The constraint `TComponent : GameObjectViewBase<TObject>` puts the type check **at the registration
site** — the one place that legitimately knows about both halves. Pairing a puck with a goalpost view
is a compile error there, and the framework loop never needs the types. This replaces the previous
attempt's hand-rolled `GameObjectUIType.Supports()` runtime test, which was a service locator
standing in for what the container does natively.

Because every type argument is closed at a call site the compiler emits, and the visitor already
removed `MakeGenericType` from the lookup path, **the whole seam is reflection-free end to end** —
nothing to preserve for the trimmer, nothing to annotate for AOT.

## Where the pieces live

- **`Sabric.Engine`** — `IGameObjectVisitor<TResult>`, and `Accept` on `GameObjectBase`.
- **`Sabric.UI.BlazorSVG`**, in the project root rather than a subnamespace — `IItemView<TItem>`,
  `IItemComponent<TItem>`, `ItemViewBase<TItem>`, `SubscribingItemViewBase<TItem>`,
  `GameObjectViewBase<TObject>`, `ComponentView<TComponent, TItem>`, `ItemListViewBase<TItem>`,
  `GameObjectsView`, `GameObjectViewResolver`, and the registration extensions. `RenderFragment` is a
  Blazor type, so everything that touches one belongs on this side of the `Sabric.UI` /
  `Sabric.UI.BlazorSVG` split.
- **The game's UI.BlazorSVG project** — its own view bases and concrete views. Sporbits has
  `AddPlanetView`, `PlanetViewBase<TPlanet>` (abstract, shared geometry) and
  `PlainPlanetView<TPlanet>`.
- **The app host** — the composition root, which calls `AddGameObjectViewResolver()` and
  `AddGeneratedViews()`.

`GameObjectViewResolver` takes an `IServiceProvider` and implements `IItemView<GameObjectBase>`. The
visitor is a small private class it allocates per call, implementing
`IGameObjectVisitor<RenderFragment>` explicitly so `Visit` is only reachable through `Accept`. Per
call rather than the resolver being its own visitor because **the key has to reach `Visit` and
`Accept` has nowhere to put it** — the alternative is holding it in a field across a dispatch that is
only single-threaded by accident of where it runs today. It costs one allocation per item per *list*
render, which is a spawn or a despawn, not a frame.

**A generic fallback view is supported and is the game's to supply.** Sporbits'
`PlainPlanetView<TPlanet>` is a plain circle, for planets that are boring or whose visuals haven't
been designed yet; specialised views override it per-type via `AddGameObjectView`.

## Source generation

**Two** generators, because the two lines they replace live on opposite sides of the `Engine` /
`UI.BlazorSVG` split and neither layer should drag the other in:

- **`Sab39.Sabric.CodeGen`** emits the `Accept` override into each concrete game object's partial
  declaration. Referenced by the game's Engine project.
- **`Sab39.Sabric.UI.BlazorSVG.CodeGen`** emits an `AddGeneratedViews()` extension containing every
  registration, fully closed. Referenced by the app host — see below.

**The association needs no annotation.** A view already declares what it renders via its
`GameObjectViewBase<T>` type argument, so the generator pairs them up from that alone — it comes for
free from inheriting the right base class.

Both replaced hand-written code that had deliberately been written out first to prove the seam
worked before automating it.

### The view generator has to run in the app host, not in the class library

**Measured, and it decides the wiring.** The views are `.razor` files, so their classes are the Razor
*source generator's* output — and a source generator cannot see another generator's output in the
same compilation. Inside the game's view library the views are therefore invisible, and the generator
would emit an empty `AddGeneratedViews()`. From a project that *references* that assembly they are
ordinary metadata.

So the generator sweeps the whole compilation — the source assembly plus every referenced assembly
that references `Sab39.Sabric.UI.BlazorSVG` — rather than walking syntax. That costs the incremental
pipeline its cache (it hangs off `CompilationProvider`, which changes on every keystroke), which is
the price of seeing views that were never in source.

**In Sporbits that means `Web.Client`, not `Web`.** `Home.razor` renders `SporbitsUI` with
`InteractiveWebAssemblyRenderMode(prerender: false)`, so nothing in the seam is ever resolved
server-side; the WASM client's container is the only one that needs the registrations. This also
keeps the standalone-publish route intact, since `.Client` carries its own composition root.

`AddGeneratedViews()` is emitted into the *consuming project's* root namespace rather than into
`Sab39.Sabric.UI.BlazorSVG`, so two assemblies both running the generator can't collide. The cost is
one `using` line in `Program.cs` naming the project's own namespace.

### Diagnostics

`TreatWarningsAsErrors` is on repo-wide, so all three block the build; the severities below are what
they'd be without it.

- **SBR0001** (error) — a concrete game object isn't `partial`. `CS0534` fires too, but its advice
  ("implement Accept") is now the wrong answer.
- **SBR0002** (warning) — a game object declares its own `Accept`, so none is generated. Reported
  **on the existing method**, so a deliberate hand-written override suppresses it in the natural
  place. It's a warning because the usual cause is a leftover rather than a decision.
- **SBR0101** (warning) — more than one view for the same game object. One is registered, chosen by
  ordinal name order so the result doesn't depend on assembly walk order.

There is deliberately no "did you remember to register a view?" diagnostic. A game object with no
matching view stays a resolution-time `InvalidOperationException`, which is fine: the generator can't
tell a genuine omission from a game object that has no business being rendered.

## Consequences

Once each object is a real component with a typed parameter, the parent doesn't need to re-render at
all — the object list is stable between spawns. Each view can invalidate itself, so a frame touches
only the components that actually changed instead of invalidating the whole tree through a
`DynamicComponent` layer. This is what fixes the previous attempt's per-frame `StateHasChanged()` on
the root. What that takes is the next section.

---

# Per-view invalidation — settled design

Every view subscribes to the one thing it renders and invalidates only itself; the root renders once
and then holds still. Nothing in the system re-renders on a timer.

The two halves had to land together. Self-invalidating views under a root that still re-rendered per
tick would simply have rendered twice a tick, and a root that stopped re-rendering before the stats
were extracted would have frozen them.

## Views subscribe to the object, not to the tick

Subscribing every view to a tick event on `GameSessionBase` only relocates the fan-out — N views wake N
times a tick whether or not anything moved. A static obstacle, of which there will eventually be
many, should never invalidate at all. So the change event belongs on `GameObjectBase`, and the object
is responsible for knowing whether it changed.

## Property change lives in `Sab39.Core`, not in Sabric

The types themselves are documented in `Sab39.Core`'s `Docs/architecture.md`; what follows is the
consumer's view of them and why Sabric uses them the way it does.

`Sab39.Core.Components` holds two marker interfaces and their `SetProperty` extensions, in a single
`extension` block on each:

- `IPropertyChange` — `void OnPropertyChanged(string? propertyName)`.
- `IPropertyValueChange` — the old/new-value variant, `OnPropertyChanged<T>(string?, T, T)`. It
  exists and nothing uses it yet.

Alongside them, the coarse form and the collection built on it:

- `IChangeNotifier` — `event EventHandler? Changed`. No args, because a consumer's answer to any
  change is the same one.
- `IReadOnlyNotifyingList<out T> : IReadOnlyList<T>, IChangeNotifier` and `NotifyingList<T>`.
  **Named to be un-confusable with `ObservableCollection`**, whose notification is detailed where
  this one is deliberately not. Covariance costs nothing because `IChangeNotifier` says nothing about
  `T`, and it is what lets a game hand its own narrower list to something typed on the base. Bulk
  operations raise once; an operation that changed nothing raises not at all.

`SetProperty(ref field, value, [CallerMemberName] string? propertyName = null)` gives
`public Vector2 Position { get; set => this.SetProperty(ref field, value); }` — one line per
property, and any property added later gets notification without anyone remembering to.

This is generic plumbing rather than a game concept, so it sits in `Sab39.Core` and is reusable by
anything. `GameObjectBase` implements `IPropertyChange` **explicitly** and is an `IChangeNotifier`,
so raising is not part of the public surface and **the property name is dropped at the source rather
than ignored at the consumer**. Nothing in `System.ComponentModel` is involved, and there is no
`EventArgs` object per notification.

Being a plain `IChangeNotifier` is what lets a view subscribe to a game object with the same
machinery it uses for a list or for the pressed keys. If a consumer ever does want to know which
property moved, `IPropertyValueChange` is the interface to raise alongside this rather than in place
of it.

`SetProperty` gates on `EqualityComparer<T>.Default`, which the JIT devirtualizes for a value type
implementing `IEquatable<T>` — so `Vector2` and `float` compare without boxing, and an
awake-but-stationary body costs no notification.

## The physics sync sweep

`AetherObjectBase.Position` and `Velocity` used to read straight through to Aether's `Body`.
`World.Step` mutates the body directly, so our setters were never called and the object could not
know it had moved. That delegation was an artifact of the old code having no separation at all; it is
not a design and is gone.

**`AetherSpace.OnAdvance` is `SyncToWorld(); World.Step(delta); SyncFromWorld();`.** Each is a
`protected virtual` on the space that walks `GameObjects` calling `protected internal virtual`
`SyncToBody()` / `SyncFromBody()` on the object. `Position` and `Velocity` are ordinary
auto-properties on `GameObjectBase`; `Rotation` and `AngularVelocity` are the same on
`AetherObjectBase`, since rotation is not a concept the abstract layer carries. All four are
copied in both directions.

**Both methods are generated**, from `[SyncWith]` attributes, by the generator in `Sab39.Core.CodeGen`
— see `Sab39.Core`'s `Docs/architecture.md` for its design. Nothing about it knows Aether exists;
`Body` is just the receiver it was pointed at, and the `Vector2` conversions reach it through
`[SyncConversion]` on `VectorExtensions`.

The sweep is not an added cost — something had to walk the objects per tick regardless.

What it buys:

- **`Position` and `Velocity` stopped needing to be abstract.** They were abstract *only* so Aether
  could read through them. The abstract layer is now self-contained rather than a shape the physics
  implementation pokes holes in — which is what the Mario/Flappy Bird motivation was after.
- **Physics-driven and hand-set values behave identically**, with no carve-out for either. Setters no
  longer reach the body at all, which is why the *to*-world half exists: without it, anything
  assigned between ticks (a puck's initial `Velocity`, say) would be invisible to the physics.

  The same reasoning decides which way initial state travels: **the properties are the authoritative
  copy, so the physics seeds itself from them** — `AetherObjectBase` builds its body out of
  `Position` and `Rotation` when it attaches — rather than the properties being read back out of the
  body afterwards. Reading back would silently overwrite whatever an object initializer had said,
  since attaching happens after construction by definition.

What it introduces:

- **The relationship is bidirectional**, and the two directions have to not fight. Ordering is the
  whole answer: our values win going in, the step's values win coming out.
- **Reads between `World.Step` and `SyncFromWorld` are stale.** Aether's own controllers read the
  body directly and are unaffected, but anything Sabric-side running per tick has to run after the
  sweep.
- **The push is guarded; the pull isn't.** Writing is the expensive direction — a position goes
  through Aether's `SetTransform` and touches broadphase state, and a velocity can wake a sleeping
  body, so writing unconditionally plausibly stopped Aether ever sleeping anything. The guard is a
  plain equality check rather than dirty tracking, and it's exact rather than approximate: the pull
  copied the body's own bits into the property last tick, so the two agree bit-for-bit unless
  something assigned in between.

  Gating the *other* direction is deliberately not done. `SyncFromBody` only reads, and
  `SetProperty`'s equality check already stops a no-op read turning into a notification and a render
  — which was the only expensive consequence.

## The rest of the pieces

- **`GameSessionBase` raises a `Ticked` event** at the very end of `Tick`, after `Ticks` and `LastTickStamp`
  are updated, so a subscriber never sees a tick-old state. **Deliberately no `OnTicked` raiser.** In
  .NET convention `OnXyz` is *the protected virtual that raises event `Xyz`*, and `OnTick` here is a
  pure lifecycle hook with no event behind it; omitting the raiser keeps the two from colliding.
  Whether the `OnXyz` hooks should be renamed wholesale — WPF's `XyzCore` / `XyzOverride` is the
  convention for the overridable part of a fixed public method — is shelved, not decided.
- **`SubscribingViewBase<TSource>`** owns the whole attach/detach dance: a cached source field,
  re-subscribing in `OnParametersSet` when it changes, unsubscribing on dispose. Derived types supply
  `Source`, `Subscribe` and `Unsubscribe`. Parameterised on the source so those hooks are typed
  rather than the base holding something it has to cast.

  **`ChangeSubscribingViewBase<TSource>` seals in the `IChangeNotifier` case**, leaving derived views
  nothing to supply but `Source`. Everything but the game-stats view uses it — game objects, the
  object list, the pressed keys. `GameSessionBase.Ticked` is a richer event with no reason to become a
  coarse one, so the stats view derives from `SubscribingViewBase` directly and says how to attach.

  This reverses an earlier decision that a common change interface wasn't worth a second, coarser
  event: it turned out no second event was needed, because the property name `PropertyChanged`
  carried had no consumer.
- **`ShouldRender` gates each view on its own event.** A view renders because its source said
  something changed, whatever the parent happened to do. Redundant next to the root holding still,
  and kept anyway: it makes a view correct regardless of *why* it was re-rendered.
- **Pressed-key state is an object**, next to `KeyboardInputSource`, which takes it in place of a raw
  `SortedSet<string>`. It owns the set, exposes add/remove, and raises a change event. Two things it
  buys: the key view subscribes like everything else instead of being a special case, and the state
  comes off the root. Blazor auto-invalidates the component that *owns* an event handler, so while
  `OnKeyDown` lives on the root, the root re-renders on every keypress — and `keydown` auto-repeats
  while a key is held, making that a full-tree render at the OS repeat rate.
- **The root overrides `ShouldRender`** to a flat `false`, so automatic invalidation goes nowhere, and
  its per-tick `StateHasChanged()` goes. It can stay flat because the object list is a child
  component that invalidates itself.
- **`InvokeAsync(StateHasChanged)`, not the bare call.** Identical on single-threaded WASM today; not
  identical if anything ever renders server-side, which multiplayer may want.
- **`ComponentView` calls `SetKey`.** `@key` can't go on a bare fragment expression in a `@foreach`.
  `SetKey` applies to the currently-open frame, so the order is `OpenComponent` → `SetKey` →
  `AddComponentParameter` → `CloseComponent`. Getting it wrong fails quietly rather than loudly:
  called before `OpenComponent`, it keys whatever frame the fragment was invoked inside, and only
  throws if nothing at all is open.

  **The key is the list's, which is why it travels through `IItemView.Render`.** It identifies an
  item among its siblings, and only the list knows what makes them distinguishable — so
  `ItemListViewBase.GetKey` supplies it and every `Render` takes it. An abstract `Key` on the view is
  ruled out anyway: `SetKey` runs while the render tree is being written, before any component
  instance exists.

## View lifetime and disposal

`SubscribingViewBase<TSource>` is `IDisposable`, with **a non-virtual `Dispose` and a protected
virtual hook** — the base unsubscribes and then calls `OnDisposing()`, which derived views override.
A derived type physically cannot skip the unsubscribe, and if it forgets `base.OnDisposing()` it
loses only its own cleanup.

Don't "tidy" it into either alternative. `public virtual Dispose` relies on every derived type
remembering `base.Dispose()`, and forgetting silently leaks a subscribed, still-rendering view.
Explicit `void IDisposable.Dispose()` is worse: the reflexive Blazor idiom is `@implements
IDisposable` plus `public void Dispose()`, which under an explicit base implementation compiles
cleanly and simply never runs — whereas under the non-virtual base method it's `CS0108`, i.e. a build
failure.

No `Dispose(bool)`, no finalizer, no `GC.SuppressFinalize`, no disposed flag — there are no unmanaged
resources, Blazor's call is deterministic, and `-=` against a handler that was never added is a
no-op, so double disposal is harmless.

**Neither CA1063 nor CA1816 fires at the current analysis level**, so no carve-out was needed. If
either turns up when analyzer selection is revisited, it gets turned off rather than placated: they
encode the full unmanaged-resource dispose pattern, which is designed for a situation this codebase
doesn't have and would make this code worse.

**Subscribe in `OnParametersSet`, not `OnInitialized`, and unsubscribe from a cached field.** By
design a view only ever renders one game object, and `SetKey` reinforces that — a different object
means a new component instance, not a re-parameterised one. But the handling is defensive rather than
enforced: if the parameter is re-set by mistake, or some situation turns up where `SetKey` isn't
practical, the view unsubscribes from the previous object and subscribes to the new one and carries
on working, rather than throwing. The cached field also covers a component disposed before it was
ever parameterised, where its source is still `default!`.

This is all on `SubscribingViewBase`, which is why it exists: the stats and key views have the
identical problem against their own sources without deriving from `GameObjectViewBase`.

## Object removal needs no disposal event

Blazor already owns component lifetime, and the existing chain is correct: object leaves the
collection, parent re-renders, the diff drops the component frame, `Dispose` runs, subscription
released. A "you've been removed" event on the game object would be a second authority on lifetime
running alongside Blazor's, and it has nowhere useful to land anyway — a view cannot remove itself
from the render tree, so the best it could do is render nothing and become an invisible component
that still exists and is still subscribed.

What that chain depends on is the parent re-rendering. The root no longer does, which is what the
list view exists to restore for the one case that needs it.

## The list view

`GameSpaceBase.GameObjects` is an `IReadOnlyNotifyingList<TObject>` over a private
`NotifyingList<TObject>`, and `ItemListViewBase<TItem>` is the component that watches it. This is
what lets the root's `ShouldRender` be a flat `false`: a spawn re-renders a list's worth of fragments
rather than the whole tree, and nothing above the list ever renders again.

**Re-rendering the list does not re-render the items in it.** Each is handed the item it already had,
and a view whose source hasn't changed suppresses its own render — so a spawn costs a fragment and a
parameter set per item, and a DOM change only where the keys say something moved. That is why coarse
notification is enough: granular args would describe a difference nothing acts on.

Removal completes the chain the section above describes without needing a disposal event: the object
leaves the list, the list re-renders, the diff drops the component frame, `Dispose` runs, the
subscription is released.

- **`ItemListViewBase` is abstract with an abstract `GetKey`.** A `.razor` file can't declare
  `abstract` itself, but a modifier on one part of a partial class applies to the whole type — so the
  markup is an ordinary `@foreach` in the `.razor` and `abstract` lives in the `.razor.cs`. Worth
  knowing generally: the same trick is how a `.razor` component is made `sealed`.
- **`GameObjectsView` closes it** rather than the call site writing
  `<ItemListViewBase TItem="GameObjectBase">`. Inference would take `TItem` from whatever the space's
  list is typed as — `AetherObjectBase` — and then ask for an `IItemView` of *that*, which MS.DI
  would not find, since it does no variance resolution. Covariance on the list means closing it costs
  nothing.
- **`Add` attaches and runs its hook before the object joins the list; `Remove` runs its hook and
  detaches after the object leaves.** Both are announced, and **the list is only ever announced when
  the space agrees with it** — no subscriber can see an object that is listed but not live, or one
  torn down but still listed.
- **Removal is asymmetric with adding, and the asymmetry is invisible until tried.** `OnAttached` is
  what creates the body and `OnDetached` is what destroys it, so an override that registers the body
  somewhere has to run *after* `base.OnAttached` and unregister *before* `base.OnDetached` — the two
  halves sit either side of the base call, which reads like a mistake and isn't. What gets
  unregistered may not be symmetric either: Aether's `GravityController` has no `RemoveBody`, only
  `Gravity.Bodies`, the list `AddBody` appends to.

**Future work, wanted but not now.** Two pieces of this are Blazor plumbing with nothing
game-specific about them, and generalizing them sounds like fun:

- **The heterogeneous dispatch could be generalized past game objects** — `Accept`,
  `IGameObjectVisitor<TResult>` and the generator that writes the overrides are the same mechanism
  for any hierarchy that wants a view per concrete type. A homogeneous list needs none of it already:
  `ItemViewBase<T>`, `AddItemView<T, TView>()`, a `NotifyingList<T>`, done.
- **A `Sab39.Core.Blazor` RCL** for the parts of this that aren't Sabric's — `SubscribingViewBase`,
  `ChangeSubscribingViewBase`, `IItemView`, `IItemComponent`, `ComponentView`, `ItemListViewBase`.

---

# Session, Space and Level — settled design

The split, the naming, and the attach model are built. The DI chain is designed but not yet written —
`SporbitsUI` still constructs its own session. Controllers are deliberately *not* abstracted; see the
open question.

## Three concepts, three lifetimes

| Concept | Lifetime | Owns |
|---|---|---|
| `Game` | the app | the definition of a game: its levels, and what it registers |
| `GameSessionBase` | one playthrough | the tick loop, score, the current space, lifecycle state |
| `GameSpaceBase` | one level | the object list, the controllers, and the physics |

**Levels are what force the split.** Starting a level tears down the physics world and every object
in it while keeping the score, so those cannot be one object. Once a session outlives a space, a level
transition is building a new space and dropping the old one — and there is still no `Reset()`
anywhere, which is the property the current Blazor-component-lifetime trick gets by accident.

**A level is data, separate from the space that runs it.** A level that *is* the code populating a
space forecloses level files, a level editor, procedural generation, and "the same level but harder".
What a Level type actually looks like is open.

`Game` is the *definition* only. Nothing persists past a session. A player's ongoing relationship with
a game — high scores, unlocked levels, career — is a different thing with a different lifetime, and
gets its own type if it ever appears rather than being stuffed into `Game`.

## Naming

```
Game                the app-level definition of a game
  GameSessionBase   one playthrough
  GameSpaceBase     one populated space
    AetherSpace     ... backed by an Aether World
  GameObjectBase    unchanged
    AetherObjectBase
```

Sporbits closes these as `SporbitsSession`, `SporbitsSpace` and `SporbitsObjectBase` — the last a new
layer between `AetherObjectBase` and `PlanetBase`, for the things that aren't planets.

**The `Game` prefix is Sabric's disambiguator, and is dropped downstream.** `Space` and `Object`
alone are far too generic for a general-purpose framework; `AetherSpace` and `SporbitsObjectBase` are
already unambiguous, so prefixing those too would be paying twice. The cost is that a derived type no
longer shares a stem with its base, which is cheaper than the length.

**`Session` is tentative.** It already means something in this stack — Blazor circuits, user
sessions, scoped services — and server-side multiplayer would make the collision live, since a
`GameSession` could then span several user sessions or a user session hold several. The `Game` prefix
carries it for now; a better name gets taken if one turns up.

## Aether drops out of the session

The space owns the physics, so the old `AetherGameBase` has no successor: there is no
Aether-flavoured session, and `GameSessionBase` is physics-agnostic. This carries the
"`Sabric.Engine` stays fully abstract" goal further than the pre-split shape did, where every game
had to inherit from a concrete Aether-descended game class.

## Tick order

```
Session.Tick(stamp)
  ├─ bookkeeping: Delta, Ticks
  ├─ OnTick
  │    └─ CurrentSpace.Advance(Delta)
  │         ├─ OnAdvance
  │         │    ├─ SyncToWorld
  │         │    ├─ World.Step   ← controllers run in here, Aether's and ours alike;
  │         │    │                 collision hooks fire in here and queue
  │         │    └─ SyncFromWorld
  │         └─ DispatchEvents    ← the queue drains here, on settled state
  └─ Ticked
```

A game's own `OnAdvance` override runs *inside* `OnAdvance`, so before `DispatchEvents` — anything
wanting settled post-step state handles an event rather than overriding `OnAdvance`.

Space transitions happen *between* ticks, in the session's reaction to a space finishing — never from
inside `Advance`. That is what keeps a space being torn down from ever being observable mid-sweep.

## Objects are constructed inert and attached

```
var planet = new PlayerPlanet { Position = ..., Radius = 2 };   // no session, no space, no body
space.Add(planet);      // → Attach: creates its Body in the space's World
space.Remove(planet);   // → Detach: body out of the world
```

This is what retires `EnsureInitialized`/`isInitialized`. That pair exists only because a derived
type's body initialization needs a fully-constructed object; `Attach` runs after the constructor by
definition, so the problem stops existing rather than being worked around.

What it buys beyond that is the reason to prefer it: an object can exist before any space does, which
is what lets a level be data *describing* objects; an object can move between spaces, carrying state
into the next level; objects become serializable, so level files and save/load stay open; and objects
get constructors DI can call.

The design is already half-way there. `GameObjectBase` deliberately holds the authoritative `Position`
and `Velocity` rather than reading through to the body, so a detached object already has meaningful
state, and the sync sweep only ever walks the space's own list. What changes is that `Body` becomes
nullable, created on attach rather than in a field initializer.

**Controllers deliberately do not attach this way.** `AetherSpace.AddController` takes one of Aether's
own `Controller`s and puts it straight in the world, kept separate from `Add` rather than folded into
it: a controller has no position, is never rendered, and none of the inert-then-attach model applies
to it. Sharing one list would buy a shorter call site at the cost of a list that means two things.
That is the whole of Sabric's controller story — a holding pattern rather than a design, and the
reason it is one is an open question.

## DI stays flat: one container, the host's

**Everything registers in the host's own provider, and session and space lifetimes are plain object
lifetimes.** A session and a space are constructed, held and dropped by whatever owns them; neither is
a container concept. There is nothing to forward, nothing to dispose in a particular order, and no
second provider. In Sporbits the host is the WASM client's `WebAssemblyHostBuilder` root — the game
renders under `InteractiveWebAssemblyRenderMode(prerender: false)`, so that is the only provider game
code ever sees.

What this gives up is constructor injection of session-level services into space-level types. Those
get passed instead, which costs little: a space already holds its `Session`.

A designed app → session → space chain was worked out and measured before being put aside, as was
replacing the container outright with one that has hierarchical scopes — see
[Roads not taken](#roads-not-taken) for both, and for what was measured about them.

## The render tree reflects lifetimes; it does not drive them

Settled, begrudgingly. **A session owns its spaces and tears them down. The UI follows.** A level
transition is something the game decides, so the session builds the next space and drops the previous
one; a view keyed on the current space is disposed and rebuilt as a consequence.

The other direction is genuinely tempting and is what Sporbits does today for sessions: a game exists
because `SporbitsUI` is in the render tree, and stops existing when it leaves. No teardown code, no
reset path, and Blazor's renderer is a well-tested manager of tree-shaped lifetimes. Extending that
downwards — a space exists because a component renders it — costs almost nothing and reads well.

It is given up because **a level transition is a game event, not a UI event.** If rendering is what
makes a space exist, a session cannot move to the next level on its own: something on screen has to
cause it, and a session with no screen — a test, a replay, a headless simulation — cannot transition
at all. That is too much to trade for the lines it saves.

Two mechanical facts that any version of this has to respect:

- **DI cannot own these lifetimes, whichever direction they run.** MS.DI disposes a transient with
  the scope that created it, and in WASM that scope is the app root. So a disposable resolved into a
  component is held until the tab closes; removing the component disposes the *component*, not what
  it was given. Component `IDisposable` is the mechanism, and the container is not involved.
- **Blazor reuses a component when only a parameter changes.** `<SpaceView Space="..." />` at the
  same position in the markup is the same instance with a new value, not a new instance — so a space
  view needs `@key` on the space to be torn down and rebuilt on a transition. Without it the old view
  quietly rebinds, carrying whatever per-space state it had. It fails in the direction of appearing
  to work.

Neither bites yet: nothing in the engine is `IDisposable`, so dropping the reference is currently the
whole of teardown.

Sessions stay owned by the shell for now. Nothing is pressing on that, and the argument above is
about spaces.

---

# Collision — settled design

## Collision is a Sabric concept, unlike force

The controller question turns on force being a poor candidate for abstraction: one implementation, no
second candidate to check against, and an impulse has no authoritative copy to travel through the sync
sweep. Collision is the opposite case on every count. **Every conceivable implementation has it** — the
Mario/Flappy Bird motivation that justifies an abstract layer at all is a genre that is *entirely*
collision — and "these two touched" is implementation-neutral as a shape in a way that "apply this
impulse" is not. So `GameObjectBase` and `GameSpaceBase` learn about collision, and the Aether layer
feeds it, the same seam the `[SyncWith]` sweep solved for position and velocity.

## Handlers hang off the space

**The space is where a collision is handled; the game object is not.** The rules that want collisions
are pairwise — the puck hits *the player's planet*, the puck enters *a goal* — and a pair is a fact
about the space, not about either half of it. An object-level handler would make every such rule pick
one of the two objects to live on arbitrarily.

**An object-level API is wanted as well, and is deliberately not built yet.** "Something hit *me*" is
the more obvious thing to reach for and will want to exist; the space is the level that has to work
first, because it is where the dispatch happens either way.

## Edges only

`BeginContact` and `EndContact`, no "still touching". Continuous contact is available from Aether's
`PostSolve`, and nothing wants it; making the general abstraction edge-triggered is the commitment.

## `Advance` grows a dispatch phase

**`GameSpaceBase.Advance` is `OnAdvance(delta); DispatchEvents();`** — the same non-virtual public
entry with a protected hook as everywhere else, so a derived space cannot put itself on the wrong side
of the dispatch.

**Named `DispatchEvents`, not `DispatchCollisions`.** Collisions are the first tenant of the after-step
moment, not the only conceivable one, and the phase is the general thing: post-`SyncFromWorld`, on
settled state, with the physics no longer mid-step. This is half of what
[Rules, events, and what happens after the step](#rules-events-and-what-happens-after-the-step) was
asking; what remains open there is whether a *rule* concept is wanted alongside events.

## Nothing physics-owned crosses the step boundary

**Every value is copied out at hook time.** The queue holds Sabric's own records, never Aether's
`Contact`. Reading the impulse off the retained `Contact` at drain time was measured to work — the
solver writes impulses back into the same manifold — and is not done: Aether pools contacts, so a
contact that begins and ends inside one step could be recycled before the drain, and the failure would
be a silently wrong value rather than a throw.

The cost is that the impulse arrives from a second hook. `BeginContact` fires in the collide phase,
**before the solver**, so the manifold impulse is zero there; `PostSolve` fires later in the same step
with the solved value. So `BeginContact` queues the record and `PostSolve` fills its impulse in,
matched on `Contact` reference identity within the step.

## Two levels of collision information

```
CollisionInfo         the two objects, contact point, normal, approach speed
  AetherCollisionInfo + normal and tangent impulse, both manifold points, friction, restitution
```

The line between them is **what a physics implementation must be able to produce**, not what Aether
happens to have. Point, normal and approach speed are available to anything that detects a collision
at all — approach speed is relative velocity along the normal, which needs no solver. **Impulse
presupposes one**, so it cannot be promised by the abstract layer: an engine that resolves overlaps
without exchanging momentum has no impulse to report.

Both measure "how hard", and they are different quantities rather than two spellings of one. Approach
speed is kinematic and mass-free; impulse is momentum actually exchanged, so a heavy body drifting in
outweighs a light one arriving fast. Measured, two equal unit circles closing at 10: approach speed
10, normal impulse 15.71.

## The Aether side

**One subscription for the whole space, taken at construction.** `World.ContactManager` carries
world-level hooks, so there is no per-object hookup in `OnAttached`/`OnDetached` and nothing to unwind
on detach. Body-to-object identity is the space's own business — `Body.Tag` or a dictionary — and never
leaves the Aether layer.

Measured in Aether.Physics2D 2.2.0, and the reason the design is shaped this way:

- The hooks are **public fields on `ContactManager`, not events**: `BeginContact`, `EndContact`,
  `PreSolve`, `PostSolve`, `ContactFilter`. Assignment, not subscription — one owner, and `AetherSpace`
  is it.
- `bool BeginContactDelegate(Contact)`, `void EndContactDelegate(Contact)`,
  `void PreSolveDelegate(Contact, ref Manifold)`,
  `void PostSolveDelegate(Contact, ContactVelocityConstraint)`.
- `Contact.GetWorldManifold(out Vector2 normal, out FixedArray2<Vector2> points)` gives world-space
  contact points — one for two circles, up to two in general. The general `CollisionInfo` carries a
  single representative point; `AetherCollisionInfo` carries both.
- Solved impulses land in `Contact.Manifold.Points[i].NormalImpulse`, equal to what `PostSolve`
  reports.

**`BeginContact` returning `bool` is a veto, and `AetherSpace` always returns `true`.** Deciding whether
a contact happens at all is a physics concern that has to be answered mid-step on pre-solve state — the
opposite of everything this design is for. A game that wants it reaches through to `Body.OnCollision` or
the per-`Fixture` delegates, the same escape hatch that `Body.ApplyForce` is today. Static filtering
(collision categories, `Fixture.IsSensor`) covers most of what a veto would otherwise be used for and is
plain body configuration.

All of the above is established by the `AetherContactProbe` scratch project, which is kept.

---

# Roads not taken

Recorded so they don't get re-explored from scratch:

- **A DI chain of app → session → space works, and still isn't worth it.** MS.DI scopes are flat, so
  nesting has to be built rather than asked for: a child `ServiceProvider` per session, with a real
  `CreateScope()` per space beneath it. That was measured to do exactly what the design wanted —
  sibling spaces get distinct space-scoped services, they share their session, and tearing a space
  down leaves the session up. Three costs sank it. **Registrations don't inherit across providers**,
  so every host service game code wants must be named in a forwarding list, and a missing one fails
  at runtime. **Forwarding has a silent trap**: a provider disposes what its factories produced, so
  `AddSingleton(_ => root.GetRequiredService<T>())` makes the session's teardown dispose the *app's*
  singleton, while `AddSingleton(instance)` does not — same intent, opposite outcome, nothing catches
  it at compile time. And **`@inject` resolves from Blazor's provider regardless**, so a component
  could never have injected session-level state anyway; the container's reach stops at the engine
  boundary either way. Blazor's own scope is also not a session: in WASM `Scoped` and `Singleton` are
  the same thing, one scope for the whole app, while sessions come and go inside one app load.
- **Replacing the container with one that has hierarchical scopes doesn't rescue the chain either.**
  `WebAssemblyHostBuilder.ConfigureContainer` does let a third-party container *be* Blazor's rather
  than sit alongside it, so this was worth measuring rather than assuming. Measured on net10.0 with
  Autofac.Extensions.DependencyInjection 11.0.2, DryIoc.Microsoft.DependencyInjection 6.2.0 and
  Pure.DI 2.5.3:
  - **Autofac and DryIoc really do give the whole chain — through their own APIs.** Registrations
    inherit into nested scopes, a session-scoped service resolved from two sibling space scopes is
    one instance, sibling spaces get distinct space-scoped instances, and tearing a space down leaves
    the session and its sibling standing. Everything MS.DI cannot do.
  - **But their MS.DI adapter surface is flat, identically to MS.DI.** `IServiceScopeFactory` is
    reference-identical at every level and `CreateScope()` from inside a scope yields a sibling. An
    adapter reproduces MS.DI's semantics, which is the point of one. So nesting is reachable only via
    `BeginLifetimeScope`/`OpenScope` — meaning Sabric's engine would name a specific container's API
    to express its own lifetimes. That is the cost that actually sinks it.
  - **None of the three cascade disposal.** Disposing a parent scope leaves an open child's instances
    undisposed in all of them. Autofac at least poisons the child, so using it afterwards throws
    `ObjectDisposedException`; DryIoc leaves the orphan fully working. Every scope has to be disposed
    explicitly whatever the container, so that discipline is not something a container buys off.
  - **Pure.DI tops out at two levels.** A child composition inherits the parent's singletons but not
    its scoped instances, and there is no named or tagged scope lifetime, so a middle tier cannot be
    expressed at all — sibling spaces each got their own "session".
  - **A compile-time container can't be the framework's container regardless.** Blazor registers its
    own services at runtime with `Type` objects and factory delegates; a source generator needs the
    graph while it compiles. That rules out the reflection-free options — Jab, Pure.DI, StrongInject
    — for the "share it with Blazor" case, whatever their scoping.

  The capability is real. The price is a container-specific API in the framework layer, a dependency,
  worse trimming, and the manual disposal discipline surviving anyway — for something nothing
  currently needs.
- **Open generic DI registration cannot replace the visitor.** It solves what the container can
  *construct*, not how you *name* what you want. Asking for `IItemView<PlayerPlanet>` from a variable
  statically typed `GameObjectBase` still requires `MakeGenericType`. The two are orthogonal; open
  generics are downstream of a question you can't yet ask.
- **Open generic registration for fallback views is a trap, not just unused.** Registering
  `typeof(IItemView<>) -> typeof(PlanetViewBase<>)` with a `where TObject : PlanetBase` constraint
  looks like it would give implicit fallbacks. It does — right up until something asks for a view for
  a game object that isn't a planet, at which point `GetService` throws `ArgumentException` from
  `MakeGenericType` rather than returning null (see measured behaviour below). There is no
  registration order that avoids this: an unconstrained catch-all registered last wins over the
  constrained one for *everything*, and registered first never gets reached. Beyond that it
  reintroduces reflective construction, and the generator makes explicit registrations free anyway.
- **MS.DI does no variance resolution.** `IItemView<in TItem>` being contravariant means the CLR
  would happily assign `IItemView<PlanetBase>` to `IItemView<PlayerPlanet>`, but the built-in
  container matches service types exactly (plus open-generic closure) and will return nothing. Autofac
  and Simple Injector have variance features; MS.DI doesn't and won't. Don't design expecting it.
- **AOT preservation hatches exist but shouldn't be needed.** `[DynamicDependency]`, ILLink descriptor
  XML via `TrimmerRootDescriptor`, and `TrimmerRootAssembly` are all available if reflective
  construction ever returns. Also worth knowing: Blazor WASM AOT is Mono AOT, which shares generic
  instantiations across reference types and keeps the interpreter as a fallback, so the risk was
  always smaller than a NativeAOT equivalent.

## Measured MS.DI behaviour

Two scratch probes establish these empirically against .NET 10.0.11, with stubbed types
mirroring the design. Re-run them rather than re-deriving anything about MS.DI by argument.

**Resolution** (`DiSeamProbe`, 16 checks). The settled design registers only closed types, so almost
none of MS.DI's generic-resolution behaviour touches it. The one fact that matters, and that the
road-not-taken above rests on: **generic constraints are validated in the `IEnumerable` path but NOT
in single resolution.** `GetServices<IItemView<Goalpost>>()` correctly skips a `where T : PlanetBase`
open registration; `GetService<IItemView<Goalpost>>()` throws `ArgumentException` from
`MakeGenericType`.

**Scopes** (`DiScopeProbe`, 14 checks), which is why the session is a provider rather than a scope:

- **Scopes do not nest.** A scope created from inside another scope — by `IServiceScopeFactory` or by
  `provider.CreateScope()`, identically — is a sibling. Scoped services resolve to different
  instances in each.
- **`IServiceScopeFactory` is the same object at every level.** Resolved from the root and from
  inside a scope, it is reference-identical, so there is nothing for a scope to nest *with*.
- **There is no lifetime containment between scopes.** Disposing the outer scope leaves the inner
  one's scoped instances undisposed. Nesting cannot be faked by disposal order either.
- **A provider disposes what its factories produced, including things it was only forwarding.**
  `AddSingleton(_ => other.GetRequiredService<T>())` in a child provider disposes the *other*
  provider's instance when the child is disposed. `AddSingleton(instance)` with the same instance
  does not. This is the difference between forwarding by factory and forwarding by instance, and it
  is invisible at the call site.
- **A scoped mutable holder, seeded after the scope is created, does work** — and fails silently if
  anything depending on it is resolved before the seeding, because the scope caches that resolution
  with the holder still empty. It is the fallback if provider-per-session ever stops being viable,
  not a first choice.

If the generator ever stops being worth it and implicit fallback views become desirable,
`sp.GetServices<IItemView<T>>().LastOrDefault()` is the sanctioned route — `GetServices` validates
constraints, so the `ArgumentException` disappears. It costs the unenforced discipline "register
fallbacks before specifics" (hence `Last`), which a `MostDerivedOrDefault()` extension would remove.

---

# Open design questions

These are the parts of the seam between Sabric, its Aether implementation, and the game on top that
have **not** been designed. Whatever the code in these areas currently does is not evidence of a
decision.

Each item states the problem and whatever has been established as fact around it. It deliberately
does not propose a solution.

## Levels and what builds a space

The lifecycle is settled; what fills a space is not.

**What a Level type is has not been designed.** It is data rather than code, and it describes the
objects and controllers a space starts with. Beyond that, nothing.

**Who constructs the objects a level describes is a real fork.** Either the level constructs them
directly, or it names what it wants and a factory resolved from the space's scope builds them. The
second is what would let a level be pure data with no code at all — the prerequisite for level files —
and lets a game substitute implementations; it also costs more machinery, and it shapes what a Level
looks like.

**Where the authority to spawn sits is undecided.** `GameSpaceBase.Add`/`Remove` are public, which is
forced rather than chosen: the space is the thing being populated, so once population is external
something has to be callable. Whether *everything* holding a space should be able to spawn into it is
the actual question, and it is unanswered.

## Controllers

**Being designed now — see `Docs/WIP/effects-and-rectro.md`, which supersedes what was here.** The
abstraction is an *effect* rather than a controller, and `Sabric.Engine.Rectro` is being built
alongside it so the design is checked against an engine that isn't Aether. That doc folds back into
this one once it's implemented.

What exists in the meantime is `AetherSpace.AddController`/`RemoveController` taking Aether's own
`Controller`, and game code writing `Planet.Body.ApplyForce` — reaching through Sabric to Aether.

## Rules, events, and what happens after the step

The **event** half is settled: `DispatchEvents` is the post-sweep phase and collision is its first
tenant — see [Collision](#collision--settled-design).

What is still open is whether a **rule** concept is wanted alongside it: a per-space thing with an
update that runs every tick after the sweep, the mirror of a controller on the far side of the step.
Events answer "something happened"; a rule would answer "check this every tick regardless" — a clock
running down, a puck drifting out of bounds. Nothing has established that both are needed, and a rule
may turn out to be an event source rather than a separate category.

## Input

**Input has no abstraction at the right level.** `IPlayerInputSource` — a single `MovementDirection`
vector — currently lives in `Sabric.Engine`, and `KeyboardInputSource` and `PressedKeys` live in
`Sabric.UI.BlazorSVG`. Mouse input doesn't exist at all yet.

What's wanted: keyboard and mouse going through a Sabric abstraction with `Sabric.UI.BlazorSVG`
providing the browser implementation, and the abstraction sitting at a general-purpose level —
`Sabric.UI` perhaps, which currently contains no code at all. Whether `IPlayerInputSource` in its
present shape is the right abstraction, or just the one the port happened to carry over, is part of
the question; so is whether "the player is trying to go this way" and "the pointer is here" are the
same kind of thing.

Related: `Sporbits.Engine.PlayerInputController` translates player intent into a force on the
player's planet. Its placement is flagged as provisional in the source — it is arguably an input
concern rather than an engine one.

## Dependency injection

That there is one container and it is the host's is settled. What isn't:

**What registers what.** What Sabric registers on the host's behalf versus what a game registers, and
what the composition root is expected to say, is undesigned. Everything outside the view seam
constructs its collaborators directly — `SporbitsUI` does `new SporbitsSession()` as a field
initializer, and the keyboard input source is wired up by hand in `OnInitialized`.

**How much there is to register is itself unclear, and the flat decision shrank it.** Nothing in the
engine currently has a constructor dependency, so registering a session buys only the removal of that
`new`. The question gets real once levels arrive and something has to construct the objects a level
describes.

One lifetime fact already recorded, because it constrains the answer: `AddGameObjectViewResolver`
registers a singleton holding the root provider, which is right while the app is one WASM client and
won't be once anything renders per-circuit.

## Session lifecycle: how a space finishes

The three-way split and the scope chain settle where a session begins and ends, and where a level
attaches. What remains:

**How a space reports that it is finished, and what the session does about it.** Sporbits carries an
`IsOver` flag on the space that the session reads and refuses to advance past. That much is only the
old flag moved across the split — finishing is a property of a space (this level is won, lost,
abandoned) and deciding what follows is the session's, but neither the shape of that outcome nor the
session's own state machine has been designed.

Two things established while building the Sporbits version that any general design has to keep:

- A game component that owns a tick loop must be `IDisposable` and stop the loop. A scheduled
  animation frame cannot be cancelled, so the loop stops by declining to schedule the next one.
  Without it, a component torn down mid-game goes on ticking a game nothing is rendering.
- Dismissing a game-over notice has to be gated for the notice's fade-in duration. Crashing while
  holding a key is the normal way to lose, and `keydown` auto-repeats at the OS rate, so an ungated
  notice is dismissed before it finishes appearing. Reacting to the animation's end instead would
  need a custom Blazor `[EventHandler]` registration: there are no built-in animation events.

## Layer boundaries that aren't pulling their weight

- **`Sabric.UI` is empty.** It's meant to be the general rendering abstraction that BlazorSVG
  implements, and nothing has needed to live there yet. The input question above is the first
  candidate.
- **It isn't clear whether there's a meaningful distinction between `Engine` and `Core`**, in either
  repo. `Sabric.Core` currently holds only `VectorExtensions`.
- **It isn't clear what, if anything, belongs in a game's non-Blazor `UI` project.** Nothing in the
  Sporbits port obviously wants to live there.

## Scoped CSS doesn't reach child components

A `.razor.css` file applies only to its own component's markup, so styling can't be written at the
level of the hierarchy it logically belongs to and left to apply downward — which is exactly how it
would be organised given the choice. Wanted: some way to get both halves — defined at the scope where
it applies, inherited by what's below it.

## AOT and trimming

**Hasn't been demonstrated end to end.** Being reflection-free should make it moot, but "should" is
doing work there.
