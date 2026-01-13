# Chat Module Modernization Plan

## 1. Vision & Guardrails
- Align chat UX with app-wide design language (BaseContentPage, shared controls, localization).
- Preserve XMPP transport, encryption flows, and embed experiences for IDs, contracts, wallet assets.
- Deliver maintainable separation between domain services, MVVM orchestration, and UI templates.
- Prioritize responsiveness on mid-tier Android hardware; avoid blocking the UI thread.
- Allow gradual rollout by keeping current chat page available until new implementation reaches parity.

## 2. Baseline Snapshot
### 2.1 UI Layer (`NeuroAccessMaui/UI/Pages/Contacts/Chat/ChatPage.xaml`)
- ScrollView plus VerticalStackLayout retains every message, causing memory and scrolling issues on long threads.
- Manual ScrollTo logic in code-behind and scattered focus behaviors complicate state handling.
- Action tray ties Expander state directly to IsWriting without adaptive layout or accessibility metadata.
- Inline frame styling duplicates padding/margin logic instead of relying on shared resources.

### 2.2 ViewModel (`NeuroAccessMaui/UI/Pages/Contacts/Chat/ChatViewModel.cs`)
- Single 1551-line class mixes navigation, transport, storage, rendering, and composer state.
- LinkedList collections manage frames/messages and directly manipulate layout children.
- Markdown parsing and XAML generation run synchronously on the UI thread for every message.
- Wide usage of MainThread.BeginInvokeOnMainThread with locks makes threading hard to reason about.
- Command enablement intertwines network availability, composer, and media flows inside the same class.

### 2.3 Persistence & Rendering (`NeuroAccessMaui/UI/Pages/Contacts/Chat/ChatMessage.cs`)
- Direct Waher.Persistence calls (Find/Insert/Update) occur inside the view model without abstraction.
- `ChatMessage` stores parsed XAML and command instances, coupling persistence with UI concerns.
- Delivery, read, edit, and reply metadata are absent; updates overwrite content without history.
- Paging uses fixed batch sizes and does not cache or evict data intelligently.

### 2.4 Messaging & Integration
- Transport pipeline calls `ServiceRef.XmppService` directly for send, upload slots, presence, and receipts.
- Media capture, encryption, upload, and retry logic live in the view model.
- Notification cleanup invokes `ServiceRef.NotificationService` ad hoc after initialization.
- Composer share commands (ID, contract, wallet, tokens, things) rely on imperative navigation paths.

## 3. Target Architecture
### 3.1 Solution Structure
- Create `Services/Chat` namespace covering repositories, transport adapters, rendering, and media orchestration.
- Split chat UI assets into `UI/Pages/Contacts/Chat/Session`, `Composer`, and `Templates` for clarity.
- Introduce `ChatSessionViewModel : XmppViewModel` to own session state and delegate specialized work.
- Extract composer experience into `ChatComposerView` with a dedicated `ChatComposerViewModel`.
- Maintain legacy page behind a navigation flag until the new stack delivers feature parity.

### 3.2 Domain Services
- `IChatMessageRepository` wraps Waher.Persistence for paging, inserts, updates, and soft deletes.
- `IChatTransportService` encapsulates XMPP send/receive, upload slot negotiation, receipts, and presence hints.
- `IMarkdownRenderService` converts markdown to renderable blocks off-thread with caching.
- `IMediaPipeline` manages capture, validation, encryption, upload, cancellation, and progress reporting.
- `IChatEventStream` (mediator) batches inbound/outbound events and marshals them via `IDispatcherAdapter`.

### 3.3 Rendering & Content Pipeline
- Remove UI objects from `ChatMessage`; persist only content, metadata, and render hashes.
- Queue render jobs through `ObservableTaskBuilder` to keep heavy work off the UI thread.
- Cache rendered fragments keyed by message id and content hash to avoid reprocessing.
- Provide adapters covering plain text, markdown, system events, replies, and attachment previews.

### 3.4 Presentation Layer
- Replace ScrollView/stack with a `CollectionView` using `ItemsLayout="VerticalList"` and `RecycleElement`.
- Use a `DataTemplateSelector` for sent, received, system, and media messages with pure bindings.
- Track scroll state via attached behavior to expose `IsAtBottom` and show a "New messages" banner.
- Bind composer view to `ChatComposerViewModel` exposing markdown, reply target, edit mode, and validation.
- Surface context actions (reply, edit, copy, retry, delete) through toolkit menus or popups.

### 3.5 Message State & Session Semantics
- Extend message model with `DeliveryStatus`, `IsEdited`, `ReplyToId`, `LocalTempId`, and reaction placeholders.
- Surface session-level aggregates such as unread count, typing indicators, presence, and sync timestamp.
- Drive paging through a `ChatHistoryLoader` coordinating repository calls and telemetry.
- Let notification cleanup run through repository/service layer instead of inline view model calls.

