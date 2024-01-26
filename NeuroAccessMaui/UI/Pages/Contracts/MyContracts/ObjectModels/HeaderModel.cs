using CommunityToolkit.Mvvm.ComponentModel;

namespace NeuroAccessMaui.UI.Pages.Contracts.MyContracts.ObjectModels
{
	/// <summary>
	/// The data model for a contract category header containing a collection of contracts of the same category.
	/// </summary>
	/// <param name="Category">Contract category</param>
	/// <param name="Contracts">Contracts in category.</param>
	/// <param name="NrEvents">Number of events</param>
	public class HeaderModel(string Category, ICollection<ContractModel> Contracts, int NrEvents) : ObservableObject, IUniqueItem
	{
		private int nrEvents = NrEvents;
		private bool expanded;

		/// <summary>
		/// Displayable category name for the contracts.
		/// </summary>
		public string Category { get; } = Category;

		/// <inheritdoc/>
		public string UniqueName => this.Category;

		/// <summary>
		/// Number of events.
		/// </summary>
		public int NrEvents
		{
			get => this.nrEvents;
			set
			{
				if (this.nrEvents != value)
				{
					bool b1 = this.nrEvents > 0;
					bool b2 = value > 0;

					this.nrEvents = value;
					this.OnPropertyChanged(nameof(this.NrEvents));

					if (b1 ^ b2)
						this.OnPropertyChanged(nameof(this.HasEvents));
				}
			}
		}

		/// <summary>
		/// If the category contains contracts with events.
		/// </summary>
		public bool HasEvents => this.nrEvents > 0;

		/// <summary>
		/// The category's contracts.
		/// </summary>
		public ICollection<ContractModel> Contracts { get; } = Contracts;

		/// <summary>
		/// Number of contracts in category.
		/// </summary>
		public int NrContracts => this.Contracts.Count;

		/// <summary>
		/// If the group is expanded or not.
		/// </summary>
		public bool Expanded
		{
			get => this.expanded;
			set
			{
				if (this.expanded != value)
				{
					this.expanded = value;
					this.OnPropertyChanged(nameof(this.Symbol));
				}
			}
		}

		/// <summary>
		/// Symbol used for the category.
		/// </summary>
		public string Symbol => this.expanded ? "🗁" : "🗀";  // TODO: SVG symbols
	}
}
