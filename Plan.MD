# Fetching, Caching, and Invalidation Strategy

This plan standardizes how the app fetches items from servers (now: Theme; next: KYC) with robust caching, TTL-based invalidation, and event-driven cache busting via PubSub. Goals: maintainability, minimal network traffic, resilience offline, and consistent behavior across services.

## Objectives
- Reliable fetching with graceful fallbacks and retries.
- Layered caching with TTL and optional permanence.
- Event-driven invalidation tied to PubSub updates.
- Unified APIs to keep feature code simple and consistent.
- Minimize data transfer using conditional requests when possible.

## Design Overview
- Layered cache: in-memory (fast, short-lived) over persistent disk cache (existing `FileCacheManager` via `IInternetCacheService`).
- Standard fetch utility: a reusable `IResourceFetcher` that wraps: cache lookup -> HTTP GET (conditional using ETag/Last-Modified if available) -> parse/validate -> store -> return.
- Cache policy per resource type: TTL, permanence, key/parent grouping, and stale-while-revalidate option.
- Event-driven invalidation: a `ICacheInvalidationService` that listens to PubSub events and broadcasts invalidation messages, plus removes affected keys from cache.
- Consistent keying: use absolute URI strings for HTTP resources; for logical groups use `ParentId` (e.g., `Branding:{domain}`, `KycProcess:{domain}`).

## Resilience Layer (Reuse MVVM Policies outside UI)
- Existing MVVM policies (`Retry`, `Timeout`, `Bulkhead`, `CircuitBreaker`, `Debounce`) already provide a Polly-like foundation and are UI-agnostic.
- Introduce a non-UI runner (e.g., `PolicyRunner`) to compose and execute these policies in services without `ObservableTask`/UI dependencies:
  - `Task RunAsync(Func<CancellationToken, Task> action, CancellationToken ct, params IAsyncPolicy[] policies)`
  - `Task<T> RunAsync<T>(Func<CancellationToken, Task<T>> action, CancellationToken ct, params IAsyncPolicy[] policies)`
- Extend `IAsyncPolicy` with generic overloads to avoid boilerplate captures in typed operations:
  - `Task<T> ExecuteAsync<T>(Func<CancellationToken, Task<T>> action, CancellationToken ct)`
- Add jittered backoff helpers and transient-classification utilities for HTTP/XMPP exceptions (so `Retry` only retries transient failures by default).
- Keep `ObservableTaskBuilder` for ViewModels; have it internally delegate to the same `PolicyRunner` to avoid duplicated logic.

### ObservableTask decoupling for broader use
- Add an injectable dispatcher abstraction:
  - `public interface IDispatcher { void Post(Action action); }`
  - Default UI implementation uses `MainThread.BeginInvokeOnMainThread`.
  - A `ImmediateDispatcher` (no-op marshal) for service-side/background scenarios.
- Extend `ObservableTaskOptions` to accept a dispatcher or a simple `DispatchOnMainThread` boolean (default true). Ensure all UI notifications go through the dispatcher.
- Result: `ObservableTask` can run in services without requiring a UI thread while still benefiting from policies and telemetry when desired.

### Namespace/layout adjustments (non-breaking)
- Move or mirror the `Policies` and `TaskContext` types to a neutral folder/namespace (e.g., `Services/Resilience`) and keep a small forwarding type or updated `using` in UI code to avoid churn.
- Add `PolicyRunner` to the same neutral namespace for reuse by Theme, KYC, and future services.

## Core Components
- `IInternetCacheService` (exists): persistent, byte-oriented cache with TTL. Extend with:
  - Remove by parent ID: `Task<int> RemoveByParentId(string parentId)`.
  - Optional metadata storage (ETag, Last-Modified) to enable conditional GETs.

- `IResourceFetcher` (new): simple façade for services.
  - `Task<ResourceResult<T>> GetAsync<T>(Uri uri, ResourceFetchOptions options, Func<byte[], T> parse)`
  - Handles: in-memory cache, `IInternetCacheService`, HTTP fetch with retries/backoff, conditional GET, validation, and storage.
  - Returns typed results and origin info: cache hit, network hit, 304 (not modified), or fallback.

