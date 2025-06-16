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
			await Task.Delay(3000);
			MainThread.BeginInvokeOnMainThread(() =>
			{
				this.deez.CurrentCoordinate = new Waher.Runtime.Geo.GeoPosition(59.3293, 18.0686); // Stockholm, Sweden

			});
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
