using System.Collections.ObjectModel;
using System.ComponentModel;

namespace NeuroAccessMaui.UI.Pages.Contracts.MyContracts.ObjectModels
{
	/// <summary>
	/// The data model for a contract category containing a set of contracts of the same category.
	/// </summary>
	/// <param name="Category">Contract category</param>
	/// <param name="Contracts">Contracts in category.</param>
	public class ContractCategoryModel(string Category, ICollection<ContractModel> Contracts) : ObservableCollection<ContractModel>, INotifyPropertyChanged
	{
		private bool expanded;

		/// <summary>
		/// Displayable category for the contracts.
		/// </summary>
		public string Category { get; } = Category;

		/// <summary>
		/// Displayable category for the contracts.
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

					this.OnPropertyChanged(new PropertyChangedEventArgs(nameof(this.Expanded)));
					this.OnPropertyChanged(new PropertyChangedEventArgs(nameof(this.Symbol)));

					if (this.expanded)
					{
						foreach (ContractModel Contract in this.Contracts)
							this.Add(Contract);
					}
					else
						this.Clear();
				}
			}
		}

		/// <summary>
		/// Symbol used for the category.
		/// </summary>
		public string Symbol => this.expanded ? "🗁" : "🗀";	// TODO: SVG symbols
	}
}
