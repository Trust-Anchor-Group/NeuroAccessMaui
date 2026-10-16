using NeuroAccessMaui.Resources.Languages;
using NeuroFeatures;
using Waher.Content.Markdown;
using Waher.Networking.XMPP.Contracts;

namespace NeuroAccessMaui.Services.Wallet.Tokens
{
	/// <summary>
	/// Formats tokens for sharing in chat. Only the token's name and ID are shared. The app receiving the link fetches the
	/// token from its Neuron when the link is opened.
	/// </summary>
	public static class TokenShareFormatter
	{
		/// <summary>
		/// Gets the chat message that shares a token: a token link, which chat shows as a token card with the shared name.
		/// </summary>
		/// <param name="Token">The token to share.</param>
		/// <returns>Markdown for the chat message.</returns>
		public static string ToChatMarkdown(Token Token)
		{
			string Name = string.IsNullOrWhiteSpace(Token.FriendlyName) ? Token.TokenId : Token.FriendlyName;
			Name = Name.Replace('\r', ' ').Replace('\n', ' ').Trim();

			return "![" + MarkdownDocument.Encode(Name) + "](" + Constants.UriSchemes.NeuroFeature + ":" + Token.TokenId + ")";
		}

		/// <summary>
		/// Checks if anyone a token is shared with can open it.
		/// </summary>
		/// <param name="Token">The token.</param>
		/// <returns>If the token is public.</returns>
		public static bool IsPublic(Token Token)
		{
			return Token.Visibility == ContractVisibility.Public || Token.Visibility == ContractVisibility.PublicSearchable;
		}

		/// <summary>
		/// Asks the user to confirm sharing a token that isn't public, since the contact may not be able to open it.
		/// Public tokens need no confirmation.
		/// </summary>
		/// <param name="Token">The token to share.</param>
		/// <param name="ContactName">Name of the contact the token is shared with.</param>
		/// <returns>If the token can be shared.</returns>
		public static Task<bool> ConfirmShareAsync(Token Token, string ContactName)
		{
			if (IsPublic(Token))
				return Task.FromResult(true);

			return ServiceRef.UiService.DisplayAlert(
				ServiceRef.Localizer[nameof(AppResources.Share)],
				ServiceRef.Localizer[nameof(AppResources.TokenShareNotPublic), ContactName],
				ServiceRef.Localizer[nameof(AppResources.Continue)],
				ServiceRef.Localizer[nameof(AppResources.Cancel)]);
		}
	}
}
