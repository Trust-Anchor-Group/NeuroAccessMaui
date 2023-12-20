namespace NeuroAccessMaui.UI.Pages.Registration.Views
{
	public partial class LoadingView
	{
		public static LoadingView Create()
		{
			return Create<LoadingView>();
		}

		public LoadingView(LoadingViewModel ViewModel)
		{
			this.InitializeComponent();
			this.ContentViewModel = ViewModel;
		}
	}
}
