using CommunityToolkit.Mvvm.ComponentModel;
using CommunityToolkit.Mvvm.Input;
using DevStorybook.FakeServices;
using DevStorybook.Services;
using DevStorybook.Stories;
using NeuroAccessMaui.Resources.Languages;
using NeuroAccessMaui.Services.Data;

namespace DevStorybook.ViewModels
{
	/// <summary>
	/// Supplies controlled UI state and fake dependencies to a production Phone Verification view.
	/// </summary>
	public partial class StoryViewerViewModel : ObservableObject
	{
		private static readonly ISO_3166_Country unitedStates = ISO_3166_1.Countries.First(Country => Country.Alpha2 == "US");
		private static readonly ISO_3166_Country sweden = ISO_3166_1.Countries.First(Country => Country.Alpha2 == "SE");

		private readonly IPhoneVerificationService phoneVerificationService;
		private PhoneVerificationOutcome outcome = PhoneVerificationOutcome.Success;

		[ObservableProperty]
		private string title = string.Empty;

		[ObservableProperty]
		private string subtitle = string.Empty;

		[ObservableProperty]
		private string storyTypeLabel = string.Empty;

		[ObservableProperty]
		private string automationId = "storybook_page_story";

		[ObservableProperty]
		private bool isComponentStory;

		[ObservableProperty]
		private ISO_3166_Country selectedCountry = unitedStates;

		[ObservableProperty]
		[NotifyPropertyChangedFor(nameof(ShowValidationError))]
		[NotifyPropertyChangedFor(nameof(LocalizedValidationError))]
		[NotifyPropertyChangedFor(nameof(CanSendCode))]
		private string phoneNumber = string.Empty;

		[ObservableProperty]
		[NotifyPropertyChangedFor(nameof(ShowValidationError))]
		[NotifyPropertyChangedFor(nameof(LocalizedValidationError))]
		[NotifyPropertyChangedFor(nameof(CanSendCode))]
		private bool typeIsValid = true;

		[ObservableProperty]
		[NotifyPropertyChangedFor(nameof(ShowValidationError))]
		[NotifyPropertyChangedFor(nameof(LocalizedValidationError))]
		[NotifyPropertyChangedFor(nameof(CanSendCode))]
		private bool lengthIsValid = true;

		[ObservableProperty]
		private bool isPhoneReadOnly;

		[ObservableProperty]
		[NotifyPropertyChangedFor(nameof(CanSendCode))]
		private bool isBusy;

		[ObservableProperty]
		private bool isScreenStory;

		[ObservableProperty]
		private bool isCountryPickerStory;

		[ObservableProperty]
		private bool isPhoneInputStory;

		[ObservableProperty]
		private bool isComponentEnabled = true;

		[ObservableProperty]
		private bool shouldFocusInput;

		[ObservableProperty]
		private bool isCountrySelectionVisible;

		[ObservableProperty]
		[NotifyPropertyChangedFor(nameof(HasStatus))]
		private string statusMessage = string.Empty;

		[ObservableProperty]
		private Color statusColor = Colors.Transparent;

		/// <summary>
		/// Initializes a new instance of the <see cref="StoryViewerViewModel"/> class.
		/// </summary>
		/// <param name="PhoneVerificationService">The deterministic fake Phone Verification boundary.</param>
		public StoryViewerViewModel(IPhoneVerificationService PhoneVerificationService)
		{
			this.phoneVerificationService = PhoneVerificationService;
		}

		/// <summary>Gets the localized text used by the production send-code button.</summary>
		public string LocalizedSendCodeText => AppResources.SendCode;

		/// <summary>Gets whether the current phone value is valid.</summary>
		public bool NumberIsValid => this.TypeIsValid && this.LengthIsValid;

		/// <summary>Gets whether the validation message is visible.</summary>
		public bool ShowValidationError => !this.NumberIsValid && this.PhoneNumber.Length > 0;

		/// <summary>Gets the same localized validation message used by the production ViewModel.</summary>
		public string LocalizedValidationError => !this.TypeIsValid
			? AppResources.PhoneValidationDigits
			: !this.LengthIsValid ? AppResources.PhoneValidationLength : string.Empty;

