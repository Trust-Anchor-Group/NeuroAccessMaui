using NeuroAccessMaui.UI.Pages;

namespace NeuroAccessMaui.UI.Pages.Notifications
{
	/// <summary>
	/// Notifications page.
	/// </summary>
	public partial class NotificationsPage : BaseContentPage
	{
		/// <summary>
		/// Initializes a new instance of the <see cref="NotificationsPage"/> class.
		/// </summary>
		/// <param name="ViewModel">View model.</param>
		public NotificationsPage(NotificationsViewModel ViewModel)
		{
			this.InitializeComponent();
			this.BindingContext = ViewModel;
		}
	}
}
