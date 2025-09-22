# KYC Data Consistency & Concurrency Remediation Plan

This document outlines a phased implementation strategy to eliminate race conditions and data inconsistency in the KYC application flow (`KycService`, navigation commands, autosave, persistence of `KycReference`).

---
## Overview of Core Problems
- Unsynchronized mutations of `KycReference` and associated `KycProcess.Values` across UI thread, autosave background task, and submission/rejection flows.
- Autosave persisting *live mutable* objects leading to stale overwrite (out-of-order completion).
- No optimistic concurrency or versioning — last writer wins silently.
- Navigation commands (e.g., rapid "Next") allow overlapping validation + snapshot + autosave scheduling.
- Snapshot creation is not atomic: partially updated aggregates may be saved.
- Null-ing arrays (`Fields = null`) while other operations enumerate them.
- Dispose does not guarantee final persistence.

---
## Goals
1. Deterministic, ordered persistence (no stale overwrites).
2. Atomic snapshotting of UI/process state into an immutable structure before background work.
3. Eliminate concurrent mutation races (serialize writes, or confine all writes to one logical executor).
4. Introduce optimistic concurrency (Version) to guard against out-of-process or concurrent logical updates.
5. Make navigation commands reentrancy-safe (no overlapping transitions).
6. Provide structured logging + metrics hooks for visibility.
7. Maintain backward compatibility with existing storage data (migration-friendly).

---
## Phase 0 – Preparation & Metrics (Optional but Recommended)
**Purpose:** Gain baseline and ensure safe rollout.
- Instrument current `SaveReferenceAsync` with temporary structured log: ReferenceId, UpdatedUtc, ThreadId, Hash(Fields), CallSite.
- Duration: 0.5 day.

---
## Phase 1 – Add Versioning & Immutable Snapshot DTO
**Files Impacted:**
- `NeuroAccessMaui/Services/Kyc/KycReference.cs`
- `NeuroAccessMaui/Services/Kyc/KycService.cs`

**Changes:**
1. Add `public int Version { get; set; }` (default 0) to `KycReference` with persistence attribute if required.
2. Create internal immutable `KycReferenceSnapshot` (new file) containing:
   - ReferenceId
   - Version
   - Fields (array copy)
   - Progress
   - LastVisitedPageId / Mode
   - Rejection / Submission fields
   - Timestamps (CreatedUtc, UpdatedUtc)
3. In `KycService.UpdateSnapshot` replace in-place mutation with creation of a `KycReferenceSnapshot` returned to caller.
4. Increment `Version` only when a new snapshot is captured (pre-persistence) under synchronization.
5. Modify autosave logic to accept a snapshot object instead of the mutable `KycReference`.
6. `SaveReferenceAsync(snapshot)` loads current reference, compares Version; if snapshot.Version < stored.Version => skip (stale). If equal => apply and increment Version (or increment pre-save, CAS style depending on persistence API constraints).

**Concerns & Mitigations:**
- Persistence layer may not support atomic CAS. If not, add a lightweight retry: load -> compare -> update -> if conflict (Version mismatch) -> abort.
- Existing records have no Version; treat missing as 0.

**Acceptance Criteria:**
- Saving a stale snapshot (older Version) is skipped (verified via logging).
- Normal flow increments Version each user action leading to snapshot.

---
## Phase 2 – Introduce Per-Reference Async Lock & Serialization of Mutations
**Files Impacted:**
- `KycService.cs`
- New helper: `AsyncLock` (if not already present) under `Services/Resilience` or `Services/Concurrency`.

**Changes:**
1. Add `private readonly ConcurrentDictionary<string, AsyncLock> referenceLocks;` in `KycService`.
2. Wrap all mutation entry points:
   - `ScheduleSnapshotAsync`
   - `FlushSnapshotAsync`
   - `ApplySubmissionAsync`
   - `ClearSubmissionAsync`
   - `ApplyRejectionAsync`
   - `PrepareReferenceForNewApplicationAsync`
3. Inside lock: build snapshot & enqueue persistence (autosave or immediate) using immutable DTO only.
4. Remove direct field mutation outside locked regions (except read-only access).

**Concerns:**
- Deadlocks risk minimal if lock is only awaited in top-level public methods and not re-entered. Document usage.

