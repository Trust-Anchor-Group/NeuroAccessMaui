using CommunityToolkit.Mvvm.Input;

namespace NeuroAccessMaui.UI.Pages.Wallet.TokenDetails
{
	/// <summary>
	/// Represents a page-ready identity or participant relationship for a token.
	/// </summary>
	public sealed class PartItem
	{
		/// <summary>
		/// Initializes a new instance of the <see cref="PartItem"/> class.
		/// </summary>
		/// <param name="Label">Localized relationship label.</param>
		/// <param name="LegalId">Optional legal identity identifier.</param>
		/// <param name="Jid">Optional XMPP address.</param>
		/// <param name="FriendlyName">Friendly participant name.</param>
		/// <param name="ChatCommandParameter">Parameter used to open a chat with this participant.</param>
		/// <param name="ViewIdentityCommand">Command that opens the related identity.</param>
		/// <param name="OpenChatCommand">Command that opens a chat with the related participant.</param>
		public PartItem(
			string Label,
			string? LegalId,
			string? Jid,
			string? FriendlyName,
			string? ChatCommandParameter,
			IAsyncRelayCommand? ViewIdentityCommand,
			IAsyncRelayCommand? OpenChatCommand)
		{
			this.Label = Label?.Trim() ?? string.Empty;
			this.LegalId = LegalId?.Trim() ?? string.Empty;
			this.Jid = Jid?.Trim() ?? string.Empty;
			this.FriendlyName = FirstNonEmpty(
				FriendlyName?.Trim(),
				BuildShortIdentifier(this.LegalId),
				BuildShortIdentifier(this.Jid));
			this.ChatCommandParameter = ChatCommandParameter?.Trim() ?? string.Empty;
			this.ViewIdentityCommand = ViewIdentityCommand;
			this.OpenChatCommand = OpenChatCommand;
		}

		/// <summary>
		/// Gets the localized relationship label.
		/// </summary>
		public string Label { get; }

		/// <summary>
		/// Gets the complete related legal identity identifier.
		/// </summary>
		public string LegalId { get; }

		/// <summary>
		/// Gets the complete related XMPP address.
		/// </summary>
		public string Jid { get; }

		/// <summary>
		/// Gets the best available participant display name.
		/// </summary>
		public string FriendlyName { get; }

		/// <summary>
		/// Gets the parameter used to open a chat with the participant.
		/// </summary>
		public string ChatCommandParameter { get; }

		/// <summary>
		/// Gets a value indicating whether an identity can be opened.
		/// </summary>
		public bool CanViewIdentity =>
			this.ViewIdentityCommand is not null &&
			!string.IsNullOrWhiteSpace(this.LegalId);

		/// <summary>
		/// Gets a value indicating whether a chat can be opened.
		/// </summary>
		public bool CanOpenChat =>
			this.OpenChatCommand is not null &&
			!string.IsNullOrWhiteSpace(this.Jid) &&
			!string.IsNullOrWhiteSpace(this.ChatCommandParameter);

		/// <summary>
		/// Gets the command that opens the related identity.
		/// </summary>
		public IAsyncRelayCommand? ViewIdentityCommand { get; }

		/// <summary>
		/// Gets the command that opens a chat with the related participant.
		/// </summary>
		public IAsyncRelayCommand? OpenChatCommand { get; }

		private static string FirstNonEmpty(params string?[] Values)
		{
			foreach (string? Value in Values)
			{
				if (!string.IsNullOrWhiteSpace(Value))
					return Value;
			}

			return string.Empty;
		}

		private static string BuildShortIdentifier(string Value)
		{
			if (Value.Length <= 20)
				return Value;

			return Value[..8] + "…" + Value[^6..];
		}
	}
}
