using System.ComponentModel;
using CommunityToolkit.Mvvm.ComponentModel;
using CommunityToolkit.Mvvm.Input;
using NeuroAccessMaui.Resources.Languages;
using NeuroAccessMaui.Services;
using NeuroAccessMaui.Services.Localization;
using NeuroAccessMaui.Services.Tag;

namespace NeuroAccessMaui.UI.Pages.Registration.Views
{
	public partial class DefinePinViewModel : BaseRegistrationViewModel
	{
		public DefinePinViewModel() : base(RegistrationStep.DefinePin)
		{
		}

		/// <inheritdoc />
		protected override async Task OnInitialize()
		{
			await base.OnInitialize();

			LocalizationManager.Current.PropertyChanged += this.LocalizationManagerEventHandler;
		}

		/// <inheritdoc/>
		protected override async Task OnDispose()
		{
			LocalizationManager.Current.PropertyChanged -= this.LocalizationManagerEventHandler;

			await base.OnDispose();
		}

		public void LocalizationManagerEventHandler(object? sender, PropertyChangedEventArgs e)
		{
			this.OnPropertyChanged(nameof(this.LocalizedValidationError));
		}

		[ObservableProperty]
		[NotifyPropertyChangedFor(nameof(IsPin1NotValid))]
		[NotifyPropertyChangedFor(nameof(IsPin2NotValid))]
		[NotifyPropertyChangedFor(nameof(LocalizedValidationError))]
		[NotifyPropertyChangedFor(nameof(PinsMatch))]
		[NotifyCanExecuteChangedFor(nameof(ContinueCommand))]
		private string? pinText1;

		[ObservableProperty]
		[NotifyPropertyChangedFor(nameof(IsPin2NotValid))]
		[NotifyPropertyChangedFor(nameof(PinsMatch))]
		[NotifyCanExecuteChangedFor(nameof(ContinueCommand))]
		private string? pinText2;

		/// <summary>
		/// Gets the value indicating how strong the <see cref="PinText1"/> is.
		/// </summary>
		public PinStrength PinStrength => ServiceRef.TagProfile.ValidatePinStrength(this.PinText1);

		/// <summary>
		/// Gets the value indicating whether the entered <see cref="PinText1"/> is the same as the entered <see cref="PinText2"/>.
		/// </summary>
		public bool PinsMatch => string.IsNullOrEmpty(this.PinText1) ? string.IsNullOrEmpty(this.PinText2) : string.Equals(this.PinText1, this.PinText2, StringComparison.Ordinal);

		/// <summary>
		/// If First PIN entry is not valid.
		/// </summary>
		public bool IsPin1NotValid => !string.IsNullOrEmpty(this.PinText1) &&  this.PinStrength != PinStrength.Strong;

		/// <summary>
		/// If Second PIN entry is not valid.
		/// </summary>
		public bool IsPin2NotValid => !string.IsNullOrEmpty(this.PinText2) && !this.PinsMatch;

		/// <summary>
		/// Localized validation error message.
		/// </summary>
		public string LocalizedValidationError => GetLocalizedValidationError(this.PinStrength);

		/// <summary>
		/// Gets a localized error message, given a PIN strength.
		/// </summary>
		/// <param name="PinStrength">PIN strength.</param>
		/// <returns>Localized error message (or empty string if OK).</returns>
		public static string GetLocalizedValidationError(PinStrength PinStrength)
		{
			return PinStrength switch
			{
				PinStrength.NotEnoughDigitsLettersSigns => ServiceRef.Localizer[nameof(AppResources.PinWithNotEnoughDigitsLettersSigns), Constants.Security.MinPinSymbolsFromDifferentClasses],

				PinStrength.NotEnoughDigitsOrSigns => ServiceRef.Localizer[nameof(AppResources.PinWithNotEnoughDigitsOrSigns), Constants.Security.MinPinSymbolsFromDifferentClasses],
				PinStrength.NotEnoughLettersOrDigits => ServiceRef.Localizer[nameof(AppResources.PinWithNotEnoughLettersOrDigits), Constants.Security.MinPinSymbolsFromDifferentClasses],
				PinStrength.NotEnoughLettersOrSigns => ServiceRef.Localizer[nameof(AppResources.PinWithNotEnoughLettersOrSigns), Constants.Security.MinPinSymbolsFromDifferentClasses],
				PinStrength.TooManyIdenticalSymbols => ServiceRef.Localizer[nameof(AppResources.PinWithTooManyIdenticalSymbols), Constants.Security.MaxPinIdenticalSymbols],
				PinStrength.TooManySequencedSymbols => ServiceRef.Localizer[nameof(AppResources.PinWithTooManySequencedSymbols), Constants.Security.MaxPinSequencedSymbols],
				PinStrength.TooShort => ServiceRef.Localizer[nameof(AppResources.PinTooShort), Constants.Security.MinPinLength],

				PinStrength.ContainsAddress => ServiceRef.Localizer[nameof(AppResources.PinContainsAddress)],
				PinStrength.ContainsName => ServiceRef.Localizer[nameof(AppResources.PinContainsName)],
				PinStrength.ContainsPersonalNumber => ServiceRef.Localizer[nameof(AppResources.PinContainsPersonalNumber)],
				PinStrength.ContainsPhoneNumber => ServiceRef.Localizer[nameof(AppResources.PinContainsPhoneNumber)],
				PinStrength.ContainsEMail => ServiceRef.Localizer[nameof(AppResources.PinContainsEMail)],
				PinStrength.Strong => string.Empty,
				_ => throw new NotImplementedException()
			};
		}

		public bool CanContinue => this.PinStrength == PinStrength.Strong && this.PinsMatch;

		[RelayCommand(CanExecute = nameof(CanContinue))]
		private void Continue()
		{
			ServiceRef.TagProfile.SetPin(this.PinText1!);

			GoToRegistrationStep(RegistrationStep.Complete);

			if (ServiceRef.TagProfile.TestOtpTimestamp is not null)
			{
				ServiceRef.UiSerializer.DisplayAlert(
					ServiceRef.Localizer[nameof(AppResources.WarningTitle)],
					ServiceRef.Localizer[nameof(AppResources.TestOtpUsed)],
					ServiceRef.Localizer[nameof(AppResources.Ok)]);
			}
		}
	}
}
