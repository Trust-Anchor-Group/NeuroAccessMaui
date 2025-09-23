# KYC Flow Refactor & Layering Plan (Simplified Revision)

## Executive Summary
The current `KycProcessViewModel` mixes UI binding, navigation state machine logic, summary/materialization logic, submission orchestration, validation coordination, and persistence triggering. The existing `KycService` covers persistence, snapshotting, single-page validation, property/attachment extraction, and submission state mutations, but does **not** own flow orchestration.

Original plan proposed several new service abstractions (FlowController, SummaryBuilder, PrefillService, etc.). After review, we simplify to a **single new orchestration class** (`KycFlowController`, concrete first – interface later only if needed). We reuse existing `KycService` and `IdentitySummaryFormatter`, adding only tiny internal helpers (e.g., enrichment, hashing). Goal: Slim the ViewModel without service proliferation.

---
## Current vs Target Responsibility Split

| Concern | Current Owner | Simplified Target Owner |
|---------|---------------|-------------------------|
| Load & persist `KycReference` | KycService | KycService |
| Page validation (single page & first invalid) | KycService | KycService |
| Whole-process invalid page detection | ViewModel | KycFlowController (queries KycService) |
| Navigation (Next/Previous/Summary) | ViewModel | KycFlowController |
| Editing-from-summary state | ViewModel | KycFlowController |
| Progress calculation | ViewModel | KycFlowController |
| Snapshot scheduling (trigger points) | ViewModel | KycFlowController (delegates to KycService) |
| Prefill from identity profile | ViewModel | KycFlowController (private helper; may call KycService) |
| Build mapped properties + attachments | KycService + VM enrichment | KycService + internal enrichment helper |
| Add standard properties (Jid/Phone/etc.) | ViewModel | Internal enrichment helper |
| Summary model assembly | ViewModel | IdentitySummaryFormatter (+ enrichment helper) |
| Submission / revoke / rejection orchestration | ViewModel | KycFlowController (delegates to KycService) |
| UI binding exposure | ViewModel | ViewModel (thin adapter) |

---
## Target Conceptual Architecture (Simplified)
```
+------------------------+          +------------------+
|        View (XAML)     |  binds   |  KycProcessVM    |
+-----------+------------+          +---------+--------+
		| Commands / Events                |
		v                                   v (state projection)
	+------------------+        uses      +-----------------------+
	|  KycFlowController|---------------->|  KycService           |
	|  (Orchestration)  |                 |  (Persistence,        |
	+---------+---------+                 |   Validation,         |
		    |                           |   Properties/Attach)  |
	 internal helpers                     +-----------+-----------+
		    |                                       |
		    v                                       v
     IdentitySummaryFormatter                Profile/Other Infra
     + Enrichment Helper (static)            (Xmpp, Device, etc.)
```

Key Simplifications:
1. Only one new class (`KycFlowController`). No new interfaces until a second implementation or external consumer appears.
2. Summary built via existing `IdentitySummaryFormatter` plus a small internal static `SummaryEnrichmentHelper` (adds standard properties & attachments).
3. Prefill logic remains a private method inside the controller (can later be extracted if complexity grows).
4. A single immutable `FlowState` snapshot is emitted through one `StateChanged` event instead of multiple granular events.
5. Snapshot policy lives solely inside the controller (hash + interval heuristic) delegating actual persistence to `KycService`.

---
## Guiding Principles (Revised)
1. Maintain behavioral parity at each step (no regressions).
2. Introduce only the minimum new abstraction (controller) now; defer interfaces (YAGNI).
3. Use immutable `FlowState` and pure transition helpers for testability.
4. Merge related moves to reduce phase overhead while preserving rollback points.
5. Track explicit metrics: ViewModel LOC, number of domain-mutating methods, state change frequency, snapshot duplication.

---
## Condensed Phases (0–6)

### Phase 0 – Baseline Capture (unchanged)
Goals: Record current VM LOC, cyclomatic complexity (manual), public bindable members, end-to-end flow logs (apply + rejection).
Deliverable: Baseline notes appended below.
Rollback: N/A.

### Phase 1 – Introduce `KycFlowController` (Navigation + Progress)
Scope: Build concrete controller (no interface yet). Implements Initialize, Next, Previous, GoToSummary, GoToMapping. Emits immutable `FlowState` via single `StateChanged` event. Progress derived from page index & total valid pages.
VM Impact: Commands delegate; retains old submission, summary, snapshot logic for now.
Rollback: Remove new controller file; restore original command wiring.

### Phase 2 – Move Summary Assembly
Scope: Relocate summary building & property/attachment enrichment from VM into controller using `KycService.PreparePropertiesAndAttachmentsAsync` + `IdentitySummaryFormatter` + `SummaryEnrichmentHelper` (internal static).
VM Impact: Removes `BuildMappedValuesAsync` (or equivalent) and related enrichment code; now listens for state changes carrying summary.
Parity Check: Log hash of properties before/after (SHA256 scheme in Appendix B).
Rollback: Revert controller summary changes; restore VM method.

### Phase 3 – Submission, Revoke, Rejection, Snapshots, Prefill
Scope: Move Apply/Revoke/Rejection orchestration, snapshot scheduling policy, and prefill logic (private helper) into controller transitions.
Policy: On any state-changing action, if field hash changed and interval exceeded, schedule snapshot; on terminal (Apply/Revoke) flush snapshot.
VM Impact: Command methods become thin delegates; remove snapshot/prefill code.
Rollback: Revert controller changes and restore removed VM methods.

