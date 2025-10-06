namespace NeuroAccessMaui.UI.Popups.QR
{
	public partial class ShowQRPopup : BasePopup
	{
		public ShowQRPopup(byte[] qrCodeBin)
		{
			BasePopupViewModel viewModel = new ShowQRViewModel(qrCodeBin);
			this.InitializeComponent();
			this.BindingContext = viewModel;
		}

		public ShowQRPopup()
		{
			this.InitializeComponent();
		}
	}
}
