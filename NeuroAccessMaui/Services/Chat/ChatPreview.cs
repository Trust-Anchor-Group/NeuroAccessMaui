using System.Text;
using System.Text.RegularExpressions;
using NeuroAccessMaui.Resources.Languages;
using NeuroAccessMaui.UI.Pages.Contacts.Chat;

namespace NeuroAccessMaui.Services.Chat
{
	/// <summary>
	/// Makes the short text shown for a message in the list of chats. Something shared is shown as a word for what it is,
	/// such as "Photo" or "Contract".
	/// </summary>
	public static partial class ChatPreview
	{
		private const int maxLength = 100;

		/// <summary>
		/// The short text for a message.
		/// </summary>
		/// <param name="Message">The message.</param>
		/// <returns>The text, on one line.</returns>
		public static string Of(ChatMessage Message)
		{
			string Markdown = Message.Markdown.Trim();

			string? SharedKey = SharedKeyOf(Markdown);
			if (SharedKey is not null)
				return ServiceRef.Localizer[SharedKey];

			string Text = string.IsNullOrWhiteSpace(Message.PlainText) ? Markdown : Message.PlainText;
			return Shorten(Text);
		}

		/// <summary>
		/// The text key of the word for what a message shares, if the whole message is one shared thing.
		/// </summary>
		private static string? SharedKeyOf(string Markdown)
		{
			if (Markdown.Length == 0)
				return null;

			if (Markdown.StartsWith("```" + Constants.UriSchemes.IotId, StringComparison.OrdinalIgnoreCase))
				return nameof(AppResources.ChatsPreviewId);

			if (Markdown.StartsWith("```" + Constants.UriSchemes.IotSc, StringComparison.OrdinalIgnoreCase))
				return nameof(AppResources.ChatsPreviewContract);

			Match M = SingleImageLink().Match(Markdown);
			if (!M.Success)
				return null;

			return Constants.UriSchemes.GetScheme(M.Groups["Url"].Value) switch
			{
				Constants.UriSchemes.IotId or Constants.UriSchemes.Xmpp => nameof(AppResources.ChatsPreviewContact),
				Constants.UriSchemes.IotSc => nameof(AppResources.ChatsPreviewContract),
				Constants.UriSchemes.EDaler => nameof(AppResources.ChatsPreviewPaymentRequest),
				Constants.UriSchemes.NeuroFeature => nameof(AppResources.ChatsPreviewToken),
				Constants.UriSchemes.IotDisco => nameof(AppResources.ChatsPreviewDevice),
				_ => nameof(AppResources.ChatsPreviewPhoto)
			};
		}

		/// <summary>
		/// The first line of a text with its spaces evened out, cut to a short length.
		/// </summary>
		private static string Shorten(string Text)
		{
			StringBuilder Result = new();
			bool Space = false;

			foreach (char ch in Text.Trim())
			{
				if (char.IsWhiteSpace(ch))
				{
					if (Result.Length > 0 && (ch == '\n' || ch == '\r'))
						break;

					Space = Result.Length > 0;
					continue;
				}

				if (Space)
				{
					Result.Append(' ');
					Space = false;
				}

				Result.Append(ch);

				if (Result.Length >= maxLength)
				{
					Result.Append('…');
					break;
				}
			}

			return Result.ToString();
		}

		/// <summary>
		/// A message that is only one image link, as shared things are sent.
		/// </summary>
		[GeneratedRegex(@"^!\[[^\]]*\]\((?<Url>[^)\s]+)\)$", RegexOptions.Singleline)]
		private static partial Regex SingleImageLink();
	}
}
