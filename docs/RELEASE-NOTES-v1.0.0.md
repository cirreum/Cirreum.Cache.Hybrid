# Cirreum.Cache.Hybrid 1.0.0 — the HybridCache provider

The `HybridCache`-backed implementation of Cirreum's `ICacheService`: L1 (local) + L2 (distributed)
layers, stampede protection, and tag-based invalidation. Successor to the published
`Cirreum.QueryCache.Hybrid` package, re-homed to a new id alongside the Cirreum 1.0 foundation reset
and the code-first caching model.

Migrating from `Cirreum.QueryCache.Hybrid`? See [`MIGRATION-v1.md`](MIGRATION-v1.md).

## Why this release exists

The caching foundation went code-first in `Cirreum.Contracts`/`Cirreum.Domain 1.1.1`: a provider is
chosen by the registration call, not an appsettings switch. This package is the `HybridCache` provider
under that model, and it moves to the `Cirreum.Cache.*` id family that replaces `Cirreum.QueryCache.*`.

## What's new

### `AddHybridCacheService()`

```csharp
services.AddCirreumCaching();
services.AddHybridCacheService();   // selects this provider AND ensures Microsoft's AddHybridCache()
```

Registers `HybridCacheService` as the active `ICacheService` via the foundation's code-first
`AddCacheService(factory)` helper — the registration call itself selects the provider (no
`CacheProvider` enum, no `Cirreum:Cache:Provider` appsettings switch). It **auto-ensures Microsoft's
`AddHybridCache()`**, so a single call wires up the underlying `HybridCache`; the ensure is idempotent
(`TryAdd`-based), so a prior `AddHybridCache(...)` with custom options is preserved.

## Behavior

- Leverages `HybridCache` for L1 + L2 layers, stampede protection, and **tag-based invalidation**
  (`RemoveByTagAsync` / `RemoveByTagsAsync`) — the capability the distributed provider can't offer.
- Failure results are re-written with the shorter `FailureExpiration` when configured.
- Cached `Result` / `Result<T>` values round-trip correctly via the `Cirreum.Result` System.Text.Json
  converter (ADR-0024).

## Compatibility

- **Successor to a published package** — a package-id + code-first migration, not a new capability.
  See [`MIGRATION-v1.md`](MIGRATION-v1.md).
- **Depends on `Cirreum.Domain 1.1.1`** (code-first cache abstractions) and Microsoft's
  `Microsoft.Extensions.Caching.Hybrid`.

## See also

- `Cirreum.Cache.Distributed` — the `IDistributedCache`-backed provider (single L2 layer)
