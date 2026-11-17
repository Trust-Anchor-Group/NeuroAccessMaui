using System.Globalization;
using CommunityToolkit.Mvvm.ComponentModel;
using NeuroAccessMaui.Resources.Languages;
using NeuroAccessMaui.Services;
using NeuroAccessMaui.UI.Controls;
using NeuroAccessMaui.UI.Pages.Wallet.MyWallet.ObjectModels;

namespace NeuroAccessMaui.UI.Pages.Wallet.MyTokens.ObjectModels
{
	/// <summary>
	/// One token of an unfolded <see cref="TokenFamilyItem"/>, as a short row under it. It tells the token apart from the
	/// others by its number in its batch and when it was created.
	/// </summary>
	/// <param name="Token">The token.</param>
	public partial class TokenFamilyMemberItem(TokenItem Token) : ObservableObject, IListCardItem
	{
		/// <summary>
		/// The token.
		/// </summary>
		public TokenItem Token { get; } = Token;

		/// <summary>
		/// Where the row sits in its card.
		/// </summary>
		[ObservableProperty]
		private ListCardPosition cardPosition;

		/// <summary>
		/// If the row shows the token's value, because the folded tokens are not all worth the same.
		/// </summary>
		[ObservableProperty]
		private bool showsValue;

		/// <summary>
		/// If the token was made in a batch of several, so its number in the batch tells it apart.
		/// </summary>
		public bool IsInBatch => this.Token.BatchSize > 1;

		/// <summary>
		/// What tells the token apart: its number in its batch, such as "17 of 250", or else the day it was created.
		/// </summary>
		public string Title => this.IsInBatch
			? ServiceRef.Localizer[nameof(AppResources.TokenOrdinalOfBatch), this.Token.Ordinal, this.Token.BatchSize]
			: GetCreatedText(this.Token.Created);

		/// <summary>
		/// The day the token was created, shown beside its number in its batch. Empty when <see cref="Title"/> shows it.
		/// </summary>
		public string Detail => this.IsInBatch ? GetCreatedText(this.Token.Created) : string.Empty;

		/// <summary>
		/// If the row shows when the token expires: soon, but not yet. Expired tokens are folded apart, so the fold says it.
		/// </summary>
		public bool ShowsExpiry => this.Token.HasExpiryText && !this.Token.IsExpired;

		/// <summary>
		/// Gets the day a token was created, in the user's time zone.
		/// </summary>
		/// <param name="Created">When the token was created.</param>
		/// <returns>The local date, or an empty string if it isn't known.</returns>
		private static string GetCreatedText(DateTime Created)
		{
			if (Created == DateTime.MinValue)
				return string.Empty;

			DateTime Local = Created.Kind switch
			{
				DateTimeKind.Local => Created,
				DateTimeKind.Utc => Created.ToLocalTime(),
				_ => DateTime.SpecifyKind(Created, DateTimeKind.Utc).ToLocalTime()
			};

			return Local.ToString("d", CultureInfo.CurrentCulture);
		}
	}
}
