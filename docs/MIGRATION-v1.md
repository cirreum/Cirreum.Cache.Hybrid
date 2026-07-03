# Cirreum.Cache.Hybrid v1.0.0 — Migration Guide

> **From:** `Cirreum.QueryCache.Hybrid` (now archived) &nbsp;•&nbsp; **To:** `Cirreum.Cache.Hybrid 1.0.0`

## Why v1

`Cirreum.Cache.Hybrid` is the successor to the published `Cirreum.QueryCache.Hybrid` package. The
package id changed alongside the Cirreum 1.0 foundation reset and the **code-first caching model**:
a cache provider is now selected by the registration call rather than a `CacheProvider` enum /
`Cirreum:Cache:Provider` appsettings switch. The `HybridCache`-backed `ICacheService` implementation
itself is unchanged in behavior.

---

## Breaking Changes — Find/Replace Table

| `Cirreum.QueryCache.Hybrid` | `Cirreum.Cache.Hybrid 1.0.0` | Notes |
|---|---|---|
| `<PackageReference Include="Cirreum.QueryCache.Hybrid" .../>` | `<PackageReference Include="Cirreum.Cache.Hybrid" Version="1.0.0" />` | New package id |
| `AddHybridQueryCaching()` | `AddHybridCacheService()` | Registration verb; now installs the provider via the foundation's `AddCacheService(factory)` seam and auto-ensures Microsoft's `AddHybridCache()` |
| `HybridCacheableQueryService` | `HybridCacheService` | The `ICacheService` implementation |
| `namespace Cirreum.QueryCache.Hybrid` | `namespace Cirreum.Cache.Hybrid` | + `.Extensions` for the registration method |

### From the code-first caching model (`Cirreum.Contracts`/`Cirreum.Domain 1.1.1`)

| Before | After | Notes |
|---|---|---|
| `Cirreum:Cache:Provider` appsettings + `CacheProvider` enum | (removed) | The `Add…CacheService()` call *is* the provider choice |
| `CacheExpirationSettings` | `CacheExpirationPolicy` | Runtime per-operation expiration spec |
| `namespace Cirreum.Caching` (for `CacheSettings` / `CacheExpirationOverride`) | `namespace Cirreum.Caching.Configuration` | App-author configuration types |

---

## Migration Walkthrough

1. Swap the package reference to `Cirreum.Cache.Hybrid 1.0.0`.
2. Replace `AddHybridQueryCaching()` with `AddHybridCacheService()` (after `AddCirreumCaching()` /
   `AddDomainServices()`). The call auto-ensures Microsoft's `AddHybridCache()` — a single call wires
   the underlying `HybridCache`; a prior `AddHybridCache(...)` with custom options is preserved
   (`TryAdd`-based).
3. Delete any `Cirreum:Cache:Provider` appsettings entry.
4. Apply the type/namespace renames from the tables above.

---

## What Didn't Change

- The `HybridCache`-backed behavior: L1 (local) + L2 (distributed) layers, stampede protection, and
  tag-based invalidation (`RemoveByTagAsync` / `RemoveByTagsAsync`).
- Failure results are re-written with the shorter `FailureExpiration` when configured, and
  `Result` / `Result<T>` round-trip correctly (via the `Cirreum.Result` STJ converter, ADR-0024).

---

## Downstream Package Impact

Consumers of the old `Cirreum.QueryCache.Hybrid` package should move to `Cirreum.Cache.Hybrid`.
The old package is deprecated on NuGet with a successor pointer.
