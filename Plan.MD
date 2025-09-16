# KYC Process Refactoring Plan (Updated)

Goal
Transform `KycProcessViewModel` from a multi-responsibility monolith into a thin orchestration layer delegating to cohesive, testable services (mapping, validation, navigation state, summary, reference persistence, peer review, attachments) without functional regressions.

Guiding Principles
- Preserve user-visible behavior each phase (navigation, resume, rejection, summary, submission, peer review).
- Isolate pure logic early (enables unit tests, reduces branching risk later).
- Avoid premature file churn (no partials unless unavoidable) – extract services directly.
- Prefer stateless or minimally stateful services; ViewModel owns only UI state.
- Deterministic ordering in mapping & summary for stable tests.
- Explicit main-thread marshaling; no hidden async void or racey updates.
- Replace boolean flag mesh with explicit flow state model.

Current Responsibility Buckets (Baseline Observations)
1. Initialization & resume (pending/rejected/draft) logic
2. Navigation state (page indices, summary mode, progress)
3. Validation orchestration (duplicate loops for page + first-invalid scan)
4. Mapping & transform pipeline (fields → Properties + grouped date synthesis)
5. Attachment & image compression (Skia decode, orientation, resize)
6. Summary projection (grouping + invalid highlighting)
7. Peer review feature (featured providers, petitions, QR scanning)
8. Reference persistence (autosave, rejection application, invalid claim replay)
9. Rejection/invalidation augmentation (grouped claim key expansion)
10. Event handling & throttled UI refresh (page/section/field changes)
11. Command flow (Next/Previous/Summary/Apply/Revoke)
12. Direct platform/service calls scattered (tight coupling)

Primary Risks
- Flag drift: `ShouldViewSummary` + `ShouldReturnToSummary` + anchor index interplay.
- Validation races (async rule completion after navigation).
- Silent transform failures (exceptions swallowed → partial property set).
- Attachment processing unbounded memory usage.
- Duplicated validation enumeration patterns.
- Invalidation logic spread across multiple methods (risk inconsistent updates).

Target Architecture (End State)
`KycProcessViewModel` responsibilities:
- Bind properties/collections for UI.
- Delegate: mapping, validation, summary, state transitions, persistence, peer review, image processing.
- React to events → update observable properties.
- No direct transform, compression, or invalidation logic.

Service Interfaces (Conceptual)
- `IKycFieldMapper` → Builds `KycMappingResult(Properties, Attachments)` from process & reference.
- `IKycValidationService` → Page validation + first invalid page discovery.
- `IKycNavigationStateService` → Computes and mutates `KycNavigationSnapshot` (`KycFlowState`).
- `IKycSummaryService` → Builds `KycSummaryModel` (grouped display quads + invalid sets).
- `IKycReferenceService` → Update & persist `KycReference`, rejection application, invalidation context.
- `IImageProcessingService` → Safe compression (orientation, max dimension, quality profile, size guard).
- `IPeerReviewService` → Attributes, featured reviewers, petition operations.
- (Optional) `IMainThreadDispatcher` → Unified thread marshaling abstraction.

Flow State Model
`enum KycFlowState { Form, Summary, EditingFromSummary, RejectedSummary, PendingSummary }`
`KycNavigationSnapshot { int CurrentPageIndex; int AnchorPageIndex; KycFlowState State; }`

Metrics (Track Each Phase)
- ViewModel LOC (target: reduce by ≥50%).
- Number of direct `ServiceRef.*` usages (trend toward only UI-related).
- Cyclomatic complexity of next/previous execution logic.
- Duplication count (validation & mapping loops → 0).
- Async warnings / nullable warnings (no increase).
- Allocation hotspots (post Phase 11 profiling snapshot).

Smoke Test (Executed After Each Phase)
1. Resume draft (form) → lands on expected page.
2. Resume draft (summary last) → summary or first invalid page.
3. Edit invalid field → summary apply disabled until fixed → return to summary.
4. Submit application → attachments persisted, state pending, summary read-only.
5. Rejection simulation → invalid reasons appear, summary highlights.
6. Revoke application → returns to editable summary.
7. Peer review request (featured + QR) path works.

Phases (Condensed: 12)

