# Notification Service Redesign (Draft)

## Goals

- Single source of truth for in-app notifications across transports (XMPP live events, push taps/cold start, local system notifications).
- Deterministic ingestion and persistence so notifications are available even when XMPP offline messages are replaced by push.
- Platform-agnostic intent/deep-link routing: one schema, parsed centrally, used by navigation.
- Clear lifecycle with timestamps (created, delivered, read, consumed), idempotency, and dedupe.
- Extensible for channels (`Constants.PushChannels.*`), grouping, diagnostics, and per-channel policies.

## Problems With Current Service

- Legacy data structures (type array + category dictionaries) are opaque and hard to extend.
- No unified ingestion from push; if XMPP offline message is replaced by push, the app may never surface it in-app.
- Navigation/intent handling is scattered; no shared intent model for OS taps, deep links, and in-app notification center.
- Limited state tracking (read/consumed) and diagnostics.

## Proposed Domain Model

- **Notification**
  - `Id` (prefer server correlation IDs; else hash of channel + action + entity + correlation + bucketed timestamp)
  - `Channel` (align with `Constants.PushChannels.*`)
  - `Title`, `Body`
  - `Action` (enum persisted as string) and `EntityId`
  - `Extras` (well-known keys + raw JSON), `SchemaVersion`, `RawPayload`
  - Timestamps: `TimestampCreated`, and optional `DeliveredAt`, `ReadAt`, `ConsumedAt`
  - `State` (New, Delivered, Read, Consumed)
  - `Source` (XMPP, Push, Local)
- **NotificationIntent** (platform-neutral)
  - `Action` (enum), `EntityId`, `Extras` (well-known keys + raw), optional `DeepLink`, `Channel`, `Version`
- **Events**
  - `OnNotificationAdded`, `OnNotificationUpdated`, `OnNotificationConsumed` for UI binding.

## Ingestion Flow

1) **XMPP live events** → map to `NotificationIntent` → upsert `Notification` in store → optionally render local notification if foreground rules allow.
2) **Push arrival (tap/cold start/foreground)** → renderer attaches serialized `NotificationIntent` JSON (UserInfo on iOS; extras/PendingIntent on Android) → on tap/launch, parse intent → upsert `Notification` in store → route navigation (queue if shell not ready).
3) **Deep links** → parse into `NotificationIntent` → upsert/consume as above.

## Storage & State

- Persist notifications in Waher DB keyed by Id; include `Channel`, timestamps, `State`, `Payload`, `RawPayload`.
- Mark read/consumed via explicit API; avoid duplicate inserts by hashing channel+action+entityId+timestamp bucket; prefer correlation IDs when available.
- Retention policy (e.g., max N per channel or time-based pruning), prune read/consumed first.

## Migration (Waher DB)

- Add a new `Notification` collection/table (Id, Channel, Action/Entity/Extras, Title/Body, Timestamps, State, Source, RawPayload, SchemaVersion).
- Read legacy `NotificationEvent` records, map Type/Category to `Constants.PushChannels.*`, derive Action/Entity when possible, set Source=`XMPP`, and hash a deterministic Id to avoid duplicates; store LegacyType/Category for traceability.
- Upsert into the new collection; optionally mark legacy entries as migrated or prune after verification; make migration idempotent.
- During rollout, read new model for UI and ingestion; legacy readers can be removed once migration is complete.

## Rendering

- Keep `INotificationRenderer` per platform (already DI-selected). Signature should accept title/body/channel/intent.
- Android: use `NotificationCompat`/`NotificationManagerCompat`, create channels on demand using `Constants.PushChannels.*`, attach `PendingIntent` carrying serialized `NotificationIntent`.
- iOS: use `UNUserNotificationCenter`, attach `NotificationIntent` into `UserInfo`, rely on delegate to forward taps to parser.
- Foreground handling: default to silent ingestion (no OS toast/local notification while app runs), but keep renderer pluggable to enable foreground alerts later.

## In-App Notification Center

- Provide a page/view that binds to the persisted notifications (Waher DB) and shows channel, title, body, timestamps, state (new/read/consumed), source (XMPP/Push/Local), and action/entity metadata for navigation.
- Support actions: mark read, consume/navigate, clear/prune, and optionally filter by channel/state.
- Ensure the stored payload retains enough metadata (Action, EntityId, Extras, Channel, Timestamps, Source) to render and navigate without additional lookups.

## Navigation/Intent Pipeline

- Central parser (via `NotificationIntentRouter`) that takes `NotificationIntent` and maps to navigation (via `NavigationService`/`CustomShell`); supports ignore rules through `INotificationFilter` (e.g., ignore chat notifications when already in that chat).
- All entry points (push tap, deep link, in-app selection) call this parser; no platform-specific routing elsewhere.
- Consumption: `NotificationServiceV2.Consume(notificationId)` marks state and invokes the router.
- Ignoring rules are managed via a runtime filter registry so view models or settings can add/remove filters at runtime.

## Localization Notes
- New UI strings expected for notifications page redesign: `NotificationSearchPlaceholder`, `NotificationsAllLabel`, `NotificationsUnreadLabel`, `NotificationsClearAllLabel`, `NoNotificationsLabel`.

## Expectations & Auto-Consume

- Allow view models to register expectations with correlation/predicate and timeouts (e.g., “expect a presence subscription response matching X within T”). When a matching notification arrives, auto-consume it, mark state, and optionally trigger a popup or navigation.
- Provide an API akin to `ExpectEvent` that operates on the new `NotificationIntent`/`Notification` model, prefers correlation IDs, dedupes by Id, and honors time windows to avoid stale matches (implemented as `WaitForAsync` and fire-and-forget `ExpectAsync` in `NotificationServiceV2`).

## API Sketch (Service)

- `Task AddAsync(NotificationIntent intent, NotificationSource source)` → upsert `Notification`, emit `OnNotificationAdded`.
- `Task ConsumeAsync(string id)` → mark consumed, emit event, call parser.
- `Task<IReadOnlyList<Notification>> GetAsync(filter)` → channel/state filters for UI.
- `Task PruneAsync()` → retention.
- `Task WaitForAsync(...)` → await matching notification based on predicate/timeout.
- `Task ExpectAsync(...)` → register a fire-and-forget expectation that auto-routes on match.

## Diagnostics

- Log ingestion source, channel, and routing outcome; expose last N notifications and last error for QA tooling.
- Optional developer page to trigger sample notifications and view stored intents.

## Open Decisions

- Id generation: deterministic hash vs. GUID; needs dedupe across push/XMPP for same event.
- Retention limits and read/consumed semantics (remove vs. archive).
