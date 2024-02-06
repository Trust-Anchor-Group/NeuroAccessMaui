using NeuroAccessMaui.Services;
using NeuroAccessMaui.UI.Pages.Contracts.MyContracts.ObjectModels;

namespace NeuroAccessMaui.UI.Pages.Contracts.MyContracts
{
	/// <summary>
	/// A page that displays a list of the current user's contracts.
	/// </summary>
	[XamlCompilation(XamlCompilationOptions.Compile)]
	public partial class MyContractsPage
	{
		/// <summary>
		/// Creates a new instance of the <see cref="MyContractsPage"/> class.
		/// </summary>
		public MyContractsPage()
		{
			MyContractsViewModel ViewModel = new();
			this.Title = ViewModel.Title;
			this.ContentPageModel = ViewModel;

			this.InitializeComponent();
		}

		private void ContractsSelectionChanged(object? Sender, SelectionChangedEventArgs e)
		{
			if (this.ContentPageModel is MyContractsViewModel MyContractsViewModel)
			{
				object SelectedItem = this.Contracts.SelectedItem;

				if (SelectedItem is HeaderModel Category)
				{
					Category.Expanded = !Category.Expanded;
					MyContractsViewModel.AddOrRemoveContracts(Category, Category.Expanded);
				}
				else if (SelectedItem is ContractModel Contract)
				{
					MyContractsViewModel.ContractSelected(Contract.ContractId);

					if (Contract.HasEvents)
						ServiceRef.NotificationService.DeleteEvents(Contract.Events);
				}
				else if (SelectedItem is EventModel Event)
					Event.Clicked();

				this.Contracts.SelectedItem = null;
			}
		}
	}
}
