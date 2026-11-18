namespace NeuroAccessMaui.UI.Pages.Contacts.MyCard
{
	/// <summary>
	/// The page where the user sets up their own card: the name and photo others see, and which of them to share.
	/// </summary>
	public partial class MyCardPage
	{
		/// <summary>
		/// Creates the page.
		/// </summary>
		public MyCardPage()
		{
			this.ContentPageModel = new MyCardViewModel();
			this.InitializeComponent();
		}
	}
}
