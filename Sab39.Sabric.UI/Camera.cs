using System.Numerics;

using Sab39.Core.Components;
using Sab39.Sabric.Engine;

namespace Sab39.Sabric.UI;

/// <summary>
/// Where a view of a space is looking: a position, an angle and a zoom, all in world units.
/// </summary>
/// <remarks>
/// Called a camera rather than a viewport because SVG already uses <em>viewport</em> for the
/// <c>svg</c> element's own rect - which is <see cref="Extent"/>, the other half of the pair. The
/// two are deliberately separate: the extent says how big a window is and never moves, and the
/// camera says where that window is pointed.
///
/// A camera belongs to whatever renders a space, not to the space - a space is engine-side, and
/// split screen is two cameras over one. Nothing here is Blazor- or SVG-specific; turning a camera
/// into markup is the rendering layer's job.
///
/// Being an <see cref="IChangeNotifier"/> is what keeps the root of a render tree still: a view of
/// a camera subscribes to it exactly as a view of a game object subscribes to its object, so a
/// moving camera re-renders one small component per frame and nothing above it.
/// </remarks>
public sealed class Camera : IPropertyChange, IChangeNotifier, IDisposable
{
    private readonly GameSessionBase session;

    /// <remarks>
    /// The camera takes the session rather than being driven from outside, because
    /// <see cref="GameSessionBase.Ticked"/> is the one moment that is reliably after the physics
    /// sync sweep. Driven from anywhere earlier, the camera would be a frame behind its target -
    /// which shows up as everything it follows jittering against a parallax background.
    /// </remarks>
    public Camera(GameSessionBase session)
    {
        this.session = session;
        session.Ticked += HandleTicked;
    }

    public Vector2 Position { get; set => this.SetProperty(ref field, value); }

    /// <summary>
    /// Which way up the camera is, in radians. Rotating it rotates the whole world under it.
    /// </summary>
    /// <remarks>
    /// In for its own sake: it is nearly free in an SVG transform, there is no up or down in space,
    /// and disorienting the player by rolling the camera is a mechanic worth having available.
    /// </remarks>
    public float Rotation { get; set => this.SetProperty(ref field, value); }

    public float Zoom { get; set => this.SetProperty(ref field, value); } = 1;

    /// <summary>
    /// How much world fits on screen at <see cref="Zoom"/> 1 - the size of the static window the
    /// camera looks through, and the source of truth for whatever writes an SVG <c>viewBox</c>.
    /// </summary>
    public Vector2 Extent { get; init; }

    /// <summary>
    /// What moves the camera, run once per tick. Null leaves the camera wherever it was put.
    /// </summary>
    /// <remarks>
    /// One behaviour rather than a list. Whether a camera should be able to stack them - a shake
    /// over a follow, a zoom over both - is a real question about how they would compose, and one
    /// slot declines to answer it rather than answering it by accident.
    /// </remarks>
    public CameraBehaviourBase? Behaviour { get; set; }

    /// <summary>
    /// Gives <see cref="Behaviour"/> its chance to move the camera. Called once per tick.
    /// </summary>
    public void Update(long delta) => Behaviour?.Update(delta, this);

    private void HandleTicked(object? sender, EventArgs args) => Update(this.session.Delta);

    /// <summary>
    /// Where a point in the world lands in the view, in the same units as <see cref="Extent"/> and
    /// measured from the centre of the view.
    /// </summary>
    public Vector2 ToScreen(Vector2 world) => Rotate((world - Position) * Zoom, -Rotation);

    /// <summary>
    /// The inverse of <see cref="ToScreen"/>: what a point in the view is pointing at in the world.
    /// </summary>
    /// <remarks>
    /// This is what mouse input needs, and the reason a pointer position is not the same kind of
    /// thing as a movement direction: a direction is already meaningful, and a pointer is not until
    /// a camera interprets it.
    /// </remarks>
    public Vector2 ToWorld(Vector2 screen) => Position + (Rotate(screen, Rotation) / Zoom);

    /// <remarks>
    /// The screen frame has y pointing down, matching SVG, so a positive angle here turns the same
    /// way SVG's own <c>rotate()</c> does. What matters is that both directions use this one
    /// matrix, so that they stay inverses whatever the convention.
    /// </remarks>
    private static Vector2 Rotate(Vector2 v, float radians)
    {
        var (sin, cos) = float.SinCos(radians);
        return new((v.X * cos) - (v.Y * sin), (v.X * sin) + (v.Y * cos));
    }

    public event EventHandler? Changed;
    void IPropertyChange.OnPropertyChanged(string? propertyName) => Changed?.Invoke(this, EventArgs.Empty);

    /// <remarks>
    /// Only the tick subscription, which is the one thing here that outlives a dropped reference.
    /// Whatever owns the camera owns disposing it, which in Blazor means the component that holds it.
    /// </remarks>
    public void Dispose() => this.session.Ticked -= HandleTicked;
}
