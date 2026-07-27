using CommunityToolkit.Maui.Layouts;

namespace NeuroAccessMaui.UI.Pages.Contracts.ViewContract
{
	/// <summary>
	/// A page that displays a specific contract.
	/// </summary>
	[XamlCompilation(XamlCompilationOptions.Compile)]
	public partial class ViewContractPage
	{
		/// <summary>
		/// Creates a new instance of the <see cref="ViewContractPage"/> class.
		/// </summary>
		public ViewContractPage(ViewContractViewModel ViewModel)
		{
			this.InitializeComponent();

			StateContainer.SetCurrentState(this.StateGrid, ViewContractStep.Loading.ToString());
			ViewModel.StateObject = this.StateGrid;
			this.BindingContext = ViewModel;
		}

	}
}
