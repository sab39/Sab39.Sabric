using Sab39.Sabric.Engine;

using Microsoft.AspNetCore.Components;
using Microsoft.Extensions.DependencyInjection;

namespace Sab39.Sabric.UI.BlazorSVG;

/// <summary>
/// Turns any game object into the markup for whichever view was registered for it.
/// </summary>
/// <remarks>
/// This is the seam itself: <see cref="GameObjectBase.Accept"/> supplies the object's static type,
/// and that type is what names the service to ask for. Every type argument involved is closed by
/// the compiler, so there is no <c>MakeGenericType</c> anywhere on the path.
/// </remarks>
public sealed class GameObjectViewResolver(IServiceProvider services) : IGameObjectVisitor<RenderFragment>
{
    private readonly IServiceProvider services = services;

    public RenderFragment Render(GameObjectBase obj) => obj.Accept(this);

    // Explicit, because Render is the way in - Visit is only reachable via Accept, which is
    // exactly the point of it being here.
    RenderFragment IGameObjectVisitor<RenderFragment>.Visit<T>(T obj)
        => this.services.GetRequiredService<IGameObjectView<T>>().Render(obj);
}
