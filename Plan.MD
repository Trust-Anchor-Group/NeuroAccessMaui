# KYC Process Refactoring Plan (Unified Service Revision)

## Goal
Transform `KycProcessViewModel` from a multi‑responsibility monolith into a lean UI orchestrator backed by cohesive, testable domain + service layers that optimize for best practices, maintainability, and extensibility with minimal regression risk.

## Core Design Tenets
1. Behaviors preserved phase‑by‑phase (resume, rejection, summary, submission, peer review).
2. Pure domain logic first (stateless functions & immutable snapshots) → stable foundation for later decomposition.
3. Only introduce an interface if (a) multiple impls plausible, (b) test seam required, or (c) external boundary (storage, imaging). Otherwise start with internal concretes.
4. Single dependency into the ViewModel: interim unified `IKycService` (now contains validation + data preparation) → later replaced by a facade (`IKycProcessEngine`) that also encapsulates summary, navigation, persistence & peer review.
5. Deterministic ordering & idempotent projections (mapping & summary) to keep future testing trivial (tests deferred now).
6. Explicit flow state; no scattered boolean flags. Derive secondary flags (IsEditable, CanSubmit) from state.
7. Cancellation & debouncing standardized (shared operation context) – no ad‑hoc CTS clutter.
8. Fail fast, log structured, surface user‑friendly error classes (Recoverable vs NonRecoverable).
9. Keep mutation localized: domain snapshot in → snapshot out. Side-effects (autosave, compression) isolated.
10. Incremental safety: compile + smoke test each phase; no large unverified jumps.

## Current Responsibility Buckets (Baseline)
1. Initialization & resume logic
2. Navigation & flow flags
3. Validation orchestration (sync + async rules)
4. Mapping & transform pipeline
5. Attachment & image compression
6. Summary projection & invalid highlighting
7. Peer review feature set
8. Reference persistence & rejection application
9. Invalidation augmentation logic
10. Event handling & throttled UI refresh
11. Command orchestration (Next/Previous/Submit/Revoke)
12. Direct platform/service coupling

## Target Architecture (Simplified Iteration Path)
Layers (evolutionary):
1. Domain Model: `KycProcessState`, `KycPageState`, `KycFieldState`, `KycFlowState` (enum). Pure, serializable.
2. Unified Service (current): `IKycService` (load references, validate page, find first invalid, prepare mapped properties + attachments).
3. Upcoming Facade: `IKycProcessEngine` (will wrap `IKycService` plus forthcoming modules: navigation, summary, persistence, peer review, autosave/cancellation coordination).
4. Specialized Modules (introduced only when needed, not pre-created):
	- Navigation (pure transitions reused by engine)
	- Summary + Invalidation
	- Reference Persistence & Rejection
	- Peer Review
5. Cross-cutting helpers: `KycOperationContext`, `KycOrderingComparer` (added when first consumer beyond ViewModel exists).
6. ViewModel: Reduced to UI projection + command wiring; currently calls unified service directly; later only the engine.

## Interface Strategy
Minimize surface now: only `IKycService` publicly consumed. Defer `IKycProcessEngine` until after summary + navigation extraction so the facade adds immediate value. Additional interfaces (`IPeerReviewService`, `IKycReferenceStore`, `IImageProcessingService`) remain speculative and will only emerge with concrete alternate implementations or testing friction.

## Flow State Model
```csharp
enum KycFlowState { Form, Summary, EditingFromSummary, RejectedSummary, PendingSummary }

sealed record KycNavigationSnapshot(int CurrentPageIndex, int AnchorPageIndex, KycFlowState State);
```
Derived flags (e.g. `IsEditable`) computed, not stored.

## Deterministic Ordering Contract
Ordering of mapped properties & summary items: PageIndex ASC → FieldIndex ASC → MappingOrdinal ASC. Single comparer will enforce (implementation deferred until summary extraction).

## Validation Model
Unified pipeline returns: `ValidationResult { bool IsValid; bool PendingAsync; ImmutableArray<FieldError> Errors; }`.
Async rules contribute after awaited; pending state surfaces to UI (`IsValidating`). Debouncing occurs at boundary (user input events) – core validation remains pure/deterministic.

