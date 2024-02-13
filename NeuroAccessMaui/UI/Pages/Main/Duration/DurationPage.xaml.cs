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
		/// <param name="ViewModel">View model.</param>
		public DurationPage(DurationViewModel ViewModel)
		{
			this.InitializeComponent();
			this.ContentPageModel = ViewModel;
		}
	}
}
