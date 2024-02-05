namespace NeuroAccessMaui.UI.Pages.Things.ViewClaimThing
{
	/// <summary>
	/// A page that displays a specific claim thing.
	/// </summary>
	[XamlCompilation(XamlCompilationOptions.Compile)]
	public partial class ViewClaimThingPage
	{
		/// <summary>
		/// Creates a new instance of the <see cref="ViewClaimThingPage"/> class.
		/// </summary>
		public ViewClaimThingPage()
		{
			this.ViewModel = new ViewClaimThingViewModel();
			this.InitializeComponent();
		}
	}
}