## 4. Data & Persistence
- Ship migration adding new fields while backfilling defaults for existing `ChatMessage` records.
- Normalize stored content (markdown, plain text, metadata) and drop persisted parsed XAML.
- Implement caching window for recent items and fall back to disk paging for older history.
- Preserve existing Waher indexes (`RemoteBareJid`, `Created`, `RemoteObjectId`) or adjust if needed.

## 5. Implementation Plan
### Phase 1 – Contracts & Scaffolding *(Completed)*
- Introduced dedicated chat service interfaces and implementations (`IChatMessageRepository`, `IChatTransportService`, `IMarkdownRenderService`, `IMediaPipeline`, `IChatEventStream`, `IChatMessageService`) within `Services/Chat`.
- Routed resolution through `ServiceRef`, `AppShell`, and `PageAppExtension` so downstream callers use injectable abstractions rather than `ChatViewModel` helpers.
- Outcome: future features can plug into the service layer without modifying legacy view models.

### Phase 2 – Persistence & Migration *(Completed)*
- Added storage-only models (`ChatMessageRecord`, `ChatMessageDescriptor`) and `ChatMessageRepository` for paging, delivery updates, and CRUD.
- Implemented `ChatMessageMigration` executed during repository load to backfill new fields (`DeliveryStatus`, `ReplyToId`, `LocalTempId`, etc.) on legacy rows.
- Repository now serves the session view model and new services without leaking UI concepts.

### Phase 3 – Rendering Pipeline *(Completed)*
- Delivered `MarkdownRenderService` generating plain text + HTML segments off-thread with caching and cancellation support.
- `ChatSessionViewModel` consumes the service to render message descriptors without rebuilding XAML in the UI layer.
- Next: tune cache policy and add localized placeholders post-integration testing.

### Phase 4 – Presentation Layer *(In Progress)*
- Built `ChatSessionPage` with a virtualized `CollectionView`, composer row, infinite scroll, and bound it to the new `ChatSessionViewModel` + `ChatMessageItemViewModel`.
- Updated app routing and DI so all navigation flows open `ChatSessionPage`; legacy `ChatPage` remains for parity testing.
- **Next**: introduce template selector for message variants, implement context menu actions, add new-message banner/scroll tracking, and remove dependency on `ChatViewModel` helpers once coverage is complete.

### Phase 5 – Media & Advanced Flows
- **Current assets** embed commands (`EmbedFileCommand`, etc.) live in `ChatViewModel`, handling encryption, upload slots, and navigation.
- **Refactor** move embed logic into `IMediaPipeline` implementation; ensure audio/photo capture uses shared services already leveraged in other modules (e.g., `PhotosLoader` in applications pages).
- **New work** implement upload progress reporting, cancellation, retry via Polly; surface progress UI inside message bubbles; handle temporary files and cleanup; unify composer actions with new pipeline (ID, contract, wallet, token, thing); maintain compatibility with existing navigation services for identity and contract pickers.
- **Dependencies** coordinate with `ServiceRef.XmppService` for upload slot negotiation and receipts; ensure Mopups interactions still function when triggered from the new composer.

### Phase 6 – Telemetry, Accessibility, Rollout
- **Current assets** telemetry hooks exist via `ObservableTaskTelemetry`; localization resources in `AppResources.*.resx`.
- **Refactor** centralize telemetry naming and ensure render/send/upload events adhere to existing analytics conventions; audit accessibility metadata for new controls.
- **New work** emit metrics for render durations, paging latency, upload outcomes, resend attempts; add localized strings for new UI labels across all resource files; validate dynamic font scaling and screen reader announcements; implement feature flag to swap navigation from legacy to new page once parity achieved; document migration plan and rollback procedure.
- **Dependencies** align with QA and release processes to stage rollout, and update documentation where chat behavior is referenced.

## 6. Cross-Cutting Concerns
### 6.1 Resilience & Concurrency
- Apply Polly policies for send, upload, and render retries with jittered backoff.
- Route UI updates through `IDispatcherAdapter` to centralize main-thread dispatching.
- Add cancellation support to repository and transport operations for long-running tasks.
- Favor immutable view models for message entries to avoid locking strategies.

### 6.2 Telemetry & Diagnostics
- Emit `ObservableTaskTelemetry` events for render, send, upload, and paging operations.
- Log structured records with message ids and correlation tokens via existing logging service.
- Track UI responsiveness metrics such as scroll latency and event queue depth.
- Forward critical failures to App Center or the configured analytics sink once aggregated.

