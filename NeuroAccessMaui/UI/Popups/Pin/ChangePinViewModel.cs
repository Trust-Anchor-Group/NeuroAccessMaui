using CommunityToolkit.Mvvm.ComponentModel;
using CommunityToolkit.Mvvm.Input;
using NeuroAccessMaui.Services;
using NeuroAccessMaui.Services.Tag;

namespace NeuroAccessMaui.UI.Popups.Pin
{
	public partial class ChangePinViewModel : ObservableObject
	{
		private bool popupOpened = true;
		private bool incorrectPinAlertShown = false;

		private string oldPin = string.Empty;
		private bool oldPinFocused = true;

		private string newPin = string.Empty;
		private bool enteringNewPinStarted = false;

		private string retypedNewPin = string.Empty;
		private bool enteringRetypedNewPinStarted = false;

		public ChangePinViewModel()
		{
		}

		public bool CanTryChangePin => this.NewPinStrength == PinStrength.Strong && this.NewPinMatchesRetypedNewPin;

		public bool PopupOpened
		{
			get => this.popupOpened;
			set => this.SetProperty(ref this.popupOpened, value);
		}

		public bool IncorrectPinAlertShown
		{
			get => this.incorrectPinAlertShown;
			set => this.SetProperty(ref this.incorrectPinAlertShown, value);
		}

		public string OldPin
		{
			get => this.oldPin;
			set => this.SetProperty(ref this.oldPin, value);
		}

		public bool OldPinFocused
		{
			get => this.oldPinFocused;
			set => this.SetProperty(ref this.oldPinFocused, value);
		}

		public string NewPin
		{
			get => this.newPin;
			set => this.SetProperty(ref this.newPin, value);
		}

		public bool EnteringNewPinStarted
		{
			get => this.enteringNewPinStarted;
			set => this.SetProperty(ref this.enteringNewPinStarted, value);
		}

		public string RetypedNewPin
		{
			get => this.retypedNewPin;
			set => this.SetProperty(ref this.retypedNewPin, value);
		}

		public bool EnteringRetypedNewPinStarted
		{
			get => this.enteringRetypedNewPinStarted;
			set => this.SetProperty(ref this.enteringRetypedNewPinStarted, value);
		}

		public PinStrength NewPinStrength => ServiceRef.TagProfile.ValidatePinStrength(this.NewPin);

		public bool NewPinMatchesRetypedNewPin => string.IsNullOrEmpty(this.NewPin) ? string.IsNullOrEmpty(this.RetypedNewPin) : string.Equals(this.NewPin, this.RetypedNewPin, StringComparison.Ordinal);

		//!!!
		/*
		protected override void OnPropertyChanged([CallerMemberName] string? PropertyName = "")
		{
			base.OnPropertyChanged(PropertyName);

			if (PropertyName == nameof(this.NewPin))
			{
				// This somewhat complicated condition ensures that switching from null to an empty string does not count as EnteringNewPinStarted.
				if (!this.EnteringNewPinStarted && !string.IsNullOrEmpty(this.NewPin))
				{
					this.EnteringNewPinStarted = true;
				}

				this.OnPropertyChanged(nameof(this.NewPinStrength));
			}

			if (PropertyName == nameof(this.RetypedNewPin))
			{
				// This somewhat complicated condition ensures that switching from null to an empty string does not count as EnteringRetypedNewPinStarted.
				if (!this.EnteringRetypedNewPinStarted && !string.IsNullOrEmpty(this.RetypedNewPin))
				{
					this.EnteringRetypedNewPinStarted = true;
				}
			}

			if (PropertyName == nameof(this.NewPin) || PropertyName == nameof(this.RetypedNewPin))
			{
				this.OnPropertyChanged(nameof(this.NewPinMatchesRetypedNewPin));
				this.TryChangePinCommand.ChangeCanExecute();
			}

			if (PropertyName == nameof(this.IncorrectPinAlertShown) && !this.IncorrectPinAlertShown)
			{
				this.OldPin = string.Empty;
				this.OldPinFocused = true;
			}
		}
		*/

		[RelayCommand(CanExecute = nameof(CanTryChangePin))]
		private void TryChangePin()
		{
			ITagProfile TagProfile = ServiceRef.TagProfile;

			if (TagProfile.HasPin && (TagProfile.ComputePinHash(this.OldPin) != TagProfile.PinHash))
			{
				this.IncorrectPinAlertShown = true;
			}
			else
			{
				this.PopupOpened = false;
			}
		}

		[RelayCommand]
		private void Close()
		{
			// We need to set OldPin and NewPin to null to indicate that the popup has been dismissed by the user. However, if we do not
			// reset EnteringNewPinStarted and EnteringRetypedNewPinStarted as well, error messages might appear or change, which leads
			// to a visual distortion. Resetting everything leads to a nice visual "reset and close" effect.

			this.OldPin = null;

			this.EnteringNewPinStarted = false;
			this.NewPin = null;

			this.EnteringRetypedNewPinStarted = false;
			this.RetypedNewPin = null;

			this.PopupOpened = false;
		}
	}
}
