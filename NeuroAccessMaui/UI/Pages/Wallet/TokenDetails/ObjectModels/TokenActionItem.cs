using CommunityToolkit.Mvvm.Input;

namespace NeuroAccessMaui.UI.Pages.Wallet.TokenDetails.ObjectModels
{
	/// <summary>
	/// Defines the visual priority of an available token action.
	/// </summary>
	public enum TokenActionProminence
	{
		/// <summary>
		/// Represents the single action emphasized as the likely next step.
		/// </summary>
		Primary,

		/// <summary>
		/// Represents an ordinary secondary action.
		/// </summary>
		Secondary,

		/// <summary>
		/// Represents an expert or infrequently used action.
		/// </summary>
		Advanced
	}

	/// <summary>
	/// Describes a localized token action without embedding protocol eligibility logic in the view.
	/// </summary>
	public sealed class TokenActionItem
	{
		/// <summary>
		/// Initializes a new instance of the <see cref="TokenActionItem"/> class.
		/// </summary>
		/// <param name="Title">Localized outcome-oriented action title.</param>
		/// <param name="Description">Localized explanation of the action.</param>
		/// <param name="IconSource">Optional SVG asset associated with the action.</param>
		/// <param name="Prominence">Visual priority of the action.</param>
		/// <param name="IsDestructive">Whether the action can have destructive consequences.</param>
		/// <param name="IsAvailable">Whether the action is currently available.</param>
		/// <param name="AvailabilityExplanation">Localized explanation shown when the action is unavailable.</param>
		/// <param name="Command">Command that performs the already-validated action.</param>
		public TokenActionItem(
			string Title,
			string Description,
			string? IconSource,
			TokenActionProminence Prominence,
			bool IsDestructive,
			bool IsAvailable,
			string AvailabilityExplanation,
			IAsyncRelayCommand? Command)
		{
			this.Title = Title?.Trim() ?? string.Empty;
			this.Description = Description?.Trim() ?? string.Empty;
			this.IconSource = string.IsNullOrWhiteSpace(IconSource) ? null : IconSource.Trim();
			this.Prominence = Prominence;
			this.IsDestructive = IsDestructive;
			this.IsAvailable = IsAvailable;
			this.AvailabilityExplanation = AvailabilityExplanation?.Trim() ?? string.Empty;
			this.Command = Command;
		}

		/// <summary>
		/// Gets the localized outcome-oriented action title.
		/// </summary>
		public string Title { get; }

		/// <summary>
		/// Gets the localized explanation of the action.
		/// </summary>
		public string Description { get; }

		/// <summary>
		/// Gets a value indicating whether supporting action copy is available.
		/// </summary>
		public bool HasDescription => !string.IsNullOrWhiteSpace(this.Description);

		/// <summary>
		/// Gets the optional SVG asset associated with the action.
		/// </summary>
		public string? IconSource { get; }

		/// <summary>
		/// Gets the visual priority of the action.
		/// </summary>
		public TokenActionProminence Prominence { get; }

		/// <summary>
		/// Gets a value indicating whether the action can have destructive consequences.
		/// </summary>
		public bool IsDestructive { get; }

		/// <summary>
		/// Gets a value indicating whether the action is currently available.
		/// </summary>
		public bool IsAvailable { get; }

		/// <summary>
		/// Gets the localized explanation shown when the action is unavailable.
		/// </summary>
		public string AvailabilityExplanation { get; }

		/// <summary>
		/// Gets a value indicating whether an availability explanation is present.
		/// </summary>
		public bool HasAvailabilityExplanation =>
			!string.IsNullOrWhiteSpace(this.AvailabilityExplanation);

		/// <summary>
		/// Gets the command that performs the already-validated action.
		/// </summary>
		public IAsyncRelayCommand? Command { get; }
	}
}
