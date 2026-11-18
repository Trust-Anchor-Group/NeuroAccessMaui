using NeuroAccessMaui.Services;

namespace NeuroAccessMaui.UI.Pages.Groups.NewGroup
{
	/// <summary>
	/// The page where a group is made, or people are added to a group.
	/// </summary>
	public partial class NewGroupPage
	{
		/// <summary>
		/// Creates the page where a group is made, or people are added to a group.
		/// </summary>
		public NewGroupPage()
		{
			this.ContentPageModel = new NewGroupViewModel(ServiceRef.NavigationService.PopLatestArgs<NewGroupNavigationArgs>());
			this.InitializeComponent();
		}
	}
}
