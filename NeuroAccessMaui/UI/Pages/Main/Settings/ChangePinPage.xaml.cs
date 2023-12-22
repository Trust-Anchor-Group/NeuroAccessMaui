namespace NeuroAccessMaui.UI.Pages.Main.Settings
{
	/// <summary>
	/// Allows the user to change its PIN.
	/// </summary>
	public partial class ChangePinPage
	{
		/// <summary>
		/// Initializes a new instance of the <see cref="ChangePinPage"/> class.
		/// </summary>
		/// <param name="ViewModel">View model.</param>
		public ChangePinPage(ChangePinViewModel ViewModel)
			: base()
		{
			this.InitializeComponent();
			this.ContentPageModel = ViewModel;
		}
	}
}
