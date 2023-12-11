using System.ComponentModel;
using Mopups.Pages;

namespace NeuroAccessMaui.UI.Popups.Pin
{
	/// <summary>
	/// ChangePinPopupPage defines a popup which prompts the user for their PIN.
	/// </summary>
	public partial class ChangePinPage : PopupPage
	{
		private readonly ChangePinViewModel viewModel;
		private readonly TaskCompletionSource<(string, string)> result = new();

		/// <summary>
		/// Initializes a new instance of the <see cref="ChangePinPage"/> class.
		/// </summary>
		public ChangePinPage(ChangePinViewModel ViewModel)
		{
			this.InitializeComponent();
			this.BindingContext = ViewModel;
			this.viewModel = ViewModel;

			this.UpdateMainFrameWidth();
			DeviceDisplay.MainDisplayInfoChanged += this.OnMainDisplayInfoChanged;

			ViewModel.PropertyChanged += this.OnViewModelPropertyChanged;
		}

		/// <summary>
		/// A <see cref="Task"/> waiting for the result. <c>(null, null)</c> means the dialog was closed without providing a PIN.
		/// </summary>
		public Task<(string, string)> Result => this.result.Task;

		/// <inheritdoc/>
		protected override void OnAppearing()
		{
			base.OnAppearing();
			//!!! this.OldPinEntry.Focus();
		}

		/// <inheritdoc/>
		protected override bool OnBackgroundClicked()
		{
			this.viewModel.CloseCommand.Execute(null);
			return false;
		}

		private /*async*/ void OnViewModelPropertyChanged(object? Sender, PropertyChangedEventArgs EventArgs)
		{
			//!!!
			/*
			if (EventArgs.PropertyName == nameof(this.ViewModel.PopupOpened) && !this.ViewModel.PopupOpened)
			{
				this.ViewModel.PropertyChanged -= this.OnViewModelPropertyChanged;
				DeviceDisplay.MainDisplayInfoChanged -= this.OnMainDisplayInfoChanged;

				await PopupNavigation.Instance.PopAsync();
				this.result.TrySetResult((this.ViewModel.OldPin, this.ViewModel.NewPin));
			}

			if (EventArgs.PropertyName == nameof(this.ViewModel.IncorrectPinAlertShown) && this.ViewModel.IncorrectPinAlertShown)
			{
				await this.uiSerializer.DisplayAlert(LocalizationResourceManager.Current["ErrorTitle"], LocalizationResourceManager.Current["PinIsInvalid"], LocalizationResourceManager.Current["Ok"]);
				this.ViewModel.IncorrectPinAlertShown = false;
			}
			*/
		}

		private void OnMainDisplayInfoChanged(object? Sender, DisplayInfoChangedEventArgs EventArgs)
		{
			this.UpdateMainFrameWidth();
		}

		private void UpdateMainFrameWidth()
		{
			//!!! this.MainFrame.WidthRequest = 0.75 * DeviceDisplay.MainDisplayInfo.Width / DeviceDisplay.MainDisplayInfo.Density;
		}

		private void PopupPage_BackgroundClicked(object? sender, EventArgs e)
		{

	    }
	}
}