- `ICacheInvalidationService` (new): declarative subscriptions and invalidation.
  - `void Subscribe(string topic, CacheInvalidationRule rule)` where `topic` maps to PubSub nodes/items.
  - `Task InvalidateByKeys(IEnumerable<string> keys)` and `Task InvalidateByParentId(string parentId)`.
  - Publishes `CacheInvalidatedMessage` via `WeakReferenceMessenger` so view models/services can refresh.
- `PolicyRunner` (new, service-safe): central place to apply retry/timeout/bulkhead/circuit-breaker to IO calls used by `IResourceFetcher` and future services.

- Messaging
  - Use `WeakReferenceMessenger` (already used) for local app notifications: `CacheInvalidatedMessage { Scope, Keys, ParentId }`.
  - Integrate with `XmppService` PubSub client to convert PubSub item events into invalidation calls.

## Policies (per resource)
- Theme
  - Branding descriptor (V2 preferred, V1 fallback): TTL 6–24h (configurable); ParentId: `Branding:{domain}`.
  - Image assets: TTL 7d (existing `Constants.Cache.DefaultImageCache`); ParentId: `BrandingAssets:{domain}`.
  - Invalidation: PubSub update on `NeuroAccessBranding` node -> invalidate descriptor + affected images.

- KYC
  - KYC process XML: TTL 24h; ParentId: `KycProcess:{domain}`; fallback to embedded test if network fails.
  - Invalidation: PubSub update on `NeuroAccessKyc` node -> invalidate `KycProcess:{domain}` and notify.

## Data Flow
1) Service requests resource via `IResourceFetcher` with a `ResourceFetchOptions` policy.
2) Fetcher checks in-memory cache -> disk cache via `IInternetCacheService`.
3) If miss/stale, uses HTTP with conditional headers (ETag/If-Modified-Since) when stored.
4) If 304 Not Modified: refresh metadata/expiry, return cached.
5) If 200 OK: validate/parse, store to caches, return.
6) On PubSub event: `ICacheInvalidationService` removes relevant cache entries and broadcasts `CacheInvalidatedMessage`.

## HTTP Optimizations
- Prefer conditional GETs over HEAD+GET to minimize round-trips.
- Store and reuse ETag/Last-Modified when server supports it; fall back to TTL only.
- Exponential backoff on transient errors (e.g., 0s, 2s, 5s) as in ThemeService.
- Optional stale-while-revalidate: serve stale cache if network fails, refresh in background (future extension: requires soft-expiry vs hard-expiry).
- Apply resilience policies consistently via `PolicyRunner`:
  - `Timeout`: use `Constants.Timeouts.DownloadFile` for downloads.
  - `Retry`: jittered backoff (e.g., decorrelated jitter) for transient HTTP and socket errors only.
  - `Bulkhead`: limit concurrent network fetches per domain to prevent thundering herd.
  - `CircuitBreaker`: per-domain breaker on repeated failures to cut load and surface failures quickly.

## API Sketches

```csharp
public record ResourceFetchOptions(
    string ParentId,
    TimeSpan Ttl,
    bool Permanent = false,
    bool UseMemoryCache = true,
    bool EnableConditionalGet = true
);

public enum ResourceOrigin { Memory, Disk, Network, NotModified, Fallback }

public sealed record ResourceResult<T>(T? Value, ResourceOrigin Origin, string ContentType = "");

public interface IResourceFetcher
{
    Task<ResourceResult<T>> GetAsync<T>(
        Uri uri,
        ResourceFetchOptions options,
        Func<byte[], T> parse,
        CancellationToken ct = default);
}

public sealed record CacheInvalidatedMessage(
    string Scope,                // e.g., "Branding", "Kyc"
    string? ParentId = null,
    IReadOnlyList<string>? Keys = null
);

public interface ICacheInvalidationService
{
    void Subscribe(string pubSubNodeId, Func<PubSubEvent, Task> handler);
    Task InvalidateByParentId(string parentId);
    Task InvalidateByKeys(IEnumerable<string> keys);
}
```

