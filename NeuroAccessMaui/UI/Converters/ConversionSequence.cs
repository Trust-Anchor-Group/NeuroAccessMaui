using System.Globalization;

namespace NeuroAccessMaui.UI.Converters
{
	/// <summary>
	/// Sequence of converters.
	/// </summary>
	[AcceptEmptyServiceProvider]
	public class ConversionSequence : List<IValueConverter>, IValueConverter, IMarkupExtension
	{
		/// <inheritdoc/>
		public object? Convert(object? value, Type targetType, object? parameter, CultureInfo culture)
		{
			foreach (IValueConverter Step in this)
				value = Step.Convert(value, targetType, parameter, culture);

			return value ?? string.Empty;
		}

		/// <inheritdoc/>
		public object? ConvertBack(object? value, Type targetType, object? parameter, CultureInfo culture)
		{
			int c = this.Count;

			while (--c >= 0)
				value = this[c].ConvertBack(value, targetType, parameter, culture);

			return value ?? string.Empty;
		}

		/// <inheritdoc/>
		public object ProvideValue(IServiceProvider serviceProvider)
		{
			return this;
		}
	}
}
