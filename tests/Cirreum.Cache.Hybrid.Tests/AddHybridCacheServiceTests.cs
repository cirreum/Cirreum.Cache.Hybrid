namespace Cirreum.Cache.Hybrid.Tests;

using System.Linq;
using Cirreum.Cache.Hybrid.Extensions;
using Cirreum.Caching;
using Microsoft.Extensions.Caching.Hybrid;
using Microsoft.Extensions.DependencyInjection;

/// <summary>
/// Registration tests for <see cref="ServiceCollectionExtensions.AddHybridCacheService"/>.
/// These assert at the <see cref="ServiceCollection"/> descriptor level rather than resolving the
/// fully-decorated <see cref="ICacheService"/> (the foundation instruments it and that needs a
/// fuller bootstrap than a bare collection).
/// </summary>
public sealed class AddHybridCacheServiceTests {

	[Fact]
	public void AddHybridCacheService_AutoEnsures_HybridCacheIsRegistered() {
		var services = new ServiceCollection();

		services.AddHybridCacheService();

		services.Any(d => d.ServiceType == typeof(HybridCache))
			.Should().BeTrue("the auto-ensure must register Microsoft's HybridCache");
	}

	[Fact]
	public void AddHybridCacheService_Registers_ICacheService() {
		var services = new ServiceCollection();

		services.AddHybridCacheService();

		services.Any(d => d.ServiceType == typeof(ICacheService))
			.Should().BeTrue("the Cirreum bridge must register ICacheService");
	}

	[Fact]
	public void AddHybridCacheService_AfterExplicitAddHybridCache_LeavesExactlyOneHybridCacheDescriptor() {
		var services = new ServiceCollection();

		// Consumer registers HybridCache first; the auto-ensure must be idempotent (TryAdd-based).
		services.AddHybridCache();
		services.AddHybridCacheService();

		services.Count(d => d.ServiceType == typeof(HybridCache))
			.Should().Be(1, "the idempotent auto-ensure must not duplicate the HybridCache registration");
	}

	[Fact]
	public void AddHybridCacheService_ReturnsSameServiceCollection_ForChaining() {
		var services = new ServiceCollection();

		var returned = services.AddHybridCacheService();

		returned.Should().BeSameAs(services);
	}

	[Fact]
	public void AddHybridCacheService_NullServices_Throws_ArgumentNullException() {
		var act = () => ((IServiceCollection)null!).AddHybridCacheService();

		act.Should().Throw<ArgumentNullException>();
	}
}