## Autosave & Persistence
Debounced (e.g. 800ms) field changes consolidated; flush on navigation forward, submit, suspend. Persistence API returns success + optional conflict descriptor. Rejection application centralized.

## Attachment & Image Processing
Integrated early (not late) to stabilize memory footprint. Input guard: max stream size, dimension sniff before full decode. Fallback: skip + log once if compression fails → keep original.

## Summary Projection
Pure function: `(KycProcessState, InvalidationContext) -> KycSummaryModel`. Expose optional diff model later (`SummaryDelta`) for incremental UI updates (future optimization; not Phase 1 deliverable).

## Logging & Telemetry
Structured event id pattern: `KYC.{Area}.{Action}` (e.g., `KYC.Validation.RuleFailed`). Correlate with `ProcessId`. No silent catch; classify error as Recoverable/NonRecoverable for UX.

<!-- Metrics & performance KPIs intentionally deferred. We'll retain architectural hooks (deterministic ordering, separation) without committing to measurement or optimization yet. -->

<!-- Smoke scenario list removed for brevity; manual ad-hoc validation will be performed during refactor. -->

## Updated Phases (Unified Path)
### Phase 1 – Baseline & Domain Sketch (DONE)
Annotations + initial domain record scaffolding.

### Phase 2 – Validation + Mapping Consolidation (DONE)
Merged former validation & data preparation into `KycService` and updated interface.

### Phase 3 – Navigation Transition Helpers (DONE)
Introduced pure transition functions (`KycTransitions`) and replaced imperative next/previous & summary flag logic with `KycNavigationSnapshot` + derived flags (`IsInSummary`, `IsEditingFromSummary`). ViewModel now builds `KycProcessState` snapshots for transitions.

### Phase 4 – Summary & Invalidation Extraction (IN PROGRESS)
Created `KycSummary` domain module (`KycSummarySection`, `KycSummaryModel`, `Generate`, `BuildInvalidMappingSet`) and integrated into `BuildMappedValuesAsync`. Legacy `GenerateSummaryCollection`, invalidation builders, and field mutation methods removed and replaced with `KycInvalidation.ApplyInvalidations`. Remaining work: optional ordering comparer + future diff model (deferred).

### Phase 5 – Reference Persistence & Autosave
Extract reference updates (save/revoke/rejection application) + debounced autosave engine. Provide explicit flush and rejection application methods.

### Phase 6 – Peer Review Isolation
Encapsulate reviewer discovery, request building, and QR / featured flows; expose minimal async API.

### Phase 7 – Facade Introduction (`KycProcessEngine`)
Wrap: navigation, validation (delegates to `KycService`), mapping (delegates), summary, persistence, peer review. ViewModel now depends only on facade.

### Phase 8 – Operation Context & Cancellation Unification
Introduce `KycOperationContext` for cancellation, debouncing utilities, correlation id.

### Phase 9 – Documentation & Cleanup
Finalize XML docs, remove deprecated wrappers, update developer docs. (No performance profiling or test suite implementation in this iteration.)

## Cross-Cutting Concerns
Threading: Services return pure results; ViewModel applies on main thread. Cancellation: Provided by engine facade. Error UX: Central helper turns domain error classification into user message. Telemetry: correlation id maintained in engine context.

<!-- Detailed testing focus removed. Goal: keep code structure testable; actual test implementation deferred. -->

## Rollback & Safety
Each phase isolated branch. If regression, revert last phase (git revert) – domain layer purity simplifies diff. Keep old methods delegating to new services for a single phase before removal when high risk.

<!-- KPI exit criteria removed – success now defined as: build compiles, responsibilities reduced, unified service + phase goals achieved. -->

## End State Snapshot
`KycProcessViewModel` depends solely on `IKycProcessEngine`; `KycService` demoted to internal component behind facade. Domain transitions, summary, validation, mapping, persistence, and peer review are modular yet cohesive. Deterministic outputs, minimized interface surface, and clear extension seams.

Outcome: Simplified incremental evolution path with early consolidation (less churn), reduced cognitive load, and a stable foundation for future optimizations (incremental summary diffs, alternative storage, enhanced peer review workflows).
