using NeuroAccessMaui.Services;

namespace NeuroAccessMaui.UI.Pages.Main.Duration
{
	/// <summary>
	/// A page that allows the user to duration the value of a numerical input field.
	/// </summary>
	[XamlCompilation(XamlCompilationOptions.Compile)]
	public partial class DurationPage
	{
		/// <summary>
		/// A page that allows the user to duration the value of a numerical input field.
		/// </summary>
		public DurationPage()
		{
			this.InitializeComponent();
			this.ContentPageModel = new DurationViewModel(ServiceRef.UiService.PopLatestArgs<DurationNavigationArgs>());
		}
	}
}
