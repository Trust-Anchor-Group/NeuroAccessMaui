# KYC Flow Refactor & Layering Plan

## Executive Summary
The current `KycProcessViewModel` mixes UI binding, navigation state machine logic, summary/materialization logic, submission orchestration, validation coordination, and persistence triggering. The existing `KycService` covers persistence, snapshotting, single-page validation, property/attachment extraction, and submission state mutations, but does **not** own flow orchestration.

This plan introduces a dedicated orchestration layer (`IKycFlowController`) plus small focused services to progressively extract domain logic out of the ViewModel while keeping behavior stable and testable. Refactor is incremental, with measurable checkpoints and rollback options.

---
## Current vs Target Responsibility Split

| Concern | Current Owner | Target Owner |
|---------|---------------|--------------|
| Load & persist `KycReference` | KycService | KycService |
| Page validation (single page) | KycService | KycService |
| Whole-process invalid page detection | ViewModel | FlowController |
| Navigation (Next/Previous/Summary) | ViewModel | FlowController |
| Editing-from-summary state | ViewModel | FlowController |
| Progress calculation | ViewModel | FlowController |
| Snapshot scheduling (trigger points) | ViewModel | FlowController (delegates to service) |
| Prefill from identity profile | ViewModel | PrefillService (invoked by FlowController) |
| Build mapped properties + attachments | KycService (+ augmentation in ViewModel) | KycService (+ SummaryBuilder for assembly) |
| Add standard properties (Jid/Phone/etc.) | ViewModel | SummaryBuilder / configurable enrichment |
| Summary model assembly | ViewModel | SummaryBuilder |
| Submission / revoke / rejection orchestration | ViewModel | FlowController (delegates to KycService) |
| UI binding exposure | ViewModel | ViewModel (thin adapter) |

---
## Target Conceptual Architecture
```
+------------------------+          +------------------+
|        View (XAML)     |  binds   |  KycProcessVM    |
+-----------+------------+          +---------+--------+
            | Commands / Events                |
            v                                   v
      +-----------+                 +----------------------+
      |  IKycFlow |  uses           |  KycService          |
      | Controller|---------------> |  (Persistence,       |
      +------+----+                 |   Snapshots,         |
             |                      |   Page Validation,   |
   uses      |                      |   Property/Attach)   |
             v                      +----------+-----------+
   +------------------+                        |
   | SummaryBuilder    |<----------------------+ (raw data)
   +------------------+                        |
             ^                                 v
   +------------------+              +------------------+
   | PrefillService    |  (optional) |  Other Infra     |
   +------------------+              |  (Xmpp, Profile) |
                                     +------------------+
```

---
## Guiding Principles
1. **Behavioral parity each phase** (no functional regressions).
2. **Add, migrate, then delete** (never move & delete simultaneously).
3. **Interfaces first, then implementation** (supports parallel development & test harnessing).
4. **Minimal churn per phase** to ease code review & rollback.
5. **Explicit metrics** (LOC reduction in VM, test harness coverage of controller transitions, snapshot integrity).

---
## Phased Plan

### Phase 0 – Baseline Capture
**Goals:** Establish current behavioral snapshot & metrics.
**Tasks:**
- Measure `KycProcessViewModel` LOC & cyclomatic complexity (manual note acceptable).
- List all public/observable members used by XAML.
- Capture logs from a full successful KYC flow (apply path + rejection path if possible).
**Deliverables:** Baseline notes appended to bottom of this file.
**Success Criteria:** Documented baseline; no code changes.

### Phase 1 – Define New Interfaces (No Behavior Change)
**Goals:** Introduce contracts without wiring.
**Tasks:**
- Add `IKycFlowController` interface (navigation + state exposure + submission/revoke skeleton).
- Add `IKycSummaryBuilder` (takes process/reference -> summary model, returns enriched properties + attachments + summary model).
- Add `IKycPrefillService` (process + identity -> mutated field list / result object).
- Provide empty/skeletal implementations returning NotImplementedException where appropriate (or passthrough stubs returning safe defaults).
**Deliverables:** New interfaces + stub classes in `Services/Kyc/Flow/` or similar.
**Success Criteria:** Build succeeds; ViewModel unchanged.
**Rollback:** Delete new files.

### Phase 2 – Extract Summary Building
**Goals:** Move `BuildMappedValuesAsync` logic out of ViewModel.
**Tasks:**
- Implement `KycSummaryBuilder` using existing `KycService.PreparePropertiesAndAttachmentsAsync` then enrichment.
- Move enrichment (Jid/Phone/Email/Country/DeviceId) into builder.
- ViewModel now calls `summaryBuilder.BuildAsync(...)` instead of local method; remove local method & property augmentation duplication.
**Deliverables:** Working summary builder with identical output verified by diffing property sets (log hash before & after).
**Success Criteria:** Application flows unchanged; summary displays identically (manual spot check).
**Rollback:** Revert builder wiring.