		/// <summary>Gets whether the send-code command can execute.</summary>
		public bool CanSendCode => this.NumberIsValid && this.PhoneNumber.Length > 0 && !this.IsBusy;

		/// <summary>Gets whether the controlled story status is visible.</summary>
		public bool HasStatus => !string.IsNullOrWhiteSpace(this.StatusMessage);

		/// <summary>
		/// Applies a registered story without touching production application state.
		/// </summary>
		/// <param name="Story">The story to apply.</param>
		public void LoadStory(StoryDefinition Story)
		{
			this.Reset();
			this.Title = Story.Feature;
			this.Subtitle = Story.Name;
			this.AutomationId = Story.StoryType == StoryType.FullScreen
				? StorybookAutomationIds.Create("storybook", "page", "story", Story.Screen, "fullscreen")
				: StorybookAutomationIds.Create("storybook", "page", "story", Story.Screen, Story.StoryType.ToString(), Story.Name);
			this.StoryTypeLabel = Story.StoryType switch
			{
				StoryType.FullScreen => "FULL SCREEN",
				StoryType.State => "STATE",
				StoryType.Component => "COMPONENT",
				_ => Story.StoryType.ToString().ToUpperInvariant()
			};
			this.IsComponentStory = Story.StoryType == StoryType.Component;

			switch (Story.State)
			{
				case PhoneVerificationStoryState.Default:
				case PhoneVerificationStoryState.Empty:
					this.IsScreenStory = true;
					break;
				case PhoneVerificationStoryState.ValidPhoneNumber:
					this.IsScreenStory = true;
					this.SetPhoneNumber("5551234");
					break;
				case PhoneVerificationStoryState.InvalidPhoneNumber:
					this.IsScreenStory = true;
					this.SetPhoneNumber("12ab");
					break;
				case PhoneVerificationStoryState.SendingCode:
					this.IsScreenStory = true;
					this.SetPhoneNumber("5551234");
					this.IsBusy = true;
					this.outcome = PhoneVerificationOutcome.Loading;
					this.SetStatus("Fake service request remains in progress.", Color.FromArgb("#334375B0"));
					break;
				case PhoneVerificationStoryState.CodeSent:
					this.IsScreenStory = true;
					this.SetPhoneNumber("5551234");
					this.SetStatus("Verification code sent by the fake service.", Color.FromArgb("#330A715F"));
					break;
				case PhoneVerificationStoryState.BackendError:
					this.IsScreenStory = true;
					this.SetPhoneNumber("5551234");
					this.outcome = PhoneVerificationOutcome.BackendError;
					this.SetStatus("Controlled backend error: the code was not sent.", Color.FromArgb("#33F2495C"));
					break;
				case PhoneVerificationStoryState.NetworkError:
					this.IsScreenStory = true;
					this.SetPhoneNumber("5551234");
					this.outcome = PhoneVerificationOutcome.NetworkError;
					this.SetStatus("Controlled network error: no connection is required.", Color.FromArgb("#33F2495C"));
					break;
				case PhoneVerificationStoryState.CountryUnitedStates:
					this.IsCountryPickerStory = true;
					break;
				case PhoneVerificationStoryState.CountrySweden:
					this.IsCountryPickerStory = true;
					this.SelectedCountry = sweden;
					break;
				case PhoneVerificationStoryState.CountryDisabled:
					this.IsCountryPickerStory = true;
					this.IsComponentEnabled = false;
					break;
				case PhoneVerificationStoryState.PhoneInputEmpty:
					this.IsPhoneInputStory = true;
					break;
				case PhoneVerificationStoryState.PhoneInputValid:
					this.IsPhoneInputStory = true;
					this.SetPhoneNumber("5551234");
					break;
				case PhoneVerificationStoryState.PhoneInputInvalid:
					this.IsPhoneInputStory = true;
					this.SetPhoneNumber("12ab");
					break;
				case PhoneVerificationStoryState.PhoneInputDisabled:
					this.IsPhoneInputStory = true;
					this.IsComponentEnabled = false;
					this.SetPhoneNumber("5551234");
					break;
				case PhoneVerificationStoryState.PhoneInputFocused:
					this.IsPhoneInputStory = true;
					this.ShouldFocusInput = true;
					break;
				case PhoneVerificationStoryState.PhoneCodeVerifying:
				case PhoneVerificationStoryState.EmailCodeVerifying:
					this.IsBusy = true;
					this.SetStatus("The fake service is verifying the code.", Color.FromArgb("#334375B0"));
					break;
				case PhoneVerificationStoryState.PhoneCodeVerified:
				case PhoneVerificationStoryState.EmailCodeVerified:
					this.SetStatus("The fake service accepted the code.", Color.FromArgb("#330A715F"));
					break;
				case PhoneVerificationStoryState.PhoneCodeExpired:
				case PhoneVerificationStoryState.EmailCodeExpired:
					this.SetStatus("Controlled error: the verification code has expired.", Color.FromArgb("#33F2495C"));
					break;
				case PhoneVerificationStoryState.PhoneCodeBackendError:
				case PhoneVerificationStoryState.EmailCodeBackendError:
					this.SetStatus("Controlled backend error: the code could not be verified.", Color.FromArgb("#33F2495C"));
					break;
			}

			this.SendCodeCommand.NotifyCanExecuteChanged();
		}

