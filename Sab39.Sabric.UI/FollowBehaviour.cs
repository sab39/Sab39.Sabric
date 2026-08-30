using Sab39.Sabric.Engine;

namespace Sab39.Sabric.UI;

/// <summary>
/// Keeps the camera centred on one game object, exactly and with no lag.
/// </summary>
/// <remarks>
/// The plainest thing that works, and it takes no <c>delta</c> because a hard lock has nothing to
/// integrate. Anything softer - a deadzone the target moves freely inside, lookahead along its
/// velocity, smoothing towards it - is a different behaviour rather than an option on this one.
/// </remarks>
public sealed class FollowBehaviour(GameObjectBase target) : CameraBehaviourBase
{
    public GameObjectBase Target { get; } = target;

    public override void Update(long delta, Camera camera) => camera.Position = Target.Position;
}
