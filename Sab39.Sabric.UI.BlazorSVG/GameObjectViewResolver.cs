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
/// the compiler, so there is no <c>MakeGenericType</c> anywhere on the path - and no cast either.
/// </remarks>
public sealed class GameObjectViewResolver(IServiceProvider services) : IItemView<GameObjectBase>
{
    private readonly IServiceProvider services = services;

    public RenderFragment Render(GameObjectBase item, object key) => item.Accept(new Visitor(this.services, key));

    /// <remarks>
    /// A visitor per call, rather than the resolver being its own, because the key has to reach
    /// Visit and Accept has nowhere to put it. The alternative is holding it in a field across a
    /// dispatch that is only single-threaded by accident of where it runs today. This costs one
    /// small allocation per item per *list* render, which happens when something spawns or
    /// despawns - not per frame.
    /// </remarks>
    private sealed class Visitor(IServiceProvider services, object key) : IGameObjectVisitor<RenderFragment>
    {
        private readonly IServiceProvider services = services;
        private readonly object key = key;

        // Explicit, because Render is the way in - Visit is only reachable via Accept, which is
        // exactly the point of it being here.
        RenderFragment IGameObjectVisitor<RenderFragment>.Visit<T>(T obj)
            => this.services.GetRequiredService<IItemView<T>>().Render(obj, this.key);
    }
}
