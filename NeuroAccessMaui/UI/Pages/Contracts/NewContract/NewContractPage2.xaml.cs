using CommunityToolkit.Maui.Layouts;
using NeuroAccessMaui.Services;
using Waher.Persistence;

namespace NeuroAccessMaui.UI.Pages.Contracts.NewContract
{
	/// <summary>
	/// A page that allows the user to create a new contract.
	/// </summary>
	[XamlCompilation(XamlCompilationOptions.Compile)]
	public partial class NewContractPage2 : IContractOptionsPage
	{
		/// <summary>
		/// Helper property to access the view model.
		/// </summary>
		public NewContractViewModel2? ViewModel => this.ContentPageModel as NewContractViewModel2;

		/// <summary>
		/// Creates a new instance of the <see cref="NewContractPage"/> class.
		/// </summary>
		public NewContractPage2()
		{
			this.ContentPageModel = new NewContractViewModel2(this, ServiceRef.UiService.PopLatestArgs<NewContractNavigationArgs>());
			this.InitializeComponent();

			if(this.ViewModel is NewContractViewModel2 ViewModel)
			{
				ViewModel.StateObject = this.StateGrid;
			}
			else
			{
				ServiceRef.LogService.LogAlert("NewContractPage2,ViewModel is not of type NewContractViewModel2. Cannot set StateObject.");
			}

			StateContainer.SetCurrentState(this.StateGrid, NewContractStep.Loading.ToString());
		}


		/// <summary>
		/// Method called (from main thread) when contract options are made available.
		/// </summary>
		/// <param name="Options">Available options, as dictionaries with contract parameters.</param>
		public async Task ShowContractOptions(IDictionary<CaseInsensitiveString, object>[] Options)
		{
			if (this.ContentPageModel is NewContractViewModel ViewModel)
				await ViewModel.ShowContractOptions(Options);
		}

	}
}
