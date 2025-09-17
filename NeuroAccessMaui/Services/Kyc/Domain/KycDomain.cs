namespace NeuroAccessMaui.Services.Kyc.Domain
{
	/// <summary>
	/// High-level flow states for the KYC process UI.
	/// </summary>
	public enum KycFlowState
	{
		Form,
		Summary,
		EditingFromSummary,
		RejectedSummary,
		PendingSummary
	}

	/// <summary>
	/// Navigation snapshot capturing minimal mutable UI navigation state.
	/// </summary>
	/// <param name="CurrentPageIndex">Current visible page index among visible pages.</param>
	/// <param name="AnchorPageIndex">Anchor page to return to when exiting summary/edit flows.</param>
	/// <param name="State">Current flow state.</param>
	public sealed record KycNavigationSnapshot(int CurrentPageIndex, int AnchorPageIndex, KycFlowState State);

	/// <summary>
	/// Immutable field state used by pure domain operations.
	/// </summary>
	/// <param name="Id">Field identifier.</param>
	/// <param name="IsValid">Last known validity (sync portion).</param>
	/// <param name="Value">Normalized string value.</param>
	public sealed record KycFieldState(string Id, bool IsValid, string? Value);

	/// <summary>
	/// Immutable page state abstraction for domain calculations (validation, navigation decisions).
	/// </summary>
	/// <param name="Id">Page identifier.</param>
	/// <param name="IsVisible">Whether page is currently visible given field values.</param>
	/// <param name="FieldStates">Flattened field states (includes section fields).</param>
	public sealed record KycPageState(string Id, bool IsVisible, IReadOnlyList<KycFieldState> FieldStates);

	/// <summary>
	/// Root aggregate-like snapshot passed through pure functions.
	/// </summary>
	/// <param name="Pages">All page states (original ordering).</param>
	/// <param name="Navigation">Current navigation snapshot.</param>
	/// <param name="ApplicationSent">Whether application has been submitted.</param>
	public sealed record KycProcessState(IReadOnlyList<KycPageState> Pages, KycNavigationSnapshot Navigation, bool ApplicationSent)
	{
		/// <summary>
		/// Gets indices of pages considered visible.
		/// </summary>
		public IEnumerable<int> VisiblePageIndices => this.Pages.Select((p,i) => (p,i)).Where(t => t.p.IsVisible).Select(t => t.i);
	}
}
