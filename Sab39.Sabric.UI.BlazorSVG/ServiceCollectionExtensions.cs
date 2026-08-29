using Sab39.Sabric.Engine;

using Microsoft.Extensions.DependencyInjection;

namespace Sab39.Sabric.UI.BlazorSVG;

public static class ServiceCollectionExtensions
{
    extension(IServiceCollection services)
    {
        /// <summary>
        /// Registers the resolver that turns a game object into the markup for its view.
        /// </summary>
        /// <remarks>
        /// Deliberately a line the host writes for itself rather than something the first
        /// <see cref="AddGameObjectView{TObject, TComponent}"/> quietly brings along. The lifetime
        /// stays here because it's this layer's business: a singleton holding the root provider is
        /// right while the game is one WASM client, and won't be once anything renders per-circuit.
        /// </remarks>
        public IServiceCollection AddGameObjectViewResolver()
            => services.AddSingleton<GameObjectViewResolver>();

        /// <summary>
        /// Registers <typeparamref name="TComponent"/> as the view for
        /// <typeparamref name="TObject"/>.
        /// </summary>
        /// <remarks>
        /// The <c>TComponent : GameObjectViewBase&lt;TObject&gt;</c> constraint puts the type check
        /// here, at the one call site that legitimately knows about both halves. Pairing an object
        /// with a view for something else is a compile error at this line, and nothing downstream
        /// ever has to test for it.
        /// </remarks>
        public IServiceCollection AddGameObjectView<TObject, TComponent>()
            where TObject : GameObjectBase
            where TComponent : GameObjectViewBase<TObject>
            => services.AddSingleton<IGameObjectView<TObject>, ComponentView<TComponent, TObject>>();
    }
}
