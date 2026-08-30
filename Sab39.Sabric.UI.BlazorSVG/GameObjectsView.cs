using Sab39.Sabric.Engine;

namespace Sab39.Sabric.UI.BlazorSVG;

/// <summary>
/// Renders every object in a space.
/// </summary>
/// <remarks>
/// Closed here rather than left as <c>&lt;ItemListViewBase TItem="GameObjectBase" ... /&gt;</c> at
/// the call site, because inference would take TItem from whatever the space's list is typed as -
/// AetherObjectBase, say - and then ask for an <c>IItemView</c> of that. MS.DI does no variance
/// resolution, so the resolver registered against GameObjectBase would not be found, at runtime,
/// with nothing at the call site looking wrong. Covariance on the list makes the parameter accept
/// the narrower list anyway, so nothing is lost by closing it.
/// </remarks>
public sealed class GameObjectsView : ItemListViewBase<GameObjectBase>
{
    protected override object GetKey(GameObjectBase item) => item.GameObjectId;
}
