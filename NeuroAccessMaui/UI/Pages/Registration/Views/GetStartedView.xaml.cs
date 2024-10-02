using CommunityToolkit.Mvvm.Input;

namespace NeuroAccessMaui.UI.Pages.Registration.Views
{
	public partial class GetStartedView
	{
		public static GetStartedView Create()
		{
			return Create<GetStartedView>();
		}
		public GetStartedView(GetStartedViewModel ViewModel)
		{
			this.InitializeComponent();
			this.ContentViewModel = ViewModel;
		}


	}
}
