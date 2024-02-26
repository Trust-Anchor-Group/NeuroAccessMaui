using NeuroAccessMaui.Services;

namespace NeuroAccessMaui.UI.Popups
{
	/// <summary>
	/// Creates a base popup
	/// </summary>
	public partial class BasePopup
	{
		/// <summary>
		/// The requested width of the popup view
		/// </summary>
		public virtual double ViewWidthRequest => (DeviceDisplay.MainDisplayInfo.Width / DeviceDisplay.MainDisplayInfo.Density) * (7.0 / 8.0);

		/// <summary>
		/// The maximum height of the popup view
		/// </summary>
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
		/// A shortcut for <see cref="Services.UI.IUiService.PopAsync"/>
		/// </summary>
		protected static async void PopAsync()
		{
			await ServiceRef.UiService.PopAsync();
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
			ServiceRef.UiService.PopAsync();
			return true;
		}

		/// <summary>
		/// Called when the background is pressed.
		/// </summary>
		/// <remarks>All derived methods should return base.OnBackgroundClicked()</remarks>
		/// <returns>True</returns>
		protected override bool OnBackgroundClicked()
		{
			ServiceRef.UiService.PopAsync();
			return true;
		}

		/// <summary>
		/// Sets the popup background to a blurred screenshot of the current page
		/// </summary>
		private async void LoadScreenshotAsync()
		{
			this.BackgroundImageSource = await ServiceRef.UiService.TakeBlurredScreenshotAsync();
		}
	}
}