		/// <summary>
		/// Applies a country selected in the production country list.
		/// </summary>
		/// <param name="Country">The selected country.</param>
		public void SelectCountry(ISO_3166_Country Country)
		{
			this.SelectedCountry = Country;
			this.IsCountrySelectionVisible = false;
		}

		partial void OnPhoneNumberChanged(string Value)
		{
			this.TypeIsValid = Value.All(char.IsDigit);
			this.LengthIsValid = Value.Length == 0 || Value.Length >= 4;
			this.OnPropertyChanged(nameof(this.NumberIsValid));
			this.SendCodeCommand.NotifyCanExecuteChanged();
		}

		partial void OnTypeIsValidChanged(bool Value)
		{
			this.OnPropertyChanged(nameof(this.NumberIsValid));
			this.SendCodeCommand.NotifyCanExecuteChanged();
		}

		partial void OnLengthIsValidChanged(bool Value)
		{
			this.OnPropertyChanged(nameof(this.NumberIsValid));
			this.SendCodeCommand.NotifyCanExecuteChanged();
		}

		partial void OnIsBusyChanged(bool Value) => this.SendCodeCommand.NotifyCanExecuteChanged();

		[RelayCommand]
		private void SelectPhoneCode()
		{
			this.IsCountrySelectionVisible = !this.IsCountrySelectionVisible;
		}

		[RelayCommand(CanExecute = nameof(CanSendCode))]
		private async Task SendCodeAsync(CancellationToken CancellationToken)
		{
			this.IsBusy = true;
			try
			{
				PhoneVerificationResult Result = await this.phoneVerificationService.SendCodeAsync(
					this.PhoneNumber,
					this.outcome,
					CancellationToken);
				this.SetStatus(Result.Message, Result.Succeeded ? Color.FromArgb("#330A715F") : Color.FromArgb("#33F2495C"));
			}
			finally
			{
				this.IsBusy = false;
			}
		}

		private void Reset()
		{
			this.IsComponentStory = false;
			this.IsScreenStory = false;
			this.IsCountryPickerStory = false;
			this.IsPhoneInputStory = false;
			this.IsComponentEnabled = true;
			this.ShouldFocusInput = false;
			this.IsCountrySelectionVisible = false;
			this.IsBusy = false;
			this.IsPhoneReadOnly = false;
			this.SelectedCountry = unitedStates;
			this.outcome = PhoneVerificationOutcome.Success;
			this.SetPhoneNumber(string.Empty);
			this.SetStatus(string.Empty, Colors.Transparent);
		}

		private void SetPhoneNumber(string Value)
		{
			this.PhoneNumber = Value;
			this.TypeIsValid = Value.All(char.IsDigit);
			this.LengthIsValid = Value.Length == 0 || Value.Length >= 4;
		}

		private void SetStatus(string Message, Color Color)
		{
			this.StatusMessage = Message;
			this.StatusColor = Color;
		}
	}
}