### Phase 4 – Prune ViewModel
Scope: Delete obsolete helpers (navigation, progress, summary, submission, prefill, snapshot). Retain only bindable properties & command wiring projecting latest `FlowState`.
Goal: ≥50% LOC reduction; 0 domain mutation methods.
Rollback: Git revert of this phase commit.

### Phase 5 – Logging & Diagnostics
Scope: Add structured logs for transitions (OldState -> NewState + reason), summary hash, submission lifecycle, snapshot decisions, prefill results. Add optional lightweight metrics accessor (counts only). Consider `IKycFlowObserver` only if tests or external instrumentation immediately need it.
Rollback: Remove logging additions.

### Phase 6 – Documentation & Cleanup
Scope: Update this Plan section to Completed, README architecture snippet, inline XML docs for controller public API, remove dead code remnants.
Rollback: Not typically necessary.

Optional Future (Defer Until Need Evident): Performance tuning (debounce summary), interface extraction for DI/mocking, observer hooks.

---
## Risks & Mitigations (Revised)
| Risk | Impact | Mitigation |
|------|--------|-----------|
| VM & controller both mutating state during transition phase | Inconsistent UI | Hard switch per phase; never duplicate logic simultaneously |
| Snapshot policy bug | Potential data loss | Hash + timestamp logging around each scheduled/flush action |
| Event flood / UI thrash | Performance degradation | Single consolidated `StateChanged` event; coalesce rapid transitions (optional debounce) |
| Controller bloat over time | Maintainability drop | Partial class split by concern (Navigation, Summary, Submission) without extra abstractions |
| Need for alternative implementations later | Refactor churn | Defer interfaces until a second implementation appears |

---
## Success Metrics (Revised)
| Metric | Baseline | Target |
|--------|----------|--------|
| ViewModel LOC | Capture Phase 0 | ≥50% reduction |
| Domain mutation methods on VM | Count Phase 0 | 0 (only delegates) |
| Average state changes per user action | N/A | 1 (no redundant events) |
| Snapshot duplicate ratio | Capture Phase 0 | -15% (optional improvement) |
| Time-to-add navigation step (qualitative) | High | Clearly reduced per developer feedback |

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
| 1 | 0.75 day | Controller (nav + progress) |
| 2 | 0.75 day | Summary relocation |
| 3 | 1 day | Submission + snapshots + prefill |
| 4 | 0.5 day | ViewModel pruning |
| 5 | 0.5 day | Logging & diagnostics |
| 6 | 0.25 day | Docs & cleanup |
| Future | (optional) | Performance / interfaces if needed |

---
## Open Questions (Resolve Before Phase 1 Complete)
1. Confirm `KycService` already offers first-invalid-page API; if yes, rely on it (avoid duplication).
2. Decide if summary is always recomputed or can be cached (initially always recompute, optimize later).
3. Should enrichment fields be conditionally included (feature flag)? (Defer until a real requirement.)
4. Minimum snapshot interval default (e.g., 5s? 10s?) – pick a sensible default now.
5. Hash algorithm extension need? (SHA256 fine; only change if perf issues arise.)

---
## Appendices
### Appendix A – `FlowState` & Controller Sketch
```csharp
public sealed record FlowState(
	int PageIndex,
	int TotalPages,
	bool IsInSummary,
	bool IsEditingFromSummary,
	double Progress, // 0..1
	KycSummaryModel? Summary,
	IReadOnlyList<Property> Properties,
	IReadOnlyList<LegalIdentityAttachment> Attachments,
	string SubmissionStatus // Draft / Pending / Applied / Rejected etc.
);

public enum NavigationResult
{
	Success,
	AlreadyAtFirst,
	AlreadyAtLast,
	ValidationFailed,
	NotAllowedInState,
	UnknownError
}

public sealed class KycFlowController : IDisposable
{
	public event EventHandler<FlowState>? StateChanged;
	// Public async methods: InitializeAsync, NextAsync, PreviousAsync,
	// GoToSummaryAsync, GoToMappingAsync, ApplyAsync, RevokeAsync, ApplyRejectionAsync.
	// Each returns NavigationResult or bool (for Apply*) and raises StateChanged once on success.
	// Internals maintain current FlowState and compute diffs.
}
```

### Appendix B – Snapshot Comparison Hash
```
Hash = SHA256( sorted( FieldId + "=" + (Value ?? "") + "\n" ) )
```
Used to confirm no unintended field value changes across phases.

### Baseline Notes (Phase 0 – To Fill In)
- KycProcessViewModel LOC: _(fill)_
- Distinct responsibilities enumerated: _(fill)_
- Average snapshot frequency during nominal flow: _(fill)_
- Initial property hash for sample application: _(fill)_

### Appendix C – Summary Enrichment Helper (Concept)
```csharp
internal static class SummaryEnrichmentHelper
{
	public static (IReadOnlyList<Property> Properties, IReadOnlyList<LegalIdentityAttachment> Attachments)
		Enrich(
			IEnumerable<Property> baseProperties,
			IEnumerable<LegalIdentityAttachment> baseAttachments,
			EnrichmentContext context)
	{
		// Add Jid, Phone, Email, Country, DeviceId if not already present.
		// Return new materialized lists (no mutation of inputs).
	}
}

internal sealed record EnrichmentContext(
	string? Jid,
	string? Phone,
	string? Email,
	string? Country,
	string? DeviceId
);
```

**End of Simplified Plan**
