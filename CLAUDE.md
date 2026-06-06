# CLAUDE.md

This file provides guidance to Claude Code (claude.ai/code) when working with code in this repository.

## Project Overview

This is a .NET 10.0 library package (`Cirreum.Cache.Hybrid`) that provides a hybrid caching implementation for the Cirreum Conductor framework's cacheable queries. The library bridges Microsoft's `HybridCache` service with Cirreum's `ICacheService` interface.

> Renamed from `Cirreum.QueryCache.Hybrid` during the Cirreum 1.0 foundation reset, with a
> code-first registration model.

## Common Development Commands

### Build Commands
```bash
# Restore dependencies
dotnet restore Cirreum.Cache.Hybrid.slnx

# Build the solution
dotnet build Cirreum.Cache.Hybrid.slnx --configuration Release --no-restore

# Pack for NuGet
dotnet pack Cirreum.Cache.Hybrid.slnx --configuration Release --no-build --output ./artifacts
```

## Architecture

### Core Components

1. **HybridCacheService** (`src/Cirreum.Cache.Hybrid/HybridCacheService.cs`)
   - Main implementation of `ICacheService` using Microsoft's `HybridCache`
   - Handles cache-or-create patterns with failure expiration logic
   - Supports cache invalidation by key and tags

2. **ServiceCollectionExtensions** (`src/Cirreum.Cache.Hybrid/Extensions/ServiceCollectionExtensions.cs`)
   - Provides the `AddHybridCacheService()` extension method for DI registration
   - **Code-first provider selection**: the call itself selects the provider, delegating to the
     foundation's `AddCacheService(factory)` helper (from `Cirreum.Domain`). No `CacheProvider`
     enum or `Cirreum:Cache:Provider` appsettings switch.
   - **Auto-ensures** Microsoft's `AddHybridCache()` (idempotent / `TryAdd`-based), so a single
     call wires up the underlying `HybridCache` out of the box.

### Key Dependencies
- `Cirreum.Domain` (1.x) - Provides `AddCacheService` + the cache abstractions (`ICacheService`, `CacheExpirationPolicy`). Contracts/Kernel flow transitively.
- `Microsoft.Extensions.Caching.Hybrid` - Microsoft's hybrid caching implementation, supplying both the `HybridCache` abstraction and the `AddHybridCache()` registration.

### Caching Behavior
- Supports both success and failure response caching with different expiration times
- Implements tag-based cache invalidation
- Automatically handles local (L1) vs distributed (L2) cache expiration settings
- Special logic for `IResult` responses to cache failures with shorter expiration

## Project Structure

- **Solution**: Uses `.slnx` format (Visual Studio solution)
- **Build System**: MSBuild with custom `.props` files in `/build/` folder
- **Versioning**: Package version derives from the Git tag in CI; `build/Versioning.props` holds the local default
- **Package Management**: Configured for NuGet publishing via GitHub Actions
- **Target Framework**: .NET 10.0 with latest C# language version and nullable reference types enabled

## CI/CD

The project uses GitHub Actions for automated publishing to NuGet. The workflow is triggered on releases and handles:
- Version extraction from git tags
- Building and packing the library
- Publishing to NuGet.org with OIDC authentication

## Usage Pattern

Typical registration — a single call:

```csharp
services.AddHybridCacheService(); // registers the Cirreum bridge AND auto-ensures AddHybridCache()
```
