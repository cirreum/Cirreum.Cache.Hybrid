namespace Cirreum.Cache.Hybrid.Tests;

using Cirreum;
using Cirreum.Caching;
using Microsoft.Extensions.Caching.Hybrid;
using Microsoft.Extensions.DependencyInjection;

/// <summary>
/// Behaviour tests for <see cref="HybridCacheService"/>.
/// Each test constructs the internal service DIRECTLY against a REAL, isolated
/// <see cref="HybridCache"/> (its own <see cref="ServiceProvider"/>) so cache state never leaks
/// between tests. Keys are unique per test for the same reason.
/// </summary>
public sealed class HybridCacheServiceTests {

	private static (HybridCacheService service, ServiceProvider provider) CreateService() {
		var services = new ServiceCollection();
		services.AddHybridCache();
		var provider = services.BuildServiceProvider();

		var hybridCache = provider.GetRequiredService<HybridCache>();
		return (new HybridCacheService(hybridCache), provider);
	}

	private static CacheExpirationPolicy DefaultPolicy =>
		new(Expiration: TimeSpan.FromMinutes(5), LocalExpiration: TimeSpan.FromMinutes(5));

	[Fact]
	public async Task GetOrCreateAsync_OnMiss_InvokesFactoryOnce_AndReturnsFactoryValue() {
		var (service, provider) = CreateService();
		await using var _ = provider;

		var callCount = 0;

		var result = await service.GetOrCreateAsync(
			"miss-key",
			ct => {
				callCount++;
				return new ValueTask<string>("produced");
			},
			DefaultPolicy);

		result.Should().Be("produced");
		callCount.Should().Be(1);
	}

	[Fact]
	public async Task GetOrCreateAsync_SameKeyTwice_InvokesFactoryExactlyOnce() {
		var (service, provider) = CreateService();
		await using var _ = provider;

		var callCount = 0;

		ValueTask<string> Factory(CancellationToken ct) {
			callCount++;
			return new ValueTask<string>($"value-{callCount}");
		}

		var first = await service.GetOrCreateAsync("dup-key", Factory, DefaultPolicy);
		var second = await service.GetOrCreateAsync("dup-key", Factory, DefaultPolicy);

		callCount.Should().Be(1, "the second call must be a cache hit");
		first.Should().Be("value-1");
		second.Should().Be("value-1", "the cached value must be returned, not a freshly produced one");
	}

	[Fact]
	public async Task RemoveAsync_EvictsEntry_SoNextGetOrCreateReinvokesFactory() {
		var (service, provider) = CreateService();
		await using var _ = provider;

		var callCount = 0;

		ValueTask<string> Factory(CancellationToken ct) {
			callCount++;
			return new ValueTask<string>($"value-{callCount}");
		}

		await service.GetOrCreateAsync("remove-key", Factory, DefaultPolicy);
		await service.RemoveAsync("remove-key", CancellationToken.None);
		var afterRemove = await service.GetOrCreateAsync("remove-key", Factory, DefaultPolicy);

		callCount.Should().Be(2, "removal must force the factory to run again");
		afterRemove.Should().Be("value-2");
	}

	[Fact]
	public async Task RemoveByTagAsync_EvictsTaggedEntry_SoNextGetOrCreateReinvokesFactory() {
		var (service, provider) = CreateService();
		await using var _ = provider;

		var callCount = 0;
		string[] tags = ["tag-a"];

		ValueTask<string> Factory(CancellationToken ct) {
			callCount++;
			return new ValueTask<string>($"value-{callCount}");
		}

		await service.GetOrCreateAsync("tagged-key", Factory, DefaultPolicy, tags);
		await service.RemoveByTagAsync("tag-a", CancellationToken.None);
		var afterEvict = await service.GetOrCreateAsync("tagged-key", Factory, DefaultPolicy, tags);

		callCount.Should().Be(2, "tag eviction must force the factory to run again");
		afterEvict.Should().Be("value-2");
	}

	[Fact]
	public async Task RemoveByTagsAsync_EvictsEntriesUnderThoseTags_SoNextGetOrCreateReinvokesFactory() {
		var (service, provider) = CreateService();
		await using var _ = provider;

		var keyACalls = 0;
		var keyBCalls = 0;

		ValueTask<string> FactoryA(CancellationToken ct) {
			keyACalls++;
			return new ValueTask<string>($"a-{keyACalls}");
		}

		ValueTask<string> FactoryB(CancellationToken ct) {
			keyBCalls++;
			return new ValueTask<string>($"b-{keyBCalls}");
		}

		await service.GetOrCreateAsync("key-a", FactoryA, DefaultPolicy, ["tag-a"]);
		await service.GetOrCreateAsync("key-b", FactoryB, DefaultPolicy, ["tag-b"]);

		await service.RemoveByTagsAsync(["tag-a", "tag-b"], CancellationToken.None);

		var a = await service.GetOrCreateAsync("key-a", FactoryA, DefaultPolicy, ["tag-a"]);
		var b = await service.GetOrCreateAsync("key-b", FactoryB, DefaultPolicy, ["tag-b"]);

		keyACalls.Should().Be(2, "tag-a entry must have been evicted");
		keyBCalls.Should().Be(2, "tag-b entry must have been evicted");
		a.Should().Be("a-2");
		b.Should().Be("b-2");
	}

