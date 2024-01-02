namespace NeuroAccessMaui.UI.Pages.Applications
{
	/// <summary>
	/// Page allowing the user to apply for a Personal ID.
	/// </summary>
	public partial class ApplyPersonalIdPage
	{
		/// <summary>
		/// Creates a new instance of the <see cref="ApplyPersonalIdPage"/> class.
		/// </summary>
		/// <param name="ViewModel">View model.</param>
		public ApplyPersonalIdPage(ApplyPersonalIdViewModel ViewModel)
		{
			this.InitializeComponent();
			this.ContentPageModel = ViewModel;
		}
	}
}
