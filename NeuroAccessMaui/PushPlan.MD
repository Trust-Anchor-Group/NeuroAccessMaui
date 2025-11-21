# Push Notification Reimplementation Plan

## Vision
Deliver a provider-agnostic push stack that:
- Boots platform transports (Firebase/APNS/WNS) deterministically during MAUI startup.
- Requests and tracks user permission as part of onboarding.
- Registers tokens immediately when they change, independent of XMPP connectivity.
- Provisions broker rules from declarative metadata.
- Delivers background notifications without depending on MAUI DI.
- Offers consistent deep-link routing and in-app notification history across Android, iOS, and Windows.

## Status
- ✅ **Bootstrap foundation**: `RegisterFirebaseServices` is active again and `PushNotificationService` now manages activation hooks plus token verification during its `Load` lifecycle.
- ⚙️ **Token lifecycle**: FCM `TokenChanged` is wired to `PushNotificationService`; retry/backoff added via `PushTokenRegistrar` using `PolicyRunner`, smarter queuing still pending.
- ⚙️ **Rule provisioning**: All broker rules live in `PushRuleDefinitions`, so scripts are centralized, but platform notification UX + intent refactor remain.
- ⏳ **Upcoming work**: Permission/onboarding flow (**deferred for now**), richer platform notification UX, intent pipeline cleanup, and observability tooling.

## Milestones
1. **Bootstrap Foundation**
   - Re-enable and extend `RegisterFirebaseServices`; add hooks for APNS/WNS when supported.
   - Ensure `PushNotificationService` wires activation events during its load routine so push transport checks run deterministically.
   - Ensure iOS delegates (`UNUserNotificationCenterDelegate`) and Android Firebase initialization run before `App` starts.

2. **Permission & UX Flow**
   - Define a notification-permission state machine in onboarding (`Content/Onboarding.md`, related view models).
   - Implement dedicated UI surfaces for granting or revisiting permissions and persist outcomes in `RuntimeSettings`.
   - Block token registration until permission is granted, surfacing actionable errors otherwise.
   - **Deferred**: Pause implementation until we prioritize onboarding UX.

3. **Token Lifecycle Service**
   - Create/extend `PushRegistrationService` that subscribes to platform token events (Firebase `TokenChanged`, APNS callbacks).
   - Standardize a `TokenRegistration` DTO containing `Service`, `ClientType`, and metadata (locale, app version).
   - Queue registrations locally and retry with exponential backoff to the XMPP broker (`XmppService.ReportNewPushNotificationToken`) — initial backoff implemented in `PushTokenRegistrar`, queuing still TODO.

4. **Declarative Rule Provisioning**
   - Model push rules as structured data (JSON/YAML or strongly typed objects) mapping stanza patterns to payload templates.
   - Compute a hash of the rules for versioning; only reapply when the hash changes or the broker requests sync.
   - Build validation tooling that checks localization keys and schema correctness at compile time.

5. **Platform Notification Layer**
   - Refactor Android/iOS notification helpers so native services can display notifications without MAUI (inject lightweight `INotificationRenderer` that uses platform APIs directly).
   - Platform renderers live under `Platforms/Android` and `Platforms/iOS` and are selected via DI; use per-channel IDs aligned to `Constants.PushChannels` (Android channels created on demand with NotificationCompat/NotificationManagerCompat; attach PendingIntent for routing).
   - Support grouped notifications (per conversation, contract, etc.) and unique IDs to avoid overwrites.
   - Introduce a Windows implementation using toast notifications or explicitly disable push features when unsupported.

6. **Intent & Deep-Link Pipeline**
   - Define a compact payload schema shared by all push types (fields such as `action`, `entityId`, `extra`).
   - Update `IntentService` to parse this schema and route to pages/view models uniformly.
   - Ensure push payloads recorded in local storage sync with `NotificationService` so the in-app notification center mirrors OS alerts.
   - Keep intents platform-agnostic: define a small intent model and let renderers attach it (UserInfo on iOS, extras/PendingIntent on Android) for consistent navigation.

8. **Notification Service Redesign**
   - Rebuild `NotificationService` as the single store over Waher DB using a new `Notification` + `NotificationIntent` model (channels, action/entity, state).
   - Migrate legacy `NotificationEvent` data into the new schema (map categories/types to `Constants.PushChannels`, hash-based IDs for dedupe).
   - Update all producers (XMPP events, push taps) to ingest via `NotificationIntent` and route consumption through the unified service/UI.

7. **Observability & QA**
   - Add structured logging around token registration, rule provisioning, and notification delivery (with privacy-safe metadata).
   - Provide developer-facing diagnostics (page/CLI) to trigger sample pushes, inspect current token state, view last push payload, and flush broker rules.
   - Document manual test matrices per platform (permission granted/denied, cold start, background, multi-device).

## Architecture Notes
- Keep `PushNotificationService` transport-agnostic where possible. Add slim transport adapters (e.g., Firebase, APNS, WNS) behind an `IPushTransport` that raises token changes/incoming payloads.
- Introduce a minimal `IPushTokenRegistrar` (can live alongside `PushNotificationService`) to handle dedupe/backoff/persistence for broker registration without bloating the transport surface.
- Inject a thin `INotificationRenderer` per platform to display local notifications without MAUI DI coupling; `PushNotificationService` orchestrates and routes through these abstractions.

## Dependencies & Risks
- Requires Firebase/Apple/Windows credentials for each environment.
- Declarative rules need buy-in from backend team to ensure compatibility.
- Platform refactors might touch onboarding UX and DI setup; coordinate with UI team.

## Success Criteria
- Tokens register within seconds of rotating, even if the app is offline.
- Permission state is transparent to users and recoverable via settings.
- Notification payloads render consistently across platforms and persist in-app.
- Adding a new push channel only requires editing rule metadata and localization, not platform code.
