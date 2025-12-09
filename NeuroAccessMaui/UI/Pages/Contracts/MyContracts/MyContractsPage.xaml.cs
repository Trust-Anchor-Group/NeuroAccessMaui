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

			ViewModel.TagSelected += this.OnTagSelected;
		}

		private void OnTagSelected(MyContractsViewModel.SelectableTag tag)
		{
			try
			{
				MainThread.BeginInvokeOnMainThread(async () =>
				{
					var filterLayout = this.FindByName<HorizontalStackLayout>("FilterTagsLayout");
					var filterScroll = this.FindByName<ScrollView>("FilterTagsScroll");
					if (filterLayout is null || filterScroll is null)
						return;

					foreach (var child in filterLayout.Children)
					{
						if (child is VisualElement ve && ve.BindingContext == tag)
						{
							await filterScroll.ScrollToAsync(ve, ScrollToPosition.Center, true);
							break;
						}
					}
				});
			}
			catch (Exception Ex)
			{
				ServiceRef.LogService.LogException(Ex);
			}
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
