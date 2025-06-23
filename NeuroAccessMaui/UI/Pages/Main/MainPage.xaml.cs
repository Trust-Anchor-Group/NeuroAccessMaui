using NeuroAccessMaui.Services;

namespace NeuroAccessMaui.UI.Pages.Main
{
	public partial class MainPage
	{
		public MainPage(MainViewModel ViewModel)
		{
			this.InitializeComponent();
			this.ContentPageModel = ViewModel;
		}

		protected override async Task OnAppearingAsync()
		{
			await base.OnAppearingAsync();

			
		}

		private async void SwipeGestureRecognizer_Swiped(object sender, SwipedEventArgs e)
		{
			try
			{
				if(this.ContentPageModel is MainViewModel ViewModel)
					await ViewModel.ViewId();
			}
			catch (Exception ex)
			{
				ServiceRef.LogService.LogException(ex);
			}
		}

		private void OnFlyoutButtonClicked(object sender, EventArgs e)
		{
			Shell.Current.FlyoutIsPresented = true;
		}
	}
}
