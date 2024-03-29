﻿using NeuroAccessMaui.Services;

namespace NeuroAccessMaui.UI.Pages.Wallet.RequestPayment
{
	/// <summary>
	/// A page that displays information about eDaler received.
	/// </summary>
	[XamlCompilation(XamlCompilationOptions.Compile)]
	public partial class RequestPaymentPage
	{
		/// <summary>
		/// Creates a new instance of the <see cref="RequestPaymentPage"/> class.
		/// </summary>
		public RequestPaymentPage()
		{
			this.ContentPageModel = new RequestPaymentViewModel(this, ServiceRef.UiService.PopLatestArgs<EDalerBalanceNavigationArgs>());
			this.InitializeComponent();
		}

		/// <summary>
		/// Scrolls to display the QR-code.
		/// </summary>
		public async Task ShowQrCode()
		{
			await this.ScrollView.ScrollToAsync(this.ShareExternalButton, ScrollToPosition.MakeVisible, true);
		}
	}
}