### Phase 3 – Introduce FlowController (Navigation Only)
**Goals:** Move navigation & progress logic.
**Tasks:**
- Implement initial `KycFlowController` with: Initialize, Next, Previous, GoToSummary, GoToMapping, properties: CurrentPage, IsInSummary, Progress.
- Internally reuse existing `KycTransitions` and replicate logic from ViewModel.
- Raise events: `NavigationChanged`, `CurrentPageChanged`, `ProgressChanged`.
- ViewModel subscribes to events and updates bindings (removing internal navigation logic gradually).
**Deliverables:** Controller integrated; ViewModel delegates for next/previous/summary.
**Success Criteria:** All navigation actions work; snapshot scheduling still occurs (temporarily still in VM or partially in controller).
**Rollback:** Switch command handlers back to original VM methods; keep controller unused.

### Phase 4 – Migrate Snapshot Scheduling & Progress Computation
**Goals:** Centralize state mutation triggers.
**Tasks:**
- Move calls to `ScheduleSnapshotAsync` & `FlushSnapshotAsync` into controller transitions.
- Controller exposes Progress property & raises change event; remove progress calculation from VM.
- VM reduces to reading controller.Progress only.
**Deliverables:** Slimmed progress logic in VM.
**Success Criteria:** Snapshots still persist (verify by examining storage / metrics if available). UI progress unchanged.
**Rollback:** Restore snapshot calls in VM.

### Phase 5 – Move Submission / Revoke / Rejection Handling
**Goals:** Centralize apply/revoke logic.
**Tasks:**
- Implement `ApplyAsync`, `RevokeAsync`, `ApplyRejectionAsync` in controller (invoke `KycService` methods + summary rebuild + navigation update + events).
- VM commands delegate directly.
- Remove duplication of editing-from-summary state toggling; maintain invariant inside controller.
**Deliverables:** Controller fully responsible for application lifecycle.
**Success Criteria:** Apply/Revoke/Rejection scenarios still behave identically (manual test with log comparison if possible).
**Rollback:** Revert delegation changes.

### Phase 6 – Prefill Extraction
**Goals:** Move `PrefillFieldsFromProfile` logic.
**Tasks:**
- Implement `PrefillService.PrefillAsync(KycProcess, Profile)` returning results (count mutated, maybe list of field IDs).
- Controller invokes during Initialize if conditions met (not pending/rejected).
- Remove method from ViewModel.
**Deliverables:** PrefillService plus integration; added logging for mutated fields.
**Success Criteria:** Fields still auto-populate; no regression.
**Rollback:** Reintroduce original method in VM.

### Phase 7 – Clean ViewModel
**Goals:** Reduce ViewModel to adapter.
**Tasks:**
- Remove now-redundant private helpers: navigation, summary rebuild, progress calc, apply/revoke logic.
- Replace direct property sources with controller-backed or event-driven updates.
- Ensure naming + XML doc updates reflecting new boundaries.
**Deliverables:** Lean ViewModel (~50–60% LOC reduction expected).
**Success Criteria:** Build passes; manual smoke test passes.
**Rollback:** Git revert this phase.

### Phase 8 – Logging, Metrics & Diagnostics
**Goals:** Enhance observability for new layer.
**Tasks:**
- Structured logs: navigation transitions, summary build hash, submission lifecycle, prefill results.
- Expose controller metrics: navigation steps, snapshots scheduled vs flushed.
- Add optional `IKycFlowObserver` hook for test instrumentation.
**Deliverables:** Extra logging + metrics retrieval method.
**Success Criteria:** Logs show single-source navigation decisions; no duplication.
**Rollback:** Remove added logging calls.

### Phase 9 – Optional Performance / UX Refinements
**Goals:** Optimize responsiveness & resource use.
**Potential Improvements:**
- Coalesce redundant summary rebuilds (debounce inside builder).
- Lazy image compression (parallel vs sequential) if bottleneck.
- Fine-grained snapshot suppression when no field changes.
**Deliverables:** Micro-optimizations with profiling notes.
**Success Criteria:** Reduced snapshot frequency & UI thread latency (qualitative unless profiler integrated later).
**Rollback:** Revert performance patches.

### Phase 10 – Final Consolidation & Documentation
**Goals:** Finish with clarity and maintainability.
**Tasks:**
- Update README / internal docs (architecture section).
- Update `.github/copilot-instructions.md` if conventions adjusted.
- Add sequence diagrams (optional) for navigation & submission flows.
- Mark deprecated internal methods (if any remained) or remove them.
**Deliverables:** Updated documentation & final Plan status.
**Success Criteria:** No dead code; consistent comments; onboarding time reduced.
**Rollback:** Not typically required (doc-only + cleanup).

