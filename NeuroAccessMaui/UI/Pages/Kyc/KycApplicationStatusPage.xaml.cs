namespace NeuroAccessMaui.UI.Pages.Kyc
{
	/// <summary>
	/// Read-only page that shows the current KYC application status.
	/// </summary>
	public partial class KycApplicationStatusPage : BaseContentPage
	{
		/// <summary>
		/// Initializes a new instance of the <see cref="KycApplicationStatusPage"/> class.
		/// </summary>
		/// <param name="ViewModel">Page view model.</param>
		public KycApplicationStatusPage(KycApplicationStatusViewModel ViewModel)
		{
			this.InitializeComponent();
			this.ContentPageModel = ViewModel;
		}
	}
}
