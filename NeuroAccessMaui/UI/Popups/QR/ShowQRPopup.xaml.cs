namespace NeuroAccessMaui.UI.Popups.QR
{
	public partial class ShowQRPopup : SimplePopup
	{
		public ShowQRPopup(byte[] QrCodeBin, string? QrCodeUri)
		{
			this.InitializeComponent();
			this.BindingContext = new ShowQRViewModel(QrCodeBin, QrCodeUri);
		}

		public ShowQRPopup(byte[] QrCodeBin, string? QrCodeUri, string Title)
		{
			BasePopupViewModel ViewModel = new ShowQRViewModel(QrCodeBin, QrCodeUri, Title);

			this.InitializeComponent();
			this.BindingContext = ViewModel;
		}

		public ShowQRPopup()
		{
			this.InitializeComponent();
		}
	}
}