Phase 1: Baseline & Instrumentation
Tasks:
- Add XML doc header summarizing current responsibilities, risks.
- Add TODO markers around major logic blocks (// TODO[KYC:MAPPING], etc.).
- Record baseline metrics (LOC, field count, direct service calls).
Acceptance: No logic change, build passes.

Phase 2: Define Core Interfaces (Contracts Only)
Tasks:
- Add interface files (no implementations) for mapper, validation, navigation state, summary, reference, image, peer review, dispatcher.
- Document contracts (inputs/outputs, thread expectations, determinism guarantees).
Acceptance: Compiles; VM unchanged except added using statements if needed.

Phase 3: Extract Mapping & Attachment Pipeline
Tasks:
- Move `BuildMappedValues*`, `BuildPropertiesFromFieldAsync`, file handling into `KycFieldMapper`.
- Introduce `KycMappingResult` DTO (ordered `Properties` + `Attachments`).
- Deterministic ordering: page order, field order, mapping order.
- Wrap transform application with try/catch + structured logging.
Acceptance: Summary still builds; VM no longer contains mapping internals.

Phase 4: Validation Orchestration Service
Tasks:
- Move `ValidateCurrentPageAsync` + `GetFirstInvalidVisiblePageIndexAsync` logic into `KycValidationService`.
- Single enumeration per page validation (no duplication).
- Return `PageValidationResult { bool IsValid; IReadOnlyList<FieldError> Errors; }`.
Acceptance: Navigation gating unchanged; code duplication removed.

Phase 5: Navigation State Service & Flow Enum
Tasks:
- Introduce `KycNavigationStateService` implementing initial snapshot derivation (pending, rejected, summary resume).
- Replace `ShouldViewSummary` & `ShouldReturnToSummary` + anchor index with snapshot.
- Adapt Next/Previous commands to use service transitions.
Acceptance: Behavioral parity; fewer booleans in VM.

Phase 6: Summary & Invalidation Consolidation
Tasks:
- Extract `GenerateSummaryCollection`, invalid claim/photo expansion, reason mapping into `KycSummaryService` + helper `KycInvalidationContext`.
- ViewModel only assigns observable collections from `KycSummaryModel`.
Acceptance: Summary visuals identical; invalid highlighting intact.

Phase 7: Reference Persistence Service
Tasks:
- Move `UpdateReference`, `SaveReferenceToStorageAsync`, rejection application to `KycReferenceService`.
- Introduce debounced save (configurable) + explicit `FlushAsync()`.
- Provide `ApplyRejectionAsync` integrating invalidation context.
Acceptance: Autosave still effective; fewer direct saves in VM.

Phase 8: Image Processing Service
Tasks:
- Extract `CompressImage`, orientation to `ImageProcessingService` (async API: `CompressAsync(Stream, profile, ct)`).
- Add max input size guard & safe failure fallback (skip + log once).
Acceptance: Attachments still produced; VM has no SkiaSharp code.

Phase 9: Peer Review Adapter
Tasks:
- Move application attributes + featured provider load + petition send into `PeerReviewService`.
- Provide simple methods & optional events.
Acceptance: Peer review UX unchanged.

Phase 10: Command & State Simplification
Tasks:
- Refactor navigation/apply/revoke/summary entry into minimal state machine driver.
- Collapse branching in former `ExecuteNextAsync`.
Acceptance: Logic simpler; no regression in navigation tests.

Phase 11: Performance & Allocation Optimization
Tasks:
- Profile (page change, validation, mapping, summary build).
- Replace hot LINQ with indexed loops if measurable benefit.
- Cache visible page list per transition (invalidate on visibility change event).
- Optional: Reuse buffers via `ArrayPool` for transient property lists.
Acceptance: No behavior change; reduced GC allocations in hotspots.

Phase 12: Final Polish & Documentation
Tasks:
- XML docs for public interfaces & ViewModel exposed members.
- Remove obsolete TODOs, dead private helpers superseded by services.
- Ensure naming/style per repo standard (explicit locals, PascalCase).
- Update README / developer notes for new architecture.
Acceptance: Clean build, zero warnings (new), plan retired.

Cross-Cutting Concerns
- Threading: services return plain results; ViewModel applies to UI thread.
- Cancellation: mapping (for large forms), image compression, peer review fetch support `CancellationToken`.
- Logging: all service failures surface structured events (category: KYC). Silent swallow avoided.
- Error UX: central helper for alert display (Phase 10 or 12) consolidates messages.

Test Targets (Post Phase 6 Minimum)
1. Mapper: grouped date & company representative birth date composition; transform ordering.
2. Validation: required + async personal number rule interplay.
3. Navigation: pending resume, rejected summary return, edit-from-summary cycle.
4. Summary: invalid claim highlighting & reason propagation.
5. Reference: rejection application stores + expands grouped claims.
6. Image: orientation & resize idempotence (optional integration).
7. Peer review: petition building happy/failure paths.

Open Items / Decisions
- Grouped date handling: remain in mapper vs dedicated composite field? (Reevaluate Phase 4 after validation extraction.)
- Localized invalidation reasons per claim: implement via reason map → localization key (Phase 12 if needed).
- Pre-compression size enforcement: now Phase 8 (drop large base64 early).

Rollback Strategy
- Each phase in its own branch.
- Retain old method stubs delegating to services for at least one phase before removal if high-risk.
- If regression: revert last phase; reapply with finer-grained commits.

KPIs After Completion
- ViewModel LOC reduced (target < 50% of original).
- 0 duplicated loops for validation/mapping.
- Command method cyclomatic complexity < 10.
- All pure services testable without MAUI types.
- No direct SkiaSharp references in ViewModel.

End State Summary
A lean `KycProcessViewModel` orchestrating:
- Mapping (IKycFieldMapper)
- Validation (IKycValidationService)
- Navigation (IKycNavigationStateService)
- Summary (IKycSummaryService)
- Reference persistence & rejection (IKycReferenceService)
- Peer review (IPeerReviewService)
- Image processing (IImageProcessingService)
- Thread dispatch (IMainThreadDispatcher)

Result: High cohesion, improved readability, lower regression risk, and clear extension points for future KYC feature evolution.
