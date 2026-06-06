namespace Cirreum.Cache.Hybrid.Extensions;

using Cirreum;
using Cirreum.Caching;
using Microsoft.Extensions.Caching.Hybrid;
using Microsoft.Extensions.DependencyInjection;

/// <summary>
/// Extension methods for registering the hybrid cache service.
/// </summary>
public static class ServiceCollectionExtensions {

	/// <summary>
	/// Registers <see cref="HybridCacheService"/> as the active <see cref="ICacheService"/> (replacing
	/// the no-op default), bridging Cirreum caching to Microsoft's <see cref="HybridCache"/>.
	/// </summary>
	/// <remarks>
	/// Auto-ensures Microsoft's <c>AddHybridCache()</c> is registered, so this single call wires up the
	/// underlying <see cref="HybridCache"/> out of the box — no separate registration step. The ensure is
	/// idempotent: if you have already called <c>AddHybridCache(...)</c> with custom options, that
	/// registration wins (it is <c>TryAdd</c>-based) and this method only layers the Cirreum bridge on top.
	/// </remarks>
	/// <param name="services">The service collection.</param>
	/// <returns>The service collection for chaining.</returns>
	public static IServiceCollection AddHybridCacheService(this IServiceCollection services) {
		ArgumentNullException.ThrowIfNull(services);

		// Ensure the underlying HybridCache exists. AddHybridCache is TryAdd-based, so a consumer-supplied
		// configuration is preserved and this is a no-op when one is already present.
		services.AddHybridCache();

		return services.AddCacheService(static sp => new HybridCacheService(
			sp.GetRequiredService<HybridCache>()));
	}
}
