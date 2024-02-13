namespace NeuroAccessMaui.UI.Pages.Things.CanRead
{
	/// <summary>
	/// A page that asks the user if a remote entity is allowed to read the device.
	/// </summary>
	[XamlCompilation(XamlCompilationOptions.Compile)]
	public partial class CanReadPage
	{
		/// <summary>
		/// Creates a new instance of the <see cref="CanReadPage"/> class.
		/// </summary>
		public CanReadPage()
		{
			this.ContentPageModel = new CanReadModel();
			this.InitializeComponent();
		}
	}
}
