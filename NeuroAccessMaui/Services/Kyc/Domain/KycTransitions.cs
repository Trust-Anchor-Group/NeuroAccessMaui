using System;
using System.Collections.Generic;
using System.Linq;

namespace NeuroAccessMaui.Services.Kyc.Domain
{
	/// <summary>
	/// Pure transition helpers for KYC navigation &amp; flow. No side-effects, deterministic.
	/// </summary>
	internal static class KycTransitions
	{
		/// <summary>
		/// Computes next navigation snapshot when advancing from a form page.
		/// </summary>
		public static KycNavigationSnapshot Advance(KycProcessState State, IReadOnlyList<int> VisibleIndices)
		{
			KycNavigationSnapshot nav = State.Navigation;
			if (State.Navigation.State is KycFlowState.Summary or KycFlowState.PendingSummary)
				return nav; // Already at summary; engine will decide apply action.

			// Find next visible page after current
			int Current = nav.CurrentPageIndex;
			int Next = -1;
			foreach (int idx in VisibleIndices)
			{
				if (idx > Current) { Next = idx; break; }
			}
			if (Next < 0)
			{
				// No further pages -> transition to summary
				return nav with { State = State.ApplicationSent ? KycFlowState.PendingSummary : KycFlowState.Summary };
			}
			return nav with { CurrentPageIndex = Next };
		}

		/// <summary>
		/// Computes previous navigation snapshot when moving backward.
		/// </summary>
		public static KycNavigationSnapshot Back(KycProcessState state, IReadOnlyList<int> visibleIndices)
		{
			KycNavigationSnapshot Nav = state.Navigation;
			if (Nav.State == KycFlowState.Summary)
			{
				// Leave summary returning to anchor page
				return Nav with { State = KycFlowState.Form, CurrentPageIndex = Nav.AnchorPageIndex >= 0 ? Nav.AnchorPageIndex : Nav.CurrentPageIndex };
			}
			int Current = Nav.CurrentPageIndex;
			int Prev = -1;
			for (int i = visibleIndices.Count - 1; i >= 0; i--)
			{
				int idx = visibleIndices[i];
				if (idx < Current) { Prev = idx; break; }
			}
			if (Prev < 0)
				return Nav; // At first page; caller decides to exit.
			return Nav with { CurrentPageIndex = Prev };
		}

		/// <summary>
		/// Enters summary explicitly (e.g. via button) capturing anchor.
		/// </summary>
		public static KycNavigationSnapshot EnterSummary(KycProcessState state)
		{
			KycNavigationSnapshot Nav = state.Navigation;
			if (Nav.State == KycFlowState.Summary) return Nav;
			return Nav with { State = state.ApplicationSent ? KycFlowState.PendingSummary : KycFlowState.Summary, AnchorPageIndex = Nav.CurrentPageIndex };
		}

		/// <summary>
		/// Transition when editing a field from summary (drill down) preserving anchor.
		/// </summary>
		public static KycNavigationSnapshot EditFromSummary(KycProcessState state, int targetPageIndex)
		{
			KycNavigationSnapshot Nav = state.Navigation;
			if (Nav.State != KycFlowState.Summary && Nav.State != KycFlowState.RejectedSummary)
				return Nav; // Only allowed from summary flavors
			return new KycNavigationSnapshot(targetPageIndex, Nav.AnchorPageIndex >= 0 ? Nav.AnchorPageIndex : Nav.CurrentPageIndex, KycFlowState.EditingFromSummary);
		}

		/// <summary>
		/// Returns to summary after editing a field; if invalid pages remain, remain in form mode (caller handles scan for first invalid).
		/// </summary>
		public static KycNavigationSnapshot ReturnToSummary(KycProcessState state, bool hasInvalid)
		{
			KycNavigationSnapshot Nav = state.Navigation;
			if (Nav.State != KycFlowState.EditingFromSummary) return Nav;
			if (hasInvalid)
				return Nav with { State = KycFlowState.Form }; // Caller may reposition to first invalid
			return Nav with { State = KycFlowState.Summary, CurrentPageIndex = Nav.AnchorPageIndex >= 0 ? Nav.AnchorPageIndex : Nav.CurrentPageIndex };
		}
	}
}
