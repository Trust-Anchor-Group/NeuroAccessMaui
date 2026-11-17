using System.ComponentModel;
using System.Globalization;
using CommunityToolkit.Mvvm.ComponentModel;
using CommunityToolkit.Mvvm.Input;
using NeuroAccessMaui.Resources.Languages;
using NeuroAccessMaui.Services;
using NeuroAccessMaui.UI.Controls;
using NeuroAccessMaui.UI.Converters;
using NeuroAccessMaui.UI.Pages.Wallet.MyWallet.ObjectModels;

namespace NeuroAccessMaui.UI.Pages.Wallet.MyTokens.ObjectModels
{
	/// <summary>
	/// Tokens of the same kind, folded into one row of the token list: same name, category and currency, and either all
	/// expired or none. A tap unfolds them, and each one opens on its own.
	/// </summary>
	/// <param name="Key">What the folded tokens have in common.</param>
	public partial class TokenFamilyItem(string Key) : ObservableObject, IListCardItem
	{
		private readonly Dictionary<string, TokenFamilyMemberItem> memberRows = new(StringComparer.OrdinalIgnoreCase);
		private IReadOnlyList<TokenItem> members = [];
		private bool valuesDiffer;

		/// <summary>
		/// What the folded tokens have in common.
		/// </summary>
		public string Key { get; } = Key;

		/// <summary>
		/// The row at the end of the unfolded tokens that loads more of them.
		/// </summary>
		public TokenFamilyMoreItem MoreRow { get; } = new(Key);

		/// <summary>
		/// If the tokens are shown under the row.
		/// </summary>
		[ObservableProperty]
		private bool isExpanded;

		/// <summary>
		/// Where the row sits in its card.
		/// </summary>
		[ObservableProperty]
		private ListCardPosition cardPosition;

		/// <summary>
		/// The tokens, in the order they are listed: the most recently changed first.
		/// </summary>
		public IReadOnlyList<TokenItem> Members => this.members;

		/// <summary>
		/// The first token, whose glyph the row shows.
		/// </summary>
		public TokenItem? Leader => this.members.Count > 0 ? this.members[0] : null;

		/// <summary>
		/// If the server may have more tokens of this kind, right after the ones loaded.
		/// </summary>
		public bool MayHaveMore { get; private set; }

		/// <summary>
		/// The name the tokens share.
		/// </summary>
		public string FriendlyName => this.Leader?.FriendlyName ?? string.Empty;

		/// <summary>
		/// The category the tokens share.
		/// </summary>
		public string Category => this.Leader?.Category ?? string.Empty;

		/// <summary>
		/// If the tokens have a category.
		/// </summary>
		public bool HasCategory => this.Leader?.HasCategory ?? false;

		/// <summary>
		/// The currency of the tokens' value.
		/// </summary>
		public string Currency => this.Leader?.Currency ?? string.Empty;

		/// <summary>
		/// How many tokens are folded, such as "12 tokens", or "10+ tokens" when the server may have more.
		/// </summary>
		public string CountText => ServiceRef.Localizer[nameof(AppResources.TokenFamilyCount), this.WithMore(this.members.Count.ToString(CultureInfo.CurrentCulture))];

		/// <summary>
		/// The value of the folded tokens together, with a "+" when the server may have more.
		/// </summary>
		public string TotalText => this.WithMore(MoneyToString.ToString(this.members.Sum(Member => Member.Value)));

		/// <summary>
		/// If the tokens have expired.
		/// </summary>
		public bool IsExpired => this.Leader?.IsExpired ?? false;

		/// <summary>
		/// When the tokens expire, from the one that expires first, if that is soon or has happened.
		/// </summary>
		public string? ExpiryText => this.FirstToExpire?.ExpiryText;

		/// <summary>
		/// If <see cref="ExpiryText"/> is shown.
		/// </summary>
		public bool HasExpiryText => this.FirstToExpire is not null;

		/// <summary>
		/// The number of unread notifications about the tokens.
		/// </summary>
		public int UnreadCount => this.members.Sum(Member => Member.UnreadCount);

		/// <summary>
		/// If there are unread notifications about the tokens.
		/// </summary>
		public bool HasUnread => this.UnreadCount > 0;

