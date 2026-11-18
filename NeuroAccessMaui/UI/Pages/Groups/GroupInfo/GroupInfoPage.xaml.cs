using NeuroAccessMaui.Services;

namespace NeuroAccessMaui.UI.Pages.Groups.GroupInfo
{
	/// <summary>
	/// A group's page: what the group is, who is in it, and what the user can do with it.
	/// </summary>
	public partial class GroupInfoPage
	{
		/// <summary>
		/// Creates a group's page.
		/// </summary>
		public GroupInfoPage()
		{
			this.ContentPageModel = new GroupInfoViewModel(ServiceRef.NavigationService.PopLatestArgs<GroupInfoNavigationArgs>());
			this.InitializeComponent();
		}
	}
}
