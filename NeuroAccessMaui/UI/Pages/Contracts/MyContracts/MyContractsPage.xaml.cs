using System.Collections.ObjectModel;
using CommunityToolkit.Mvvm.Input;
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
			MyContractsViewModel ViewModel = new(ServiceRef.NavigationService.PopLatestArgs<MyContractsNavigationArgs>());
			this.ContentPageModel = ViewModel;

			this.InitializeComponent();
		}

		private void ContractsSearchChanged(object? Sender, TextChangedEventArgs e)
		{
			try
			{
				MainThread.BeginInvokeOnMainThread(() =>
				{
					if (this.ContentPageModel is MyContractsViewModel MyContractsViewModel)
					{
						MyContractsViewModel.UpdateSearch(e.NewTextValue);
					}
				});
			}
			catch (Exception Ex)
			{
				ServiceRef.LogService.LogException(Ex);
			}
		}
	}
}
