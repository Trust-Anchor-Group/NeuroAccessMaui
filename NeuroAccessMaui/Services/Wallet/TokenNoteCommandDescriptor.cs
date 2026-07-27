using Waher.Networking.XMPP.Contracts;

namespace NeuroAccessMaui.Services.Wallet
{
	/// <summary>
	/// Describes a token-defined action that is applicable to the current user and state.
	/// </summary>
	public sealed class TokenNoteCommandDescriptor
	{
		/// <summary>
		/// Initializes a token-defined action descriptor.
		/// </summary>
		/// <param name="CommandIndex">Stable position in the current token definition.</param>
		/// <param name="Id">Protocol command identifier.</param>
		/// <param name="Title">Localized outcome-oriented title.</param>
		/// <param name="HelpText">Localized supporting explanation.</param>
		/// <param name="ConfirmationText">Localized confirmation copy.</param>
		/// <param name="SuccessText">Localized success copy.</param>
		/// <param name="FailureText">Localized failure copy.</param>
		/// <param name="Personal">Whether the generated update is private to the owner.</param>
		/// <param name="IsOwnerContext">Whether the command was discovered for the current owner.</param>
		/// <param name="CurrentState">Current state used to determine eligibility.</param>
		/// <param name="Parameters">Supported parameter definitions.</param>
		internal TokenNoteCommandDescriptor(
			int CommandIndex,
			string Id,
			string Title,
			string HelpText,
			string ConfirmationText,
			string SuccessText,
			string FailureText,
			bool Personal,
			bool IsOwnerContext,
			string CurrentState,
			Parameter[] Parameters)
		{
			this.CommandIndex = CommandIndex;
			this.Id = Id?.Trim() ?? string.Empty;
			this.Title = Title?.Trim() ?? string.Empty;
			this.HelpText = HelpText?.Trim() ?? string.Empty;
			this.ConfirmationText = ConfirmationText?.Trim() ?? string.Empty;
			this.SuccessText = SuccessText?.Trim() ?? string.Empty;
			this.FailureText = FailureText?.Trim() ?? string.Empty;
			this.Personal = Personal;
			this.IsOwnerContext = IsOwnerContext;
			this.CurrentState = CurrentState?.Trim() ?? string.Empty;
			this.Parameters = Parameters ?? [];
		}

		/// <summary>
		/// Gets the command's position in the current token definition.
		/// </summary>
		public int CommandIndex { get; }

		/// <summary>
		/// Gets the protocol command identifier.
		/// </summary>
		public string Id { get; }

		/// <summary>
		/// Gets the localized outcome-oriented title.
		/// </summary>
		public string Title { get; }

		/// <summary>
		/// Gets localized supporting help.
		/// </summary>
		public string HelpText { get; }

		/// <summary>
		/// Gets a value indicating whether supporting help is available.
		/// </summary>
		public bool HasHelpText => !string.IsNullOrWhiteSpace(this.HelpText);

		/// <summary>
		/// Gets localized confirmation copy.
		/// </summary>
		public string ConfirmationText { get; }

		/// <summary>
		/// Gets localized success copy.
		/// </summary>
		public string SuccessText { get; }

		/// <summary>
		/// Gets localized failure copy.
		/// </summary>
		public string FailureText { get; }

		/// <summary>
		/// Gets a value indicating whether the generated update is private to the owner.
		/// </summary>
		public bool Personal { get; }

		/// <summary>
		/// Gets a value indicating whether the action was discovered for the current owner.
		/// </summary>
		public bool IsOwnerContext { get; }

		/// <summary>
		/// Gets the current state used to determine eligibility.
		/// </summary>
		public string CurrentState { get; }

		/// <summary>
		/// Gets a value indicating whether a current state label is available.
		/// </summary>
		public bool HasCurrentState => !string.IsNullOrWhiteSpace(this.CurrentState);

		/// <summary>
		/// Gets the supported action parameters.
		/// </summary>
		public IReadOnlyList<Parameter> Parameters { get; }

		/// <summary>
		/// Gets a value indicating whether the action requires parameters.
		/// </summary>
		public bool HasParameters => this.Parameters.Count > 0;
	}
}
