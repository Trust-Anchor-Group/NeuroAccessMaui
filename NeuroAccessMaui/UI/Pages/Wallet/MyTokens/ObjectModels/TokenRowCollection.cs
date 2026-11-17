using System.Collections.ObjectModel;
using System.ComponentModel;
using NeuroAccessMaui.UI.Pages.Wallet.MyWallet.ObjectModels;

namespace NeuroAccessMaui.UI.Pages.Wallet.MyTokens.ObjectModels
{
	/// <summary>
	/// The rows of the token list. Tokens of the same kind fold into one <see cref="TokenFamilyItem"/>, placed where the
	/// first of them is listed. An unfolded row is followed by a <see cref="TokenFamilyMemberItem"/> for each of its tokens,
	/// and by its <see cref="TokenFamilyMoreItem"/> when the server may have more of them.
	/// </summary>
	public class TokenRowCollection : ObservableCollection<object>
	{
		private readonly Dictionary<string, TokenFamilyItem> families = new(StringComparer.CurrentCultureIgnoreCase);
		private IReadOnlyList<TokenItem> tokens = [];
		private bool hasMore;

		/// <summary>
		/// Shows tokens, folding those of the same kind. Rows that stay are kept and moved, so the list doesn't flash, and a
		/// fold stays open or closed.
		/// </summary>
		/// <param name="Tokens">The tokens, in the order they are listed.</param>
		/// <param name="HasMore">If the server may have more tokens after the last one.</param>
		public void Show(IReadOnlyList<TokenItem> Tokens, bool HasMore)
		{
			this.tokens = [.. Tokens];
			this.hasMore = HasMore;
			this.Rebuild();
		}

		/// <summary>
		/// Finds a fold by its key.
		/// </summary>
		/// <param name="Key">The <see cref="TokenFamilyItem.Key"/>.</param>
		/// <returns>The fold, or null if it isn't listed.</returns>
		public TokenFamilyItem? FindFamily(string Key)
		{
			return this.families.TryGetValue(Key, out TokenFamilyItem? Family) ? Family : null;
		}

		/// <summary>
		/// Gets what tokens of the same kind have in common: their name, category and currency, and if they have expired.
		/// </summary>
		/// <param name="Token">A token.</param>
		/// <returns>The key of its fold.</returns>
		private static string GetKey(TokenItem Token)
		{
			return string.Join('\u001f', Token.FriendlyName, Token.Category, Token.Currency, Token.IsExpired ? "1" : "0");
		}

		/// <summary>
		/// Lists the rows again, from the tokens last shown.
		/// </summary>
		private void Rebuild()
		{
			Dictionary<string, List<TokenItem>> ByKey = new(StringComparer.CurrentCultureIgnoreCase);
			List<string> Order = [];

			foreach (TokenItem Token in this.tokens)
			{
				string Key = GetKey(Token);

				if (!ByKey.TryGetValue(Key, out List<TokenItem>? Members))
				{
					Members = [];
					ByKey[Key] = Members;
					Order.Add(Key);
				}

				Members.Add(Token);
			}

			// Only the kind of the last token loaded may continue on the server's next page.
			string? TailKey = this.tokens.Count > 0 ? GetKey(this.tokens[^1]) : null;
			Dictionary<string, TokenFamilyItem> Kept = new(StringComparer.CurrentCultureIgnoreCase);
			List<object> Rows = [];

			foreach (string Key in Order)
			{
				List<TokenItem> Members = ByKey[Key];

				if (Members.Count == 1)
				{
					Rows.Add(Members[0]);
					continue;
				}

				if (!this.families.TryGetValue(Key, out TokenFamilyItem? Family))
				{
					Family = new TokenFamilyItem(Key);
					Family.PropertyChanged += this.Family_PropertyChanged;
				}

				Family.Update(Members, this.hasMore && string.Equals(Key, TailKey, StringComparison.CurrentCultureIgnoreCase));
				Kept[Key] = Family;
				Rows.Add(Family);

				if (Family.IsExpanded)
				{
					foreach (TokenItem Member in Members)
						Rows.Add(Family.GetMemberRow(Member));

					if (Family.MayHaveMore)
						Rows.Add(Family.MoreRow);
				}
			}

			foreach (KeyValuePair<string, TokenFamilyItem> Pair in this.families)
			{
				if (!Kept.ContainsKey(Pair.Key))
				{
					Pair.Value.PropertyChanged -= this.Family_PropertyChanged;
					Pair.Value.Release();
				}
			}

			this.families.Clear();
			foreach (KeyValuePair<string, TokenFamilyItem> Pair in Kept)
				this.families[Pair.Key] = Pair.Value;

			this.Apply(Rows);
		}

		/// <summary>
		/// Changes the listed rows into new ones with as few moves, inserts and removals as possible.
		/// </summary>
		/// <param name="Rows">The rows to list.</param>
		private void Apply(List<object> Rows)
		{
			for (int i = 0; i < Rows.Count; i++)
			{
				if (i < this.Count && ReferenceEquals(this[i], Rows[i]))
					continue;

				int Found = this.IndexOf(Rows[i]);

				if (Found > i)
					this.Move(Found, i);
				else
					this.Insert(i, Rows[i]);
			}

			while (this.Count > Rows.Count)
				this.RemoveAt(this.Count - 1);
		}

		/// <summary>
		/// Shows or hides a fold's tokens when it is unfolded or folded.
		/// </summary>
		private void Family_PropertyChanged(object? Sender, PropertyChangedEventArgs e)
		{
			if (e.PropertyName == nameof(TokenFamilyItem.IsExpanded))
				this.Rebuild();
		}
	}
}
