using Microsoft.Maui.Controls.Shapes;
using NeuroAccessMaui.UI.Pages.Contracts.ObjectModel;

namespace NeuroAccessMaui.UI.Pages.Contracts
{
	/// <summary>
	/// A group of a contract's values, shown as one list card when reading a contract: the values themselves, calculated
	/// values, values from people's IDs, and other values such as linked contracts and places.
	/// </summary>
	/// <param name="Title">The group's heading, or an empty string for the first group, which has none.</param>
	/// <param name="Icon">The icon before the heading, or null.</param>
	/// <param name="Items">The values in the group.</param>
	public sealed class ParameterGroup(string Title, Geometry? Icon, IReadOnlyList<ObservableParameter> Items)
	{
		/// <summary>
		/// The group's heading, or an empty string for the first group.
		/// </summary>
		public string Title { get; } = Title;

		/// <summary>
		/// If the group has a heading.
		/// </summary>
		public bool HasTitle => !string.IsNullOrEmpty(this.Title);

		/// <summary>
		/// The icon before the heading, or null.
		/// </summary>
		public Geometry? Icon { get; } = Icon;

		/// <summary>
		/// If the heading has an icon.
		/// </summary>
		public bool HasIcon => this.Icon is not null;

		/// <summary>
		/// The values in the group.
		/// </summary>
		public IReadOnlyList<ObservableParameter> Items { get; } = Items;
	}
}