**Acceptance Criteria:**
- Rapid spam of navigation produces strictly increasing Versions with no duplicates skipped incorrectly.

---
## Phase 3 – Refactor Autosave to Snapshot Queue (Channel)
**Files Impacted:**
- `KycService.cs` (replace `ObservableTask<int> autosaveTask` usage)
- Add new file `KycAutosaveQueue.cs`

**Changes:**
1. Replace debounce + single task with a `Channel<KycReferenceSnapshot>` or custom serial queue.
2. On schedule: try `TryWrite(snapshot)`; if channel already has a pending snapshot, drop older (keep last) to avoid redundant saves.
3. Background consumer executes: dequeues latest snapshot, persists, logs result. Cancellation token controlled by service lifecycle.
4. Flush operation: force immediate persist by writing and awaiting completion TaskCompletionSource.

**Concerns:**
- Ensure graceful shutdown: on `Dispose`, complete channel then drain remaining item(s) before return.

**Acceptance Criteria:**
- At most one outstanding persistence operation at a time.
- Stress test (manual) shows no stale overwrites in logs.

---
## Phase 4 – Navigation Command Reentrancy Guard
**Files Impacted:**
- KYC UI ViewModels (e.g., `KycProcessPageViewModel` or equivalent command definitions — not shown; identify actual file names).

**Changes:**
1. Replace `ICommand` with `AsyncRelayCommand` (CommunityToolkit) configured with `AllowsMultipleExecutions = false`
2. Disable Next/Previous buttons while validation + snapshot flush is running.
3. Centralize navigation transition pipeline: Validate -> Snapshot -> Navigate; ensure executed under reference lock.

**Concerns:**
- UX: Provide subtle loading indicator for disabled period.

**Acceptance Criteria:**
- Double-tap Next does not trigger two overlapping transitions (verified by logs showing single Version increment).

---
## Phase 5 – Final Flush & Disposal Guarantees
Status: IMPLEMENTED (Service + lifecycle hook integrated in `App.ShutdownAsync`).

**Files Impacted:**
- `KycService.cs` (added `FlushAsync` + `ShutdownAsync`)
- `IKycService.cs` (new interface members)

**Implemented Changes:**
1. `Task FlushAsync(CancellationToken)` drains any pending coalesced snapshots immediately.
2. `Task ShutdownAsync(CancellationToken)` cancels worker, completes channel, awaits worker (bounded wait) then calls `FlushAsync`.
3. Dispose still performs a best-effort cancellation; explicit host code should prefer calling `ShutdownAsync` for deterministic persistence.

**Remaining Integration Follow-ups (Optional Enhancements):**
- Add a structured log event: `KycShutdown` {PendingCountBeforeFlush}.

**Acceptance Criteria (Met):**
- Manual invocation of `FlushAsync` after rapid edits leaves zero entries in the pending dictionary.
- `ShutdownAsync` (invoked from `App.ShutdownAsync`) returns within bounded time (<2s wait for worker) even under load.
- App backgrounding (`OnSleep`) triggers bounded KYC shutdown/flush (2s timeout) before storage unload.

---
## Phase 6 – Telemetry & Health
**Files Impacted:**
- Logging central service.
- `KycService.cs`.

**Changes:**
1. Structured log event: `KycSnapshotPersisted` { ReferenceId, Version, FieldCount, DurationMs, Hash }.
2. Structured log event: `KycSnapshotStaleSkipped` { ReferenceId, SnapshotVersion, CurrentVersion }.
3. Add lightweight in-memory counters for saved vs skipped; expose via diagnostics page or debug log command.

**Acceptance Criteria:**
- Logs visible and actionable; ratio of skipped stale snapshots acceptable (<5% under normal usage).

---
## Phase 7 – Clean-Up & Hardening
**Changes:**
- Enforce non-null `Fields` (empty array instead of null). Migration: on load, if null -> set to empty.
- Add guard clauses & early returns consolidated to reduce mutation regions.
- Add retry policy around persistence (exponential backoff 2–3 attempts on transient exceptions).

---
## Data Model Change Summary
| Change | Backward Compatibility | Migration Step |
|--------|------------------------|----------------|
| Add `Version` (int) to `KycReference` | Existing rows get default 0 | None required; treat missing as 0 |
| Enforce non-null `Fields` | Safe | On load: if null -> [] |

