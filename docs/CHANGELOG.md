# Changelog

All notable changes to **Cirreum.Cache.Hybrid** are documented in this file.

The format is based on [Keep a Changelog](https://keepachangelog.com/en/1.1.0/),
and this project adheres to [Semantic Versioning](https://semver.org/spec/v2.0.0.html).

For detailed migration steps on major version bumps, see the per-version migration
guides linked at the bottom of each entry.

---

## [Unreleased]

### Updated

- Updated NuGet packages.

## [1.0.2] - 2026-07-04

### Updated

- Updated NuGet packages.

## [1.0.1] - 2026-07-04

### Updated

- Updated NuGet packages.

## [1.0.0] - 2026-07-03

### Added

- Initial release of **Cirreum.Cache.Hybrid**, the `HybridCache`-backed implementation of
  `ICacheService`. Supersedes the legacy `Cirreum.QueryCache.Hybrid` package (now archived);
  the lineage moved to a new package id alongside the Cirreum 1.0 foundation reset and the
  code-first caching model.
- `AddHybridCacheService()` registers `HybridCacheService` as the active `ICacheService` via
  the foundation's code-first `AddCacheService(factory)` helper — the registration call itself
  selects the provider (no `CacheProvider` enum, no `Cirreum:Cache:Provider` appsettings switch).
- `AddHybridCacheService()` auto-ensures Microsoft's `AddHybridCache()`, so a single call
  wires up the underlying `HybridCache` out of the box. The ensure is idempotent — a prior
  `AddHybridCache(...)` with custom options is preserved (it is `TryAdd`-based).
- Targets `Cirreum.Domain` 1.x and consumes the code-first cache abstractions
  (`ICacheService` / `CacheExpirationPolicy`).
- Leverages `HybridCache` for L1 (local) + L2 (distributed) layers, stampede protection,
  and tag-based invalidation (`RemoveByTagAsync` / `RemoveByTagsAsync`).
- Failure results are re-written with the shorter `FailureExpiration` when configured.
- Cached `Result` / `Result<T>` values round-trip correctly via the `Cirreum.Result`
  System.Text.Json converter (see ADR-0024).