---
## Risks & Mitigations
| Risk | Impact | Mitigation |
|------|--------|-----------|
| Incremental divergence (VM & controller both manipulating state) | Inconsistent UI state | Hard cutover per phase; feature flags (internal boolean) temporarily if needed |
| Snapshot regression | Data loss on resume | Add temporary log hash before & after each persisted snapshot during phases 3–5 |
| Event ordering race (UI vs controller) | Flickering UI / stale bindings | Always marshal controller events to main thread in VM subscription |
| Hidden coupling to existing private methods | Build errors late | Phase 1 interface drafting with references search |
| Scope creep (extra validation features) | Delays | Strictly defer new features to post Phase 7 |

---
## Success Metrics
| Metric | Baseline | Target |
|--------|----------|--------|
| KycProcessViewModel LOC | (capture in Phase 0) | -50% or better |
| Public methods in ViewModel performing domain mutations | >10 | ≤3 (delegations only) |
| Snapshot duplicates (stale vs persisted) | Current log ratio | Reduce by ≥15% (optional) |
| Navigation logic unit test coverage | 0% | ≥70% in controller (stretch) |
| Time to add new navigation step (est.) | High | Reduced (qualitative developer feedback) |

---
## Tooling / Testing Approach
- Add lightweight unit tests (if allowed later) targeting controller transitions: advance, back, rejection, re-entry to summary.
- Use temporary logging of: navigation state tuples `(State, CurrentPageIndex, AnchorPageIndex)`.
- Hash comparison of mapped property sets pre/post refactor (SHA256 of concatenated key=value lines) to assert parity.

---
## Rollback Strategy
Each phase ends with a stable build; rollback = git revert of that phase’s commit. Avoid multi-phase squashes until after Phase 7 completion.

---
## Tentative Timeline (Adjust as Needed)
| Phase | Effort (est.) | Notes |
|-------|---------------|-------|
| 0 | 0.5 day | Baseline only |
| 1 | 0.5 day | Interfaces + stubs |
| 2 | 1 day | Summary extraction |
| 3 | 1.5 days | Navigation controller + events |
| 4 | 0.5 day | Snapshot & progress migration |
| 5 | 1 day | Submission/revoke/rejection |
| 6 | 0.5 day | Prefill service |
| 7 | 0.5 day | ViewModel slimming |
| 8 | 0.5 day | Logging/metrics |
| 9 | (optional) | Performance passes |
| 10 | 0.5 day | Documentation |

---
## Open Questions (Resolve Before Phase 3)
1. Should `KycService` expose a bulk validation (first invalid page) to reduce controller logic even further? (Currently in service already via `GetFirstInvalidVisiblePageIndexAsync`.)
2. Do we persist summary model variants (e.g., fast rebuild vs full rebuild) or always recompute?
3. Should enrichment (Jid/Phone/Email/Country/DeviceId) be feature-flagged for future dynamic control?
4. Should snapshot scheduling frequency be policy-driven (e.g., min interval)?

---
## Appendices
### Appendix A – Initial Interface Skeletons (Illustrative Only)
```csharp
public interface IKycFlowController : IDisposable
{
	KycReference Reference { get; }
	KycProcess Process { get; }
	KycNavigationSnapshot Navigation { get; }
	KycPage? CurrentPage { get; }
	double Progress { get; }
	bool IsInSummary { get; }
	bool IsEditingFromSummary { get; }
	IReadOnlyList<Property> CurrentMappedProperties { get; }
	IReadOnlyList<LegalIdentityAttachment> CurrentAttachments { get; }
	KycSummaryModel? CurrentSummary { get; }
	Task InitializeAsync(KycReference reference, string? language, CancellationToken ct = default);
	Task<bool> NextAsync(CancellationToken ct = default);
	Task<bool> PreviousAsync(CancellationToken ct = default);
	Task<bool> GoToSummaryAsync(CancellationToken ct = default);
	Task<bool> GoToMappingAsync(string mappingKey, CancellationToken ct = default);
	Task<bool> ApplyAsync(CancellationToken ct = default);
	Task<bool> RevokeAsync(CancellationToken ct = default);
	Task<bool> ApplyRejectionAsync(string message, string[] invalidClaims, string[] invalidPhotos, string? code, CancellationToken ct = default);
	event EventHandler? NavigationChanged;
	event EventHandler? CurrentPageChanged;
	event EventHandler? ProgressChanged;
	event EventHandler? SummaryChanged;
}
```

### Appendix B – Snapshot Comparison Hash
```
Hash = SHA256( sorted( FieldId + "=" + (Value ?? "") + "\n" ) )
```
Used to confirm no unintended field value changes across phases.

---
### Baseline Notes (Phase 0 – To Fill In)
- KycProcessViewModel LOC: _(fill)_
- Distinct responsibilities enumerated: _(fill)_
- Average snapshot frequency during nominal flow: _(fill)_
- Initial property hash for sample application: _(fill)_

---
**End of Plan**