### 6.3 Accessibility & Localization
- Assign automation names/descriptions to buttons, bubbles, and status banners.
- Respect dynamic font scaling and theme resources already defined in the app dictionaries.
- Localize new strings (Edited, Retry, New messages, Upload failed) across all `.resx` variants.
- Ensure gestures have keyboard-accessible or alternative command paths.

### 6.4 Security & Compliance
- Keep encryption keys and blob handling within the media service; do not leak to UI.
- Validate inbound markdown/media size before enqueueing to renderer to prevent spikes.
- Delete or shred temporary capture files once uploads complete or fail.
- Respect existing consent flows when sharing identities, contracts, or wallet content.

### 6.5 Performance & Offline
- Preload recent history on session open and fetch older pages when thresholds are reached.
- Batch event handling (e.g., 16 ms coalescing) before updating observable collections.
- Maintain an offline send queue that retries via `ServiceRef.XmppService` when connectivity returns.
- Trim cached render fragments when memory thresholds or LRU limits are exceeded.

## 7. Integration Touchpoints
- XMPP extensions relied upon for chat flows:
  - XEP-0184 (Message Delivery Receipts) for acknowledging outgoing messages and updating delivery state when receipts arrive.
  - XEP-0085 (Chat State Notifications) for typing indicators, with `XmppClient.EnableChatStateNotifications` toggled after connect and chat-state events bridged through the chat event stream.
  - XEP-0332/XEP-0363 (HTTP File Upload) for media slot negotiation in the media pipeline.
  - XEP-0199 (XMPP Ping) already used in service health checks; ensure compatibility.

- `ServiceRef.XmppService` for transport, receipts, and upload slot negotiation.
- `ServiceRef.NotificationService` to reconcile pending contact notifications.
- `ServiceRef.UiService` for navigation to identity, contract, wallet, and thing pages.
- `Mopups` flows for popups such as Subscribe-To and media previews.
- `NeuroAccessMaui.Services.Localization` for runtime string resources.

## 8. Risks & Mitigations
- **File embed regressions**: Build integration harness covering photo, contract, wallet flows before switching default page.
- **Schema migration issues**: Provide backup/export guidance and stage migration through QA builds before production.
- **Scroll performance gaps**: Prototype CollectionView with seeded data early and profile on low-end devices.
- **Transport edge cases**: Run legacy and new pipelines in parallel with telemetry comparison prior to cutover.

## 9. Success Criteria
- Render 50 recent messages in under 250 ms on a Pixel 6 release build.
- Keep additional memory usage below 30 MB after loading 500 mixed messages.
- Achieve >98% automatic retry success rate for media uploads.
- Pass automated accessibility checks and manual screen reader review without blockers.

## 10. Immediate Next Steps
- Confirm dependency injection approach for new chat services with platform architecture.
- Draft and validate `ChatMessage` migration against a representative database backup.
- Build a CollectionView prototype with sample templates to validate visual design.
- Align telemetry naming and dimensions with existing analytics conventions.

## 11. Deferred Items
- Reactions UI (design hooks preserved for future work).
- Threaded conversations beyond single reply references.
- Message search and global conversation indexing.
- Multi-device conflict resolution beyond delivery receipts.

## 12. Progress Snapshot & Upcoming Focus
- **Recently completed**
  - Service layer: repositories, transport abstraction, markdown renderer, media/event scaffolding, and new `ChatMessageService` for outbound sends.
  - Persistence modernization with migrations and descriptor mapping.
  - First pass of the new chat session UI, navigation reroute, and composer support.
  - XEP-0085 chat-state negotiation enabled end-to-end (XmppService, transport adapter, session view model, and UI indicator).
  - XEP-0184 delivery receipts parsed and persisted; outbound IDs align with local temp IDs for reliable receipt mapping.
  - Chat message actions moved from SwipeView to a long-press action bar with reply/edit/copy/delete.
- **In-flight / next**
  - Polish remote message corrections UI (reply/edit badges) and verify XEP-0308 flows end-to-end.
  - Build template selector and context menus so the UI matches the legacy feature set (reply/edit/copy, status badges, and new-message banner).

## 13. Future Enhancements – Chat Dashboard
- **Concept**: Add a chat-oriented entry inside `AppsPage` (DM-style) featuring conversation previews, unread indicators, and quick actions.
- **Data Sources**: Use `ChatMessageRepository` to pull latest message summaries, delivery states, and per-contact stats for tiles.
- **Interaction Model**: Support filtering (people vs. services), pinning favorites, swipe actions for mute/read, and shortcuts to start new sessions.
- **Timing**: Schedule after Phase 6 rollout activities so the dashboard builds atop the stabilized service and presentation layers.
