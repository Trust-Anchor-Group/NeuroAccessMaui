namespace NeuroAccessMaui.Services.Kyc.Actions
{
	/// <summary>
	/// Describes the UI state for a KYC page action.
	/// </summary>
	public class KycPageActionState
	{
		/// <summary>
		/// Gets or sets a value indicating whether the action should be shown.
		/// </summary>
		public bool IsVisible { get; set; } = true;

		/// <summary>
		/// Gets or sets a value indicating whether the primary action can be executed.
		/// </summary>
		public bool IsEnabled { get; set; } = true;

		/// <summary>
		/// Gets or sets a value indicating whether the action is currently busy.
		/// </summary>
		public bool IsBusy { get; set; }

		/// <summary>
		/// Gets or sets the action title.
		/// </summary>
		public string? Title { get; set; }

		/// <summary>
		/// Gets or sets the action description.
		/// </summary>
		public string? Description { get; set; }

		/// <summary>
		/// Gets or sets status text for the action.
		/// </summary>
		public string? StatusText { get; set; }

		/// <summary>
		/// Gets or sets the primary button text.
		/// </summary>
		public string? PrimaryButtonText { get; set; }

		/// <summary>
		/// Gets or sets warning text for recoverable action issues.
		/// </summary>
		public string? WarningText { get; set; }

		/// <summary>
		/// Gets or sets error text for blocking action issues.
		/// </summary>
		public string? ErrorText { get; set; }
	}
}
