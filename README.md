# Cirreum.Cache.Hybrid

[![NuGet Version](https://img.shields.io/nuget/v/Cirreum.Cache.Hybrid.svg?style=flat-square&labelColor=1F1F1F&color=003D8F)](https://www.nuget.org/packages/Cirreum.Cache.Hybrid/)
[![NuGet Downloads](https://img.shields.io/nuget/dt/Cirreum.Cache.Hybrid.svg?style=flat-square&labelColor=1F1F1F&color=003D8F)](https://www.nuget.org/packages/Cirreum.Cache.Hybrid/)
[![GitHub Release](https://img.shields.io/github/v/release/cirreum/Cirreum.Cache.Hybrid?style=flat-square&labelColor=1F1F1F&color=FF3B2E)](https://github.com/cirreum/Cirreum.Cache.Hybrid/releases)
[![License](https://img.shields.io/github/license/cirreum/Cirreum.Cache.Hybrid?style=flat-square&labelColor=1F1F1F&color=F2F2F2)](https://github.com/cirreum/Cirreum.Cache.Hybrid/blob/main/LICENSE)
[![.NET](https://img.shields.io/badge/.NET-10.0-003D8F?style=flat-square&labelColor=1F1F1F)](https://dotnet.microsoft.com/)

**Hybrid caching implementation for Cirreum's cache service**

> Supersedes the legacy `Cirreum.QueryCache.Hybrid` package. The lineage moved to a new
> package id alongside the Cirreum 1.0 foundation reset and the code-first caching model.

## Overview

**Cirreum.Cache.Hybrid** provides a hybrid caching implementation that bridges Microsoft's `HybridCache` service with Cirreum's caching framework.

This library implements the `ICacheService` interface using Microsoft's `HybridCache` infrastructure, enabling automatic caching of query results with support for both local and distributed cache layers, tag-based invalidation, and failure-specific expiration policies.

## Features

- **Hybrid Caching**: Leverages Microsoft's `HybridCache` for optimal performance across local and distributed cache layers
- **Code-First Registration**: The `AddHybridCacheService()` call *is* the provider choice — no `CacheProvider` enum or appsettings switch
- **One-Call Setup**: `AddHybridCacheService()` auto-ensures Microsoft's `AddHybridCache()`, so the underlying cache is wired up out of the box
- **Tag-Based Invalidation**: Cache invalidation using tags for related data
- **Failure Handling**: Configurable expiration times for failed operations to prevent cache stampedes
- **Result-Aware Caching**: Special handling for `IResult` responses with automatic failure detection

## Installation

```bash
dotnet add package Cirreum.Cache.Hybrid
```

## Usage

### Basic Setup

```csharp
using Cirreum.Cache.Hybrid.Extensions;

// One call: registers the Cirreum bridge AND auto-ensures Microsoft's HybridCache.
services.AddHybridCacheService();
```

If you need to configure `HybridCache` (e.g. default entry options, serializers), call
Microsoft's `AddHybridCache(...)` yourself first — `AddHybridCacheService()` will detect the
existing registration and leave it untouched:

```csharp
services.AddHybridCache(options => { /* custom options */ });
services.AddHybridCacheService();
```

### Query Implementation

```csharp
public record GetUserQuery(int UserId) : ICacheableOperation<User>
{
    public CacheExpirationPolicy CacheExpiration => new(
        Expiration: TimeSpan.FromMinutes(15),
        LocalExpiration: TimeSpan.FromMinutes(5),
        FailureExpiration: TimeSpan.FromMinutes(1)
    );

    public string[] CacheTags => [$"user:{UserId}"];
}
```

The `HybridCacheService` automatically handles:
- Cache-or-create patterns
- Failure result caching with shorter expiration
- Tag-based cache invalidation

## Versioning

Cirreum.Cache.Hybrid follows [Semantic Versioning](https://semver.org/):

- **Major** - Breaking API changes
- **Minor** - New features, backward compatible
- **Patch** - Bug fixes, backward compatible

## License

This project is licensed under the MIT License - see the [LICENSE](LICENSE) file for details.

---

**Cirreum Foundation Framework**
*Layered simplicity for modern .NET*
