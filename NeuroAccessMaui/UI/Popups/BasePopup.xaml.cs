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
			ServiceRef.PopupService.PopAsync();
		}

		protected override void OnAppearing()
		{
			base.OnAppearing();
		}

		protected override void OnDisappearing()
		{
			base.OnDisappearing();
		}

		protected override bool OnBackButtonPressed()
		{
			ServiceRef.PopupService.PopAsync();
			return true;
		}

		protected override bool OnBackgroundClicked()
		{
			ServiceRef.PopupService.PopAsync();
			return true;
		}
	}
}
