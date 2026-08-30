namespace Sab39.Sabric.UI;

/// <summary>
/// Something that moves a camera, given a chance once per tick.
/// </summary>
/// <remarks>
/// The push half of the design: a camera is state, and a behaviour is what decides what that state
/// should be this tick. Deadzone follow, lookahead along velocity, smoothing, shake and
/// zoom-to-fit are all this shape.
///
/// Driven from <see cref="Sab39.Sabric.Engine.GameSessionBase.Ticked"/> rather than by anything in
/// the engine. A per-tick thing that runs on the far side of the step is the same shape as the open
/// <em>rules</em> question in Sabric's Docs/architecture.md, and a camera behaviour is deliberately
/// not being made the thing that answers it.
/// </remarks>
public abstract class CameraBehaviourBase
{
    /// <param name="delta">Milliseconds since the previous tick, matching a space's advance.</param>
    public abstract void Update(long delta, Camera camera);
}
