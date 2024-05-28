
namespace NeuroAccessMaui.UI.Pages.Registration.Views
{
	public partial class DefinePasswordView
	{
		public static DefinePasswordView Create()
		{
			return Create<DefinePasswordView>();
		}

		public DefinePasswordView(DefinePasswordViewModel ViewModel)
		{
			this.InitializeComponent();
			this.ContentViewModel = ViewModel;
		}
	}
}
