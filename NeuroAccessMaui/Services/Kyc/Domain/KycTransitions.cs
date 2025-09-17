using System;
using System.Collections.Generic;
using System.Linq;

namespace NeuroAccessMaui.Services.Kyc.Domain
{
	/// <summary>
	/// Pure transition helpers for KYC navigation & flow. No side-effects, deterministic.
	/// </summary>
	internal static class KycTransitions
	{
		/// <summary>
		/// Computes next navigation snapshot when advancing from a form page.
		/// </summary>
		public static KycNavigationSnapshot Advance(KycProcessState state, IReadOnlyList<int> visibleIndices)
		{
			KycNavigationSnapshot nav = state.Navigation;
			if (state.Navigation.State is KycFlowState.Summary or KycFlowState.PendingSummary)
				return nav; // Already at summary; engine will decide apply action.

			// Find next visible page after current
			int current = nav.CurrentPageIndex;
			int next = -1;
			foreach (int idx in visibleIndices)
			{
				if (idx > current) { next = idx; break; }
			}
			if (next < 0)
			{
				// No further pages -> transition to summary
				return nav with { State = state.ApplicationSent ? KycFlowState.PendingSummary : KycFlowState.Summary };
			}
			return nav with { CurrentPageIndex = next };
		}

		/// <summary>
		/// Computes previous navigation snapshot when moving backward.
		/// </summary>
		public static KycNavigationSnapshot Back(KycProcessState state, IReadOnlyList<int> visibleIndices)
		{
			KycNavigationSnapshot nav = state.Navigation;
			if (nav.State == KycFlowState.Summary)
			{
				// Leave summary returning to anchor page
				return nav with { State = KycFlowState.Form, CurrentPageIndex = nav.AnchorPageIndex >= 0 ? nav.AnchorPageIndex : nav.CurrentPageIndex };
			}
			int current = nav.CurrentPageIndex;
			int prev = -1;
			for (int i = visibleIndices.Count - 1; i >= 0; i--)
			{
				int idx = visibleIndices[i];
				if (idx < current) { prev = idx; break; }
			}
			if (prev < 0)
				return nav; // At first page; caller decides to exit.
			return nav with { CurrentPageIndex = prev };
		}

		/// <summary>
		/// Enters summary explicitly (e.g. via button) capturing anchor.
		/// </summary>
		public static KycNavigationSnapshot EnterSummary(KycProcessState state)
		{
			KycNavigationSnapshot nav = state.Navigation;
			if (nav.State == KycFlowState.Summary) return nav;
			return nav with { State = state.ApplicationSent ? KycFlowState.PendingSummary : KycFlowState.Summary, AnchorPageIndex = nav.CurrentPageIndex };
		}

		/// <summary>
		/// Transition when editing a field from summary (drill down) preserving anchor.
		/// </summary>
		public static KycNavigationSnapshot EditFromSummary(KycProcessState state, int targetPageIndex)
		{
			KycNavigationSnapshot nav = state.Navigation;
			if (nav.State != KycFlowState.Summary && nav.State != KycFlowState.RejectedSummary)
				return nav; // Only allowed from summary flavors
			return new KycNavigationSnapshot(targetPageIndex, nav.AnchorPageIndex >= 0 ? nav.AnchorPageIndex : nav.CurrentPageIndex, KycFlowState.EditingFromSummary);
		}

		/// <summary>
		/// Returns to summary after editing a field; if invalid pages remain, remain in form mode (caller handles scan for first invalid).
		/// </summary>
		public static KycNavigationSnapshot ReturnToSummary(KycProcessState state, bool hasInvalid)
		{
			KycNavigationSnapshot nav = state.Navigation;
			if (nav.State != KycFlowState.EditingFromSummary) return nav;
			if (hasInvalid)
				return nav with { State = KycFlowState.Form }; // Caller may reposition to first invalid
			return nav with { State = KycFlowState.Summary, CurrentPageIndex = nav.AnchorPageIndex >= 0 ? nav.AnchorPageIndex : nav.CurrentPageIndex };
		}
	}
}
