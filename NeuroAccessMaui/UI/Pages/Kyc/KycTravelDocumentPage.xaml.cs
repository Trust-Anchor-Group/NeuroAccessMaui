namespace NeuroAccessMaui.UI.Pages.Kyc
{
	/// <summary>
	/// Page for entering travel-document MRZ evidence used by the KYC NFC readout path.
	/// </summary>
	public partial class KycTravelDocumentPage : BaseContentPage
	{
		/// <summary>
		/// Initializes a new instance of the <see cref="KycTravelDocumentPage"/> class.
		/// </summary>
		/// <param name="ViewModel">The page view model.</param>
		public KycTravelDocumentPage(KycTravelDocumentViewModel ViewModel)
		{
			this.InitializeComponent();
			this.ContentPageModel = ViewModel;
		}
	}
}
