
namespace NeuroAccessMaui.UI.Pages.Registration.Views
{
	public partial class FinalizeView
	{
		public static FinalizeView Create()
		{
			return Create<FinalizeView>();
		}

		public FinalizeView(FinalizeViewModel ViewModel)
		{
			this.InitializeComponent();
			this.ContentViewModel = ViewModel;
		}
	}
}
