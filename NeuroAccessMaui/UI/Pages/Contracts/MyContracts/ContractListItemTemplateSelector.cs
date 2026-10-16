using NeuroAccessMaui.UI.Pages.Contracts.MyContracts.ObjectModels;

namespace NeuroAccessMaui.UI.Pages.Contracts.MyContracts
{
	/// <summary>
	/// Chooses the row template in the lists of contracts: a contract, a template, or a request to see a contract.
	/// </summary>
	public class ContractListItemTemplateSelector : DataTemplateSelector
	{
		/// <summary>
		/// Template for a contract row.
		/// </summary>
		public DataTemplate? ContractTemplate { get; set; }

		/// <summary>
		/// Template for a template row.
		/// </summary>
		public DataTemplate? TemplateRowTemplate { get; set; }

		/// <summary>
		/// Template for a contract-request row.
		/// </summary>
		public DataTemplate? PetitionTemplate { get; set; }

		/// <inheritdoc/>
		protected override DataTemplate? OnSelectTemplate(object Item, BindableObject Container)
		{
			return Item switch
			{
				ContractPetitionModel => this.PetitionTemplate,
				ContractModel Model when Model.ContractRef.IsTemplate => this.TemplateRowTemplate,
				_ => this.ContractTemplate
			};
		}
	}
}