	[Fact]
	public async Task GetOrCreateAsync_DistinctKeys_CacheIndependently_FactoryRunsOncePerKey() {
		var (service, provider) = CreateService();
		await using var _ = provider;

		var callCount = 0;

		ValueTask<string> Factory(CancellationToken ct) {
			callCount++;
			return new ValueTask<string>($"value-{callCount}");
		}

		var first = await service.GetOrCreateAsync("distinct-1", Factory, DefaultPolicy);
		var second = await service.GetOrCreateAsync("distinct-2", Factory, DefaultPolicy);
		// Re-read each key; these must be hits.
		var firstAgain = await service.GetOrCreateAsync("distinct-1", Factory, DefaultPolicy);
		var secondAgain = await service.GetOrCreateAsync("distinct-2", Factory, DefaultPolicy);

		callCount.Should().Be(2, "the factory must run exactly once per distinct key");
		first.Should().Be("value-1");
		second.Should().Be("value-2");
		firstAgain.Should().Be("value-1");
		secondAgain.Should().Be("value-2");
	}

	[Fact]
	public async Task GetOrCreateAsync_FailureResult_WithFailureExpiration_DoesNotThrow_AndIsCached() {
		var (service, provider) = CreateService();
		await using var _ = provider;

		var callCount = 0;
		var policy = new CacheExpirationPolicy(
			Expiration: TimeSpan.FromMinutes(5),
			LocalExpiration: TimeSpan.FromMinutes(5),
			FailureExpiration: TimeSpan.FromSeconds(1));

		ValueTask<Result<string>> Factory(CancellationToken ct) {
			callCount++;
			return new ValueTask<Result<string>>(Result<string>.Fail(new InvalidOperationException("boom")));
		}

		// First call: factory produces a failure; service re-writes it with the failure expiration.
		var first = await service.GetOrCreateAsync("failure-key", Factory, policy);
		// Second call within the (still-valid) TTL: must be a cache hit, factory not re-run.
		var second = await service.GetOrCreateAsync("failure-key", Factory, policy);

		callCount.Should().Be(1, "the cached failure must be returned without re-running the factory");
		first.IsFailure.Should().BeTrue();
		((IResult)first).IsSuccess.Should().BeFalse();
		second.IsFailure.Should().BeTrue();
	}

	[Fact]
	public async Task GetOrCreateAsync_WithNullTags_CachesNormally() {
		var (service, provider) = CreateService();
		await using var _ = provider;

		var callCount = 0;

		ValueTask<string> Factory(CancellationToken ct) {
			callCount++;
			return new ValueTask<string>($"value-{callCount}");
		}

		// tags omitted => null tags path.
		var first = await service.GetOrCreateAsync("null-tags-key", Factory, DefaultPolicy, tags: null);
		var second = await service.GetOrCreateAsync("null-tags-key", Factory, DefaultPolicy, tags: null);

		callCount.Should().Be(1, "the null-tags path must still cache and hit on the second call");
		first.Should().Be("value-1");
		second.Should().Be("value-1");
	}

	[Fact]
	public async Task GetOrCreateAsync_SuccessResult_WithFailureExpirationSet_DoesNotRewrite_AndCaches() {
		// A success IResult must NOT trigger the failure-expiration overwrite branch; it should
		// simply cache like any value and hit on the second call.
		var (service, provider) = CreateService();
		await using var _ = provider;

		var callCount = 0;
		var policy = new CacheExpirationPolicy(
			Expiration: TimeSpan.FromMinutes(5),
			LocalExpiration: TimeSpan.FromMinutes(5),
			FailureExpiration: TimeSpan.FromSeconds(1));

		ValueTask<Result<string>> Factory(CancellationToken ct) {
			callCount++;
			return new ValueTask<Result<string>>(Result<string>.Success($"ok-{callCount}"));
		}

		var first = await service.GetOrCreateAsync("success-key", Factory, policy);
		var second = await service.GetOrCreateAsync("success-key", Factory, policy);

		callCount.Should().Be(1);
		first.IsSuccess.Should().BeTrue();
		first.Value.Should().Be("ok-1");
		second.IsSuccess.Should().BeTrue();
		second.Value.Should().Be("ok-1");
	}

	[Fact]
	public async Task GetOrCreateAsync_NonResultValueType_WithFailureExpirationSet_CachesNormally() {
		// A plain (non-IResult) value with FailureExpiration set must skip the failure branch entirely.
		var (service, provider) = CreateService();
		await using var _ = provider;

		var callCount = 0;
		var policy = new CacheExpirationPolicy(
			Expiration: TimeSpan.FromMinutes(5),
			FailureExpiration: TimeSpan.FromSeconds(1));

		ValueTask<int> Factory(CancellationToken ct) {
			callCount++;
			return new ValueTask<int>(callCount);
		}

		var first = await service.GetOrCreateAsync("plain-key", Factory, policy);
		var second = await service.GetOrCreateAsync("plain-key", Factory, policy);

		callCount.Should().Be(1);
		first.Should().Be(1);
		second.Should().Be(1);
	}
}
