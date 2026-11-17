using CommunityToolkit.Mvvm.ComponentModel;
using NeuroAccessMaui.UI.Controls;

namespace NeuroAccessMaui.UI.Pages.Wallet.MyTokens.ObjectModels
{
	/// <summary>
	/// The row at the end of an unfolded <see cref="TokenFamilyItem"/>, shown when the server may have more tokens of its
	/// kind. A tap loads them.
	/// </summary>
	/// <param name="FamilyKey">The <see cref="TokenFamilyItem.Key"/> of the fold the row belongs to.</param>
	public partial class TokenFamilyMoreItem(string FamilyKey) : ObservableObject, IListCardItem
	{
		/// <summary>
		/// The <see cref="TokenFamilyItem.Key"/> of the fold the row belongs to.
		/// </summary>
		public string FamilyKey { get; } = FamilyKey;

		/// <summary>
		/// Where the row sits in its card.
		/// </summary>
		[ObservableProperty]
		private ListCardPosition cardPosition;

		/// <summary>
		/// If more tokens are being loaded.
		/// </summary>
		[ObservableProperty]
		private bool isLoading;
	}
}