---
## Risk Register
| Risk | Impact | Mitigation |
|------|--------|------------|
| CAS race if storage not transactional | Stale writes | Retry pattern + version compare |
| Performance regression from locking | UI delay | Narrow critical sections, offload IO |
| Channel backlog growth | Memory | Drop intermediate snapshots (keep latest) |
| Missed final save on abrupt kill | Lost last seconds of edits | Short autosave interval + explicit flush on lifecycle |

---
## Acceptance Test Scenarios (Manual)
1. Rapid Next tapping (10x in <2s): Versions strictly monotonic, no duplicates, final state correct.
2. Enter data then quickly navigate away & background app: Last entry persisted (after resume).
3. Simultaneous rejection application while autosave pending: Rejection fields not overwritten by stale autosave.
4. Prepare new application while previous autosave mid-flight: Old snapshot skipped (Version mismatch).
5. Image upload + navigate quickly: Attachment appears in final identity submission once; no partial duplicates.

---
## Implementation Order Justification
Ordering reduces refactor risk: introduce passive safety (versioning + snapshots) before changing execution model (queue), then guard UI reentrancy, then finalize lifecycle and telemetry.

---
## Next Steps
- Proceed directly with Phase 1 implementation (`feature/kyc-concurrency-phase1`).
- After each phase, run manual scenarios before continuing.

---
## Addendum: ObservableTask & PolicyRunner Integration

### Current Usage Summary
- `ObservableTask<int> autosaveTask` in `KycService` handles debounced autosave with a debounce policy.
- Field validation (`ObservableKycField.ValidationTask` and synchronous debounce task) relies on `ObservableTask` for stateful validation and UI binding.
- Page refresh logic (in `KycProcessViewModel`) also uses `ObservableTask`.
- `PolicyRunner` composes `IAsyncPolicy` pipelines (currently debounce, potentially retries/timeouts) around actions.

### Impact on Phases
| Phase | Adjustment Needed | Notes |
|-------|-------------------|-------|
| 1 | Keep `autosaveTask`, but persist immutable snapshot instead of live reference. | Introduce `KycReferenceSnapshot`; store latest snapshot in service. |
| 2 | Wrap snapshot creation & `pendingSnapshot` assignment in per-reference lock. | Validation `ObservableTask`s unchanged. |
| 3 | Remove `autosaveTask`; replace with channel/queue. | Complete migration—no feature flag fallback. |
| 4+ | No change to validation tasks. | Consider minor telemetry additions. |

### Transitional Strategy
1. Phase 1 retrofit: Modify `ExecuteAutosaveAsync` to use `latestSnapshot` (volatile) and skip if stale.
2. Add `long latestVersionPersisted` to detect & log skipped snapshots.
3. When moving to Phase 3 queue, delete debounce policy usage; if a coalescing delay still desired, implement a timer in producer before enqueueing (without mutating the reference further).

### PolicyRunner Reuse
- Retain `PolicyRunner` for: persistence retry/backoff, timeout around storage operations, optional jitter.
- Remove its usage for pure debouncing once queue introduced (debounce replaced by snapshot coalescing + short delay).

### Removal Checklist (Phase 3)
- Delete `AutosaveDelay`, `AutosavePolicy`, and `autosaveTask` fields.
- Remove `FlushPendingAutosaveAsync` and `RunImmediateAutosaveAsync`; replace with `FlushQueueAsync`.
- Update Dispose to shutdown queue and await drain.

### Validation Tasks Rationale to Keep
- Provide per-field UI state (IsValid, pending validation) and avoid blocking UI thread with synchronous heavy validation.
- Their concurrency is localized to field scope; no cross-field mutation races if snapshot capture occurs after `ValidationTask.WaitAllAsync()` in navigation pipeline.

### Additional Logging Events
- `KycSnapshotCreated` { ReferenceId, Version, FieldCount }
- `KycSnapshotPersistAttempt` { Version, IsImmediate }
- `KycSnapshotPersistSkipped` { SnapshotVersion, CurrentVersion }

### Risks If Ignored
| Risk | Consequence |
|------|-------------|
| Leaving autosave ObservableTask after introducing queue | Duplicate persistence paths, undefined ordering |
| Not switching to immutable snapshots early | Persisting mid-mutation state |
| Removing debounce without coalescing | Excess disk writes under rapid field edits |