		/// <summary>
		/// What a screen reader says for the row.
		/// </summary>
		public string AccessibleDescription
		{
			get
			{
				List<string> Parts = [this.FriendlyName];

				if (this.HasCategory)
					Parts.Add(this.Category);

				Parts.Add(this.CountText);
				Parts.Add(this.TotalText + " " + this.Currency);

				if (this.ExpiryText is string Expiry)
					Parts.Add(Expiry);

				if (this.HasUnread)
					Parts.Add(ServiceRef.Localizer[nameof(AppResources.TokenUnreadCount), this.UnreadCount]);

				return string.Join(", ", Parts);
			}
		}

		/// <summary>
		/// The token that expires first, among those whose expiry is shown.
		/// </summary>
		private TokenItem? FirstToExpire
		{
			get
			{
				TokenItem? First = null;

				foreach (TokenItem Member in this.members)
				{
					if (Member.HasExpiryText && (First is null || Member.Expires < First.Expires))
						First = Member;
				}

				return First;
			}
		}

		/// <summary>
		/// Sets the tokens of the fold.
		/// </summary>
		/// <param name="Members">The tokens, in the order they are listed.</param>
		/// <param name="MayHaveMore">If the server may have more tokens of this kind, right after the ones loaded.</param>
		public void Update(IReadOnlyList<TokenItem> Members, bool MayHaveMore)
		{
			foreach (TokenItem Member in this.members)
				Member.PropertyChanged -= this.Member_PropertyChanged;

			this.members = Members;
			this.MayHaveMore = MayHaveMore;

			HashSet<string> Ids = new(StringComparer.OrdinalIgnoreCase);
			decimal? FirstValue = null;
			bool Differ = false;

			foreach (TokenItem Member in this.members)
			{
				Member.PropertyChanged += this.Member_PropertyChanged;
				Ids.Add(Member.TokenId);

				if (FirstValue is null)
					FirstValue = Member.Value;
				else if (Member.Value != FirstValue)
					Differ = true;
			}

			foreach (string TokenId in this.memberRows.Keys.Where(TokenId => !Ids.Contains(TokenId)).ToList())
				this.memberRows.Remove(TokenId);

			if (this.valuesDiffer != Differ)
			{
				this.valuesDiffer = Differ;

				foreach (TokenFamilyMemberItem Row in this.memberRows.Values)
					Row.ShowsValue = Differ;
			}

			this.OnPropertyChanged(string.Empty);
		}

		/// <summary>
		/// Gets the row that shows one of the tokens under the unfolded row. The same row is returned each time, so the
		/// list doesn't redraw it.
		/// </summary>
		/// <param name="Member">One of the tokens.</param>
		/// <returns>The row.</returns>
		public TokenFamilyMemberItem GetMemberRow(TokenItem Member)
		{
			if (!this.memberRows.TryGetValue(Member.TokenId, out TokenFamilyMemberItem? Row))
			{
				Row = new TokenFamilyMemberItem(Member) { ShowsValue = this.valuesDiffer };
				this.memberRows[Member.TokenId] = Row;
			}

			return Row;
		}

		/// <summary>
		/// Stops following the tokens, when the fold is no longer listed.
		/// </summary>
		public void Release()
		{
			foreach (TokenItem Member in this.members)
				Member.PropertyChanged -= this.Member_PropertyChanged;

			this.members = [];
			this.memberRows.Clear();
		}

		/// <summary>
		/// Unfolds or folds the tokens.
		/// </summary>
		[RelayCommand]
		private void Toggle()
		{
			this.IsExpanded = !this.IsExpanded;
		}

		/// <summary>
		/// Adds a "+" to a number when the server may have more tokens of this kind.
		/// </summary>
		/// <param name="Number">The number, as text.</param>
		/// <returns>The number, with a "+" if more may exist.</returns>
		private string WithMore(string Number)
		{
			return this.MayHaveMore ? Number + "+" : Number;
		}

		/// <summary>
		/// Shows new unread notifications about one of the tokens on the row.
		/// </summary>
		private void Member_PropertyChanged(object? Sender, PropertyChangedEventArgs e)
		{
			if (e.PropertyName == nameof(TokenItem.UnreadCount))
			{
				this.OnPropertyChanged(nameof(this.UnreadCount));
				this.OnPropertyChanged(nameof(this.HasUnread));
				this.OnPropertyChanged(nameof(this.AccessibleDescription));
			}
		}
	}
}
