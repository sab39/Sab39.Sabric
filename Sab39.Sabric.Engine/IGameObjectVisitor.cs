namespace Sab39.Sabric.Engine;

/// <summary>
/// Recovers a game object's own static type from a reference typed as <see cref="GameObjectBase"/>.
/// </summary>
/// <remarks>
/// The engine learns nothing about what a visitor is for, because the result type belongs to the
/// visitor rather than to this interface. The UI layer's implementation returns a render fragment;
/// nothing here knows that.
/// </remarks>
public interface IGameObjectVisitor<out TResult>
{
    TResult Visit<T>(T obj) where T : GameObjectBase;
}
