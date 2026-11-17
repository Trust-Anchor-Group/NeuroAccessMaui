using NeuroAccessMaui.UI.Pages.Wallet.MyTokens.ObjectModels;

namespace NeuroAccessMaui.UI.Pages.Wallet.MyTokens
{
	/// <summary>
	/// Chooses the row template in the list of tokens: a token, tokens of the same kind folded together, one of the
	/// tokens of an unfolded row, or the row that loads more of them.
	/// </summary>
	public class TokenListItemTemplateSelector : DataTemplateSelector
	{
		/// <summary>
		/// Template for a token row.
		/// </summary>
		public DataTemplate? TokenTemplate { get; set; }

		/// <summary>
		/// Template for tokens of the same kind, folded into one row.
		/// </summary>
		public DataTemplate? FamilyTemplate { get; set; }

		/// <summary>
		/// Template for one of the tokens under an unfolded row.
		/// </summary>
		public DataTemplate? MemberTemplate { get; set; }

		/// <summary>
		/// Template for the row that loads more tokens of an unfolded row's kind.
		/// </summary>
		public DataTemplate? MoreTemplate { get; set; }

		/// <inheritdoc/>
		protected override DataTemplate? OnSelectTemplate(object Item, BindableObject Container)
		{
			return Item switch
			{
				TokenFamilyItem => this.FamilyTemplate,
				TokenFamilyMemberItem => this.MemberTemplate,
				TokenFamilyMoreItem => this.MoreTemplate,
				_ => this.TokenTemplate
			};
		}
	}
}
