using NeuroAccessMaui.Services.Tag;
using System.Globalization;

namespace NeuroAccessMaui.Converters;

/// <summary>
/// PinStrengthToErrorMessage is an <see cref="IValueConverter"/> which converts <see cref="PinStrength"/> to an error message to display.
/// </summary>
public class PinStrengthToErrorMessage : IValueConverter, IMarkupExtension<PinStrengthToErrorMessage>
{
	/// <summary>
	/// Returns a localized error message for a given <see cref="PinStrength"/>.
	/// </summary>
	public object Convert(object Value, Type TargetType, object Parameter, CultureInfo Culture)
	{
		if (Value is PinStrength PinStrength)
		{
			return PinStrength switch
			{
				PinStrength.NotEnoughDigitsLettersSigns => string.Format(LocalizationResourceManager.Current["PinWithNotEnoughDigitsLettersSigns"], Constants.Authentication.MinPinSymbolsFromDifferentClasses),

				PinStrength.NotEnoughDigitsOrSigns => string.Format(LocalizationResourceManager.Current["PinWithNotEnoughDigitsOrSigns"], Constants.Authentication.MinPinSymbolsFromDifferentClasses),
				PinStrength.NotEnoughLettersOrDigits => string.Format(LocalizationResourceManager.Current["PinWithNotEnoughLettersOrDigits"], Constants.Authentication.MinPinSymbolsFromDifferentClasses),
				PinStrength.NotEnoughLettersOrSigns => string.Format(LocalizationResourceManager.Current["PinWithNotEnoughLettersOrSigns"], Constants.Authentication.MinPinSymbolsFromDifferentClasses),

				PinStrength.ContainsAddress => LocalizationResourceManager.Current["PinContainsAddress"],
				PinStrength.ContainsName => LocalizationResourceManager.Current["PinContainsName"],
				PinStrength.ContainsPersonalNumber => LocalizationResourceManager.Current["PinContainsPersonalNumber"],
				PinStrength.ContainsPhoneNumber => LocalizationResourceManager.Current["PinContainsPhoneNumber"],
				PinStrength.ContainsEMail => LocalizationResourceManager.Current["PinContainsEMail"],

				PinStrength.TooManyIdenticalSymbols => string.Format(LocalizationResourceManager.Current["PinWithTooManyIdenticalSymbols"], Constants.Authentication.MaxPinIdenticalSymbols),
				PinStrength.TooManySequencedSymbols => string.Format(LocalizationResourceManager.Current["PinWithTooManySequencedSymbols"], Constants.Authentication.MaxPinSequencedSymbols),

				PinStrength.TooShort => string.Format(LocalizationResourceManager.Current["PinTooShort"], Constants.Authentication.MinPinLength),

				_ => "",
			};
		}

		throw new ArgumentException($"{nameof(Services.Tag.PinStrength)} expected but received {Value?.GetType().Name ?? "null"}.");
	}

	/// <summary>
	/// Always throws a <see cref="NotImplementedException"/>.
	/// </summary>
	public object ConvertBack(object Value, Type TargetType, object Parameter, CultureInfo Culture)
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
