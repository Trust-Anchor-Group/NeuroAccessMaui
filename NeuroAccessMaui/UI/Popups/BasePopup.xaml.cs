using System.Runtime.CompilerServices;
using NeuroAccessMaui.Services;
using NeuroAccessMaui.UI.Pages;
using Waher.Events;

namespace NeuroAccessMaui.UI.Popups
{
	/// <summary>
	/// Creates a base popup with view model lifecycle support.
	/// </summary>
	public partial class BasePopup : Mopups.Pages.PopupPage
	{
		// Define the bindable property for custom content.
		public static readonly BindableProperty CustomContentProperty =
			BindableProperty.Create(
				nameof(CustomContent),
				typeof(View),
				typeof(BasePopup),
				default(View),
				propertyChanged: OnCustomContentChanged);


		/// <summary>
		/// Gets or sets the custom content that will be injected into the base popup layout.
		/// </summary>
		public View CustomContent
		{
			get => (View)this.GetValue(CustomContentProperty);
			set => this.SetValue(CustomContentProperty, value);
		}


		private static void OnCustomContentChanged(BindableObject bindable, object oldValue, object newValue)
		{
			if (bindable is BasePopup BasePopup)
			{
				// Update the ContentPresenter named ContentSlot with the new view.
				BasePopup.ContentSlot.Content = newValue as View;
			}
		}


		/// <summary>
		/// The requested width of the popup view.
		/// </summary>
		public virtual double ViewWidthRequest => (DeviceDisplay.MainDisplayInfo.Width / DeviceDisplay.MainDisplayInfo.Density) * (7.0 / 8.0);

		/// <summary>
		/// The maximum height of the popup view.
		/// </summary>
		public virtual double MaximumViewHeightRequest => (DeviceDisplay.MainDisplayInfo.Height / DeviceDisplay.MainDisplayInfo.Density) * (3.0 / 4.0);

		/// <summary>
		/// If true, the background will be a blurred screenshot of the current page.
		/// If false the background will be fully transparent and can be modified by derived classes.
		/// </summary>
		protected bool useDefaultBackground;

		/// <summary>
		/// Returns the current BindingContext as a BasePopupViewModel.
		/// Returns null if failed.
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
			this.BackgroundColor = AppColors.PrimaryBackground;

			// Create a tap gesture recognizer that calls OnBackgroundClicked when tapped.
			TapGestureRecognizer TapBackground = new TapGestureRecognizer();
			TapBackground.Tapped += (s, e) => this.OnBackgroundClicked();
			this.CustomBackgroundImage.GestureRecognizers.Add(TapBackground);
		}

		/// <summary>
		/// Called when the Page's <see cref="Element.Parent"/> property has changed.
		/// </summary>
		protected sealed override async void OnParentSet()
		{
			try
			{
				base.OnParentSet();

				if (this.BindingContext is not BaseViewModel Vm)
					return;

				if (this.Parent is null)
				{
					if (this.ViewModel is ILifeCycleView LifeCycleView)
						await LifeCycleView.OnDisposeAsync();
				}
				else
				{
					if (this.ViewModel is ILifeCycleView LifeCycleView)
						await LifeCycleView.OnInitializeAsync();
				}
			}
			catch (Exception Ex)
			{
				Log.Exception(Ex);
			}
		}

		/// <summary>
		/// Called when the popup appears.
		/// </summary>
		protected override async void OnAppearing()
		{
			try
			{
				base.OnAppearing();

				// Call the view model's appearing logic.
				if (this.BindingContext is BaseViewModel Vm)
				{
					await Vm.OnAppearingAsync();
				}

				if (this.useDefaultBackground)
				{
					await this.LoadScreenshotAsync();
				}
			}
			catch (Exception Ex)
			{
				ServiceRef.LogService.LogException(Ex);
			}
		}

		/// <summary>
		/// Called when the popup is disappearing.
		/// </summary>
		protected override async void OnDisappearing()
		{
			try
			{
				// Call the view model's disappearing logic.
				if (this.BindingContext is BasePopupViewModel Vm)
				{
					await Vm.OnDisappearingAsync();
				}

				base.OnDisappearing();
			}
			catch (Exception Ex)
			{
				ServiceRef.LogService.LogException(Ex);
			}
		}

		/// <summary>
		/// A shortcut for <see cref="Services.UI.IUiService.PopAsync"/>.
		/// </summary>
		protected static async Task PopAsync()
		{
			await ServiceRef.UiService.PopAsync();
		}

		/// <summary>
		/// Called when the back button on the client is pressed.
		/// </summary>
		protected override bool OnBackButtonPressed()
		{
			ServiceRef.UiService.PopAsync();
			return true;
		}

		/// <summary>
		/// Called when the background is pressed.
		/// </summary>
		protected override bool OnBackgroundClicked()
		{
			if(this.CloseWhenBackgroundIsClicked)
				ServiceRef.UiService.PopAsync();
			return true;
		}

		/// <summary>
		/// Sets the popup background to a blurred screenshot of the current page.
		/// </summary>
		private async Task LoadScreenshotAsync()
		{
			// Load the screenshot and assign it to the bindable property.
			this.CustomBackgroundImage.Source = await ServiceRef.UiService.TakeBlurredScreenshotAsync();
		}
	}
}
