using NeuroAccessMaui.UI.Pages.Contracts.MyContracts.ObjectModels;

namespace NeuroAccessMaui.UI.Pages.Contracts.MyContracts
{
	/// <summary>
	/// Data Template Selector, based on Item Type.
	/// </summary>
	public class ItemTypeTemplateSelector : DataTemplateSelector
	{
		/// <summary>
		/// Template to use for contract header
		/// </summary>
		public DataTemplate HeaderTemplate { get; set; }

		/// <summary>
		/// Template to use for contracts
		/// </summary>
		public DataTemplate ContractTemplate { get; set; }

		/// <summary>
		/// Template to use for standalone notification events.
		/// </summary>
		public DataTemplate EventTemplate { get; set; }

		/// <summary>
		/// Template to use for other items.
		/// </summary>
		public DataTemplate DefaultTemplate { get; set; }

		/// <inheritdoc/>
		protected override DataTemplate OnSelectTemplate(object item, BindableObject container)
		{
			if (item is HeaderModel)
				return this.HeaderTemplate ?? this.DefaultTemplate;
			else if (item is ContractModel)
				return this.ContractTemplate ?? this.DefaultTemplate;
			else if (item is EventModel)
				return this.EventTemplate ?? this.DefaultTemplate;
			else
				return this.DefaultTemplate;
		}
	}
}
