namespace NeuroAccessMaui.UI.Pages.Applications
{
	/// <summary>
	/// Page allowing the user to apply for a Personal ID.
	/// </summary>
	public partial class ApplyIdPage
	{
		/// <summary>
		/// Creates a new instance of the <see cref="ApplyIdPage"/> class.
		/// </summary>
		/// <param name="ViewModel">View model.</param>
		public ApplyIdPage(ApplyIdViewModel ViewModel)
		{
			this.InitializeComponent();
			this.ContentPageModel = ViewModel;
		}
	}
}
