using CommunityToolkit.Mvvm.ComponentModel;

namespace NeuroAccessMaui.UI.Pages.Wallet.MyTokens
{
	/// <summary>
	/// A token category the token list can be filtered by.
	/// </summary>
	/// <param name="Name">Name of the category.</param>
	public partial class TokenCategoryChip(string Name) : ObservableObject
	{
		/// <summary>
		/// Name of the category.
		/// </summary>
		public string Name { get; } = Name;

		/// <summary>
		/// If the list is filtered by this category.
		/// </summary>
		[ObservableProperty]
		private bool isSelected;
	}
}
