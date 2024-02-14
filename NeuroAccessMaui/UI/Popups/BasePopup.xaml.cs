using Mopups.Pages;
using Mopups.PreBaked.PopupPages.SingleResponse;
using NeuroAccessMaui.Services;
using Waher.Networking.XMPP.Contracts.Search;

namespace NeuroAccessMaui.UI.Popups
{
	/// <summary>
	/// Creates a base popup
	/// </summary>
	public partial class BasePopup
	{
		public virtual double ViewWidthRequest => (DeviceDisplay.MainDisplayInfo.Width / DeviceDisplay.MainDisplayInfo.Density) * (7.0 / 8.0);
		public virtual double MaximumViewHeightRequest => (DeviceDisplay.MainDisplayInfo.Height / DeviceDisplay.MainDisplayInfo.Density) * (3.0 / 4.0);

		public BasePopupViewModel? ViewModel
		{
			get => this.BindingContext as BasePopupViewModel;
			set => this.BindingContext = value ?? throw new ArgumentNullException(nameof(value), @"ViewModel cannot be null");
		}

		protected BasePopup(ImageSource? Background)
		{
			this.InitializeComponent();

			if (Background is not null)
				this.BackgroundImageSource = Background;
			else
				this.BackgroundColor = Color.FromInt(0x20000000);
		}

		protected void ClosePopup()
		{
			ServiceRef.PopupService.PopPopupAsync();
		}

		protected override bool OnBackButtonPressed()
		{
			this.ViewModel?.Close();
			return base.OnBackButtonPressed();
		}
		protected override bool OnBackgroundClicked()
		{
			this.ViewModel?.Close();
			return base.OnBackgroundClicked();
		}

		protected override void OnDisappearing()
		{
			this.ViewModel?.Close();
			base.OnDisappearing();
		}
	}
}
