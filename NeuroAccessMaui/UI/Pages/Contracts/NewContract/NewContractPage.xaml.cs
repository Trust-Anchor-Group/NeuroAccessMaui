using CommunityToolkit.Maui.Layouts;
using CommunityToolkit.Mvvm.Messaging;
using NeuroAccessMaui.Services;
using Waher.Persistence;

namespace NeuroAccessMaui.UI.Pages.Contracts.NewContract
{
	/// <summary>
	/// A page that allows the user to create a new contract.
	/// </summary>
	[XamlCompilation(XamlCompilationOptions.Compile)]
	public partial class NewContractPage : IContractOptionsPage
	{

		/// <summary>
		/// Creates a new instance of the <see cref="NewContractPage"/> class.
		/// </summary>
		public NewContractPage(BaseViewModel ViewModel)
		{
			this.BindingContext = ViewModel;
			this.InitializeComponent();

			if (this.BindingContext is NewContractViewModel VM)
			{
				VM.StateObject.SetBinding(;
			}
			else
			{
				ServiceRef.LogService.LogAlert("NewContractPage,ViewModel is not of type NewContractViewModel. Cannot set StateObject.");
			}

			StateContainer.SetCurrentState(this.StateGrid, NewContractStep.Loading.ToString());

			WeakReferenceMessenger.Default.Register<ContractOptionsMessage>(this, async (sender, message) =>
			{
				await this.ShowContractOptions(message.Options);
			});
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
