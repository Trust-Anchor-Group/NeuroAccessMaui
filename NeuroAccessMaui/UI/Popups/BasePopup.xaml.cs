using System.Diagnostics;
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

		/// <summary>
		/// If true, the background will be a blurred screenshot of the current page
		/// If false the background will be fully transparent and can be modified by derived classes
		/// </summary>
		protected bool useDefaultBackground;

		/// <summary>
		/// Returns the current BindingContext as a BasePopupViewModel
		/// Returns null if failed
		/// </summary>
		public BasePopupViewModel? ViewModel
		{
			get => this.BindingContext as BasePopupViewModel;
			set => this.BindingContext = value;
		}

		protected BasePopup(bool useDefaultBackground = true)
		{
			this.useDefaultBackground = useDefaultBackground;
			this.InitializeComponent();
		}

		/// <summary>
		/// A shortcut for <see cref="NeuroAccessMaui.Services.Popup.IPopupService.PopAsync"/>
		/// </summary>
		protected async void PopAsync()
		{
			await ServiceRef.PopupService.PopAsync();
		}

		protected override void OnAppearing()
		{
			if (this.useDefaultBackground)
				this.LoadScreenshotAsync();
			base.OnAppearing();
		}

		/// <summary>
		/// Called when the back button on the client is pressed
		/// </summary>
		/// <remarks>All derived methods should return base.OnBackgroundClicked()</remarks>
		/// <returns>True</returns>
		protected override bool OnBackButtonPressed()
		{
			ServiceRef.PopupService.PopAsync();
			return true;
		}

		/// <summary>
		/// Called when the background is pressed.
		/// </summary>
		/// <remarks>All derived methods should return base.OnBackgroundClicked()</remarks>
		/// <returns>True</returns>
		protected override bool OnBackgroundClicked()
		{
			ServiceRef.PopupService.PopAsync();
			return true;
		}

		/// <summary>
		/// Sets the popup background to a blurred screenshot of the current page
		/// </summary>
		private async void LoadScreenshotAsync()
		{
			this.BackgroundImageSource = await ServiceRef.ScreenshotService.TakeBlurredScreenshotAsync();
		}
	}
}
