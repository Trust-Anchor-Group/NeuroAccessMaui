using CommunityToolkit.Mvvm.ComponentModel;
using CommunityToolkit.Mvvm.Input;
using NeuroAccessMaui.Resources.Languages;
using NeuroAccessMaui.Services;
using NeuroAccessMaui.Services.UI;
using NeuroAccessMaui.Services.UI.Toasts;
using NeuroAccessMaui.UI.Controls;
using NeuroAccessMaui.UI.Pages.Contacts.Chat;

namespace NeuroAccessMaui.UI.Pages.Wallet.TokenDetails
{
	/// <summary>
	/// What a token property holds, which decides what tapping it opens.
	/// </summary>
	public enum TokenPropertyKind
	{
		/// <summary>
		/// Plain text. Tapping it does nothing, but it can be copied.
		/// </summary>
		Text,

		/// <summary>
		/// A web link, opened in the browser.
		/// </summary>
		Link,

		/// <summary>
		/// A legal identity, opened in the identity view.
		/// </summary>
		LegalId,

		/// <summary>
		/// A network address, opened in a chat.
		/// </summary>
		Jid,

		/// <summary>
		/// A smart contract, opened in the contract view.
		/// </summary>
		Contract
	}

	/// <summary>
	/// A labeled value shown as a row in token details. A tap opens the value according to its <see cref="Kind"/>, and a
	/// long press copies it.
	/// </summary>
	public partial class TokenPropertyItem : ObservableObject, IListCardItem
	{
		/// <summary>
		/// Creates a property row that opens and copies its shown value.
		/// </summary>
		/// <param name="Label">Label of the property.</param>
		/// <param name="Value">Value shown.</param>
		/// <param name="Kind">What the value is.</param>
		public TokenPropertyItem(string Label, string Value, TokenPropertyKind Kind)
			: this(Label, Value, Kind, null)
		{
		}

		/// <summary>
		/// Creates a property row.
		/// </summary>
		/// <param name="Label">Label of the property.</param>
		/// <param name="Value">Value shown, such as a person's name.</param>
		/// <param name="Kind">What the value is.</param>
		/// <param name="Target">What is opened, such as the identity ID behind a name. The shown value when null.</param>
		public TokenPropertyItem(string Label, string Value, TokenPropertyKind Kind, string? Target)
		{
			this.Label = Label;
			this.Value = Value;
			this.Kind = Kind;
			this.Target = Target ?? Value;
		}

		/// <summary>
		/// Label of the property.
		/// </summary>
		public string Label { get; }

		/// <summary>
		/// Value shown.
		/// </summary>
		public string Value { get; }

		/// <summary>
		/// What the value is, which decides what tapping it opens.
		/// </summary>
		public TokenPropertyKind Kind { get; }

		/// <summary>
		/// What is opened: a web address, an identity ID, a network address or a contract ID.
		/// </summary>
		public string Target { get; }

		/// <summary>
		/// What is copied, when it isn't <see cref="Target"/>.
		/// </summary>
		public string? CopyText { get; init; }

		/// <summary>
		/// The toast shown after copying, when it isn't the general one.
		/// </summary>
		public string? CopiedMessage { get; init; }

		/// <summary>
		/// For network addresses: the legal identity of the party, passed on to the chat.
		/// </summary>
		public string? LegalId { get; init; }

		/// <summary>
		/// For network addresses: the name of the party, shown in the chat.
		/// </summary>
		public string? FriendlyName { get; init; }

		/// <summary>
		/// If tapping the value opens something.
		/// </summary>
		public bool IsActionable => this.Kind != TokenPropertyKind.Text;

		/// <summary>
		/// What screen readers say about the row: the label and the value.
		/// </summary>
		public string AccessibleDescription => this.Label + ", " + this.Value;

		/// <summary>
		/// What screen readers say the row does.
		/// </summary>
		public string AccessibleHint => this.IsActionable
			? ServiceRef.Localizer[nameof(AppResources.TapToOpenLongPressToCopy)]
			: ServiceRef.Localizer[nameof(AppResources.LongPressToCopy)];

		/// <summary>
		/// Where the row sits in its card.
		/// </summary>
		[ObservableProperty]
		private ListCardPosition cardPosition;

		/// <summary>
		/// Opens the value according to its kind. Plain text isn't opened. The command can always run, so the row keeps
		/// taking the long press that copies it.
		/// </summary>
		[RelayCommand]
		private async Task Open()
		{
			if (!this.IsActionable)
				return;

			try
			{
				switch (this.Kind)
				{
					case TokenPropertyKind.Link:
						await App.OpenUrlAsync(this.Target);
						break;

					case TokenPropertyKind.LegalId:
						await ServiceRef.ContractOrchestratorService.OpenLegalIdentity(this.Target,
							ServiceRef.Localizer[nameof(AppResources.PurposeReviewToken)]);
						break;

					case TokenPropertyKind.Contract:
						await ServiceRef.ContractOrchestratorService.OpenContract(this.Target,
							ServiceRef.Localizer[nameof(AppResources.PurposeReviewToken)], null);
						break;

					case TokenPropertyKind.Jid:
						ChatNavigationArgs Args = new(this.LegalId, this.Target, this.FriendlyName);
						await ServiceRef.NavigationService.GoToAsync(nameof(ChatPage), Args, BackMethod.Inherited, this.Target);
						break;
				}
			}
			catch (Exception Ex)
			{
				ServiceRef.LogService.LogException(Ex);
				await ServiceRef.UiService.DisplayException(Ex);
			}
		}

		/// <summary>
		/// Copies the value to the clipboard.
		/// </summary>
		[RelayCommand]
		private Task Copy()
		{
			return CopyAsync(this.CopyText ?? this.Target, this.CopiedMessage);
		}

		/// <summary>
		/// Copies a text to the clipboard, and confirms it with a toast.
		/// </summary>
		/// <param name="Text">Text to copy.</param>
		/// <param name="Message">The toast to show, or null for the general one.</param>
		public static async Task CopyAsync(string Text, string? Message)
		{
			try
			{
				await Clipboard.SetTextAsync(Text);
				await ServiceRef.ToastService.ShowTextAsync(Message ?? ServiceRef.Localizer[nameof(AppResources.SuccessfullyCopiedToClipboard)]);
			}
			catch (Exception Ex)
			{
				ServiceRef.LogService.LogException(Ex);
				await ServiceRef.UiService.DisplayException(Ex);
			}
		}
	}
}
