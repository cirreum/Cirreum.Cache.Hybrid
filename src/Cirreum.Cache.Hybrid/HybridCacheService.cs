namespace Cirreum.Cache.Hybrid;

using Cirreum.Caching;
using Microsoft.Extensions.Caching.Hybrid;
using System.Collections.Generic;

sealed class HybridCacheService(
	HybridCache hybridCache
) : ICacheService {

	public async ValueTask<TResponse> GetOrCreateAsync<TResponse>(
		string cacheKey,
		Func<CancellationToken, ValueTask<TResponse>> factory,
		CacheExpirationPolicy settings,
		string[]? tags = null,
		CancellationToken cancellationToken = default) {

		// factoryExecuted is a per-invocation local — safe to capture in the async lambda.
		// HybridCache's stampede protection means only one concurrent caller runs the factory,
		// but each call to this method gets its own bool, so there is no shared-state issue.
		var factoryExecuted = false;

		var value = await hybridCache.GetOrCreateAsync(
			cacheKey,
			async ct => {
				factoryExecuted = true;
				return await factory(ct);
			},
			options: CreateOptions(settings),
			tags: tags,
			cancellationToken: cancellationToken);

		// When the factory produces a failure result we immediately overwrite the entry with a
		// shorter FailureExpiration. This causes two writes to the cache (GetOrCreateAsync stores
		// the value first with normal expiration, then SetAsync overwrites it) — an inherent
		// limitation of the HybridCache API since expiration cannot be determined before the
		// factory runs. The brief window between the two writes is acceptable.
		if (factoryExecuted &&
			value is IResult { IsSuccess: false } &&
			settings.FailureExpiration.HasValue) {
			await hybridCache.SetAsync(
				cacheKey,
				value,
				CreateOptions(settings, useFailureExpiration: true),
				tags: tags,
				cancellationToken: cancellationToken);
		}

		return value;
	}

	public ValueTask RemoveAsync(string cacheKey, CancellationToken cancellationToken) {
		return hybridCache.RemoveAsync(cacheKey, cancellationToken);
	}

	public ValueTask RemoveByTagAsync(string tag, CancellationToken cancellationToken = default) {
		return hybridCache.RemoveByTagAsync(tag, cancellationToken);
	}

	public ValueTask RemoveByTagsAsync(IEnumerable<string> tags, CancellationToken cancellationToken = default) {
		return hybridCache.RemoveByTagAsync(tags, cancellationToken);
	}

	private static HybridCacheEntryOptions CreateOptions(CacheExpirationPolicy settings, bool useFailureExpiration = false) {
		var expiration = useFailureExpiration && settings.FailureExpiration.HasValue
			? settings.FailureExpiration.Value
			: settings.Expiration;

		// FailureExpiration is intentionally applied to both L1 (local) and L2 (distributed) cache.
		// If a separate failure TTL for L1 is ever needed, introduce a FailureLocalExpiration setting.
		var localExpiration = useFailureExpiration && settings.FailureExpiration.HasValue
			? settings.FailureExpiration.Value
			: settings.LocalExpiration;

		return new HybridCacheEntryOptions {
			Expiration = expiration,
			LocalCacheExpiration = localExpiration
		};
	}

}