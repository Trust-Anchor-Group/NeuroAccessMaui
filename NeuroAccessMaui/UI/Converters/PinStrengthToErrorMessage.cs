using Microsoft.Extensions.Localization;
using NeuroAccessMaui.Resources.Languages;
using NeuroAccessMaui.Services.Localization;
using NeuroAccessMaui.Services.Tag;
using System.Globalization;

namespace NeuroAccessMaui.UI.Converters;

/// <summary>
/// PinStrengthToErrorMessage is an <see cref="IValueConverter"/> which converts <see cref="PinStrength"/> to an error message to display.
/// </summary>
public class PinStrengthToErrorMessage : IValueConverter, IMarkupExtension<PinStrengthToErrorMessage>
{
	/// <summary>
	/// Returns a localized error message for a given <see cref="PinStrength"/>.
	/// </summary>
	public object Convert(object? Value, Type TargetType, object? Parameter, CultureInfo Culture)
	{
		if (Value is not PinStrength PinStrength)
		{
			throw new ArgumentException($"{nameof(Services.Tag.PinStrength)} expected but received {Value?.GetType().Name ?? "null"}.");
		}

		IStringLocalizer? Localizer = LocalizationManager.GetStringLocalizer<AppResources>();

		return PinStrength switch
		{
			PinStrength.NotEnoughDigitsLettersSigns => Localizer[nameof(AppResources.PinWithNotEnoughDigitsLettersSigns), Constants.Authentication.MinPinSymbolsFromDifferentClasses],

			PinStrength.NotEnoughDigitsOrSigns => Localizer[nameof(AppResources.PinWithNotEnoughDigitsOrSigns), Constants.Authentication.MinPinSymbolsFromDifferentClasses],
			PinStrength.NotEnoughLettersOrDigits => Localizer[nameof(AppResources.PinWithNotEnoughLettersOrDigits), Constants.Authentication.MinPinSymbolsFromDifferentClasses],
			PinStrength.NotEnoughLettersOrSigns => Localizer[nameof(AppResources.PinWithNotEnoughLettersOrSigns), Constants.Authentication.MinPinSymbolsFromDifferentClasses],
			PinStrength.TooManyIdenticalSymbols => Localizer[nameof(AppResources.PinWithTooManyIdenticalSymbols), Constants.Authentication.MaxPinIdenticalSymbols],
			PinStrength.TooManySequencedSymbols => Localizer[nameof(AppResources.PinWithTooManySequencedSymbols), Constants.Authentication.MaxPinSequencedSymbols],
			PinStrength.TooShort => Localizer[nameof(AppResources.PinTooShort), Constants.Authentication.MinPinLength],

			PinStrength.ContainsAddress => Localizer[nameof(AppResources.PinContainsAddress)],
			PinStrength.ContainsName => Localizer[nameof(AppResources.PinContainsName)],
			PinStrength.ContainsPersonalNumber => Localizer[nameof(AppResources.PinContainsPersonalNumber)],
			PinStrength.ContainsPhoneNumber => Localizer[nameof(AppResources.PinContainsPhoneNumber)],
			PinStrength.ContainsEMail => Localizer[nameof(AppResources.PinContainsEMail)],

			_ => "",
		};
	}

	/// <summary>
	/// Always throws a <see cref="NotImplementedException"/>.
	/// </summary>
	public object ConvertBack(object? Value, Type TargetType, object? Parameter, CultureInfo Culture)
	{
		throw new NotImplementedException();
	}

	/// <summary>
	/// Returns an instance of <see cref="PinStrengthToErrorMessage"/> class.
	/// </summary>
	public PinStrengthToErrorMessage ProvideValue(IServiceProvider ServiceProvider)
	{
		return this;
	}

	/// <summary>
	/// Returns an instance of <see cref="PinStrengthToErrorMessage"/> class.
	/// </summary>
	object IMarkupExtension.ProvideValue(IServiceProvider ServiceProvider)
	{
		return this.ProvideValue(ServiceProvider);
	}
}
