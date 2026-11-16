namespace NeuroAccessMaui.UI.Pages.Wallet.MachineVariables
{
	/// <summary>
	/// A group of a token's values shown as one card, such as Money or Dates and times.
	/// </summary>
	/// <param name="Title">The group's heading.</param>
	/// <param name="Items">The values in the group.</param>
	public sealed class VariableGroup(string Title, IReadOnlyList<VariableModel> Items)
	{
		/// <summary>
		/// The group's heading.
		/// </summary>
		public string Title { get; } = Title;

		/// <summary>
		/// The values in the group.
		/// </summary>
		public IReadOnlyList<VariableModel> Items { get; } = Items;
	}
}
