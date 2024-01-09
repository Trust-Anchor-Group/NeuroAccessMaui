namespace NeuroAccessMaui.UI.Pages.Main
{
	public partial class MainPage
	{
		public MainPage(MainViewModel ViewModel)
		{
			this.InitializeComponent();
			this.ContentPageModel = ViewModel;
		}

		private async void SwipeGestureRecognizer_Swiped(object sender, SwipedEventArgs e)
		{
			await MainViewModel.ViewId();
		}
	}
}