## PubSub Integration
- `XmppService` already wraps PubSub and exposes `GetItemsAsync` etc. Extend with event hookup:
  - Map nodes to cache scopes:
    - `NeuroAccessBranding` -> `Branding:{domain}` descriptors and `BrandingAssets:{domain}` images.
    - `NeuroAccessKyc` -> `KycProcess:{domain}`.
  - On item publish or retract, call `ICacheInvalidationService.InvalidateByParentId(...)` and broadcast `CacheInvalidatedMessage`.
  - Theme and KYC services listen and refresh when relevant.

## Migration Plan (Incremental)
1) Add `RemoveByParentId` to `IInternetCacheService` (uses `FileCacheManager`).
2) Extract/mirror `Policies` (+ `TaskContext`) to `Services/Resilience` and add `PolicyRunner` (+ generic `IAsyncPolicy<T>` overloads).
3) Extend `ObservableTaskOptions` with dispatcher or `DispatchOnMainThread`; audit `ObservableTask` to route notifications via dispatcher.
4) Introduce `IResourceFetcher` with in-memory + disk + network logic, built on `PolicyRunner` (timeout/retry/circuit/bulkhead).
5) Add `ICacheInvalidationService` and wire to `WeakReferenceMessenger`.
6) ThemeService: refactor to use `IResourceFetcher`; switch to parent-based keys; enable conditional GET where supported.
7) KycService: switch to `IResourceFetcher` for process XML with 24h TTL and fallback; group by `KycProcess:{domain}`.
8) XmppService: add PubSub handlers to call `ICacheInvalidationService` on branding/KYC updates.
9) Add lightweight tests for cache TTL, invalidation, `PolicyRunner` behaviors (retry-on-transient, breaker states), and conditional GET; add manual QA checklist.

## Suggested TTLs
- Branding descriptor: 6–24h (configurable, default 12h).
- Branding images: 7d (existing constant).
- KYC process XML: 24h.

## Edge Cases and Resilience
- Domain change or sign-out: invalidate `Branding:*` and `KycProcess:*` for previous domain.
- Offline: serve cached entries; if expired and no network, optionally use last known good (future soft-expiry).
- Partial updates: when PubSub indicates specific item IDs, compute affected keys and invalidate selectively.
- Backoff on transient errors; log and fall back to last applied theme/KYC process.

## Implementation Notes
- Keep keys consistent: cache keys should be absolute URIs; group entries using `ParentId` for bulk invalidation.
- Store metadata for conditional GET in DB alongside `CacheEntry` (add columns if/when implemented).
- Provide simple helpers in services:
  - ThemeService: `GetBrandingUri(itemId)` and key builders.
  - KycService: `GetKycProcessUri()` and key builders.
- Add developer utilities: “Clear cache” command invoking `RemoveByParentId` for current domain scopes.

## Testing Strategy
- Unit: `TryGet` eviction on expiry, `RemoveByParentId`, basic `IResourceFetcher` paths (memory/disk/network/304).
- Integration (manual/dev): toggle network, simulate PubSub events, verify UI refreshes + minimal requests (inspect logs).
- Performance: ensure no UI thread blocking; use background tasks for fetch and invalidation.

## Next Steps (Concrete)
- [ ] Add `RemoveByParentId` to `FileCacheManager` and `IInternetCacheService`.
- [ ] Extract/mirror `Policies` to `Services/Resilience`; add generic overloads; implement `PolicyRunner` with jitter helpers and transient classification.
- [ ] Update `ObservableTask` to support an injectable dispatcher for non-UI use; extend `ObservableTaskOptions` accordingly.
- [ ] Define `IResourceFetcher` and default implementation leveraging `PolicyRunner`.
- [ ] Implement `ICacheInvalidationService` + wire to `XmppService` PubSub events.
- [ ] Refactor ThemeService to use fetcher and parent grouping.
- [ ] Update KycService to use fetcher; set TTL 24h + fallback.
- [ ] Add dev menu action: clear branding + KYC caches for current domain.
- [ ] Optional: ETag/Last-Modified support in cache; conditional GETs.
