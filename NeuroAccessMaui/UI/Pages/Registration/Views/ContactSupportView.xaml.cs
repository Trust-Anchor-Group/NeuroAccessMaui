using CommunityToolkit.Mvvm.Input;

namespace NeuroAccessMaui.UI.Pages.Registration.Views
{
	public partial class ContactSupportView
	{
		public static ContactSupportView Create()
		{
			return Create<ContactSupportView>();
		}
		public ContactSupportView(ContactSupportViewModel ViewModel)
		{
			this.InitializeComponent();
			this.ContentViewModel = ViewModel;
		}


	}
}
