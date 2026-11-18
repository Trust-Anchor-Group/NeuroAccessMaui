namespace NeuroAccessMaui.UI.Pages.Chats
{
	/// <summary>
	/// The Chats tab: the user's chats, with contact requests at the top.
	/// </summary>
	public partial class ChatsPage
	{
		/// <summary>
		/// Creates the Chats tab.
		/// </summary>
		/// <param name="ViewModel">The view model.</param>
		public ChatsPage(ChatsViewModel ViewModel)
		{
			this.InitializeComponent();
			this.ContentPageModel = ViewModel;
		}
	}
}
