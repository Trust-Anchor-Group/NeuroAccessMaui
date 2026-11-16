using CommunityToolkit.Mvvm.ComponentModel;
using NeuroAccessMaui.UI.Controls;
using NeuroAccessMaui.UI.Pages.Wallet.MachineVariables;

namespace NeuroAccessMaui.UI.Pages.Wallet.TokenDetails
{
	/// <summary>
	/// A variable of a token's state machine, shown as a row in the Values card of token details. When its value changes
	/// while the page is open, the row is tinted for a moment and shows the value it had before.
	/// </summary>
	/// <param name="Name">Name of variable</param>
	/// <param name="Value">Value of variable</param>
	public partial class TokenVariableItem(string Name, object Value) : VariableModel(Name, Value), IListCardItem
	{
		/// <summary>
		/// Where the row sits in the Values card.
		/// </summary>
		[ObservableProperty]
		private ListCardPosition cardPosition;
	}
}
