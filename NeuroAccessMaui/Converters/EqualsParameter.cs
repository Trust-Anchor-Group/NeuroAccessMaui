using System.Globalization;

namespace NeuroAccessMaui.Converters;

/// <summary>
/// EqualsParameter is an <see cref="IValueConverter"/> which converts a given value to a boolean indicating if the value
/// is equal to the provided <see cref="Binding.ConverterParameter"/>.
/// </summary>
public class EqualsParameter : IValueConverter, IMarkupExtension<EqualsParameter>
{
	/// <summary>
	/// Returns <c>true</c> if <paramref name="Value"/> equals to <paramref name="Parameter"/> and <c>false</c> otherwise.
	/// </summary>
	public object Convert(object Value, Type TargetType, object Parameter, CultureInfo Culture)
	{
		return Value is null ? Parameter is null : Value.Equals(Parameter);
	}

	/// <summary>
	/// Always throws a <see cref="NotImplementedException"/>.
	/// </summary>
	public object ConvertBack(object Value, Type TargetType, object Parameter, CultureInfo Culture)
	{
		throw new NotImplementedException();
	}

	/// <summary>
	/// Returns an instance of <see cref="EqualsParameter"/> class.
	/// </summary>
	public EqualsParameter ProvideValue(IServiceProvider ServiceProvider)
	{
		return this;
	}

	/// <summary>
	/// Returns an instance of <see cref="EqualsParameter"/> class.
	/// </summary>
	object IMarkupExtension.ProvideValue(IServiceProvider ServiceProvider)
	{
		return this.ProvideValue(ServiceProvider);
	}
}
