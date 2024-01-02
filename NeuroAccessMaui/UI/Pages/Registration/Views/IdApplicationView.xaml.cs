namespace NeuroAccessMaui.UI.Pages.Registration.Views
{
	public partial class IdApplicationView
	{
		public static IdApplicationView Create()
		{
			return Create<IdApplicationView>();
		}

		public IdApplicationView(IdApplicationViewModel ViewModel)
		{
			this.InitializeComponent();
			this.ContentViewModel = ViewModel;
		}
	}
}
