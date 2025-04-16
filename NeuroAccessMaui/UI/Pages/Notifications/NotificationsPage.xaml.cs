namespace NeuroAccessMaui.UI.Pages.Notifications
{
	public partial class NotificationsPage : BaseContentPage
	{
		public NotificationsPage(NotificationsViewModel ViewModel)
		{
			this.InitializeComponent();
			this.BindingContext = ViewModel;
		}
	}

}
