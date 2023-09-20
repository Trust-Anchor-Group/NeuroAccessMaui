using Microsoft.Extensions.Localization;
using NeuroAccessMaui.Exceptions;
using NeuroAccessMaui.Resources.Languages;
using System.ComponentModel;
using System.Globalization;

namespace NeuroAccessMaui.Services.Localization;

public class LocalizationManager : INotifyPropertyChanged
{
	private static LocalizationManager? current;
	public static LocalizationManager Current => current ??= new();

#pragma warning disable CA2211 // Non-constant fields should not be visible
	public static Type DefaultStringResource = typeof(AppResources);
	public static EventHandler<CultureInfo>? CurrentCultureChanged;
	public static EventHandler<FlowDirection>? FlowDirectionChanged;
#pragma warning restore CA2211 // Non-constant fields should not be visible

	public static IStringLocalizer GetStringLocalizer(Type? StringResource = null)
	{
		Type[] Arguments = [StringResource ?? DefaultStringResource];
		Type GenericType = typeof(IStringLocalizer<>).MakeGenericType(Arguments);
		return (IStringLocalizer)(ServiceHelper.GetService(GenericType)
			?? throw new LocalizationException("There is no localization service"));
	}

	public static IStringLocalizer GetStringLocalizer<TStringResource>()
	{
		return ServiceHelper.GetService<IStringLocalizer<TStringResource>>();
	}

	public static FlowDirection FlowDirection
	{
		get
		{
			return CultureInfo.CurrentUICulture.TextInfo.IsRightToLeft
				? FlowDirection.RightToLeft
				: FlowDirection.LeftToRight;
		}
	}

	public CultureInfo CurrentCulture
	{
		get => CultureInfo.CurrentUICulture;

		set
		{
			if (CultureInfo.CurrentCulture.Name == value.Name)
			{
				return;
			}

			CultureInfo.CurrentCulture = CultureInfo.CurrentUICulture = value;

			CurrentCultureChanged?.Invoke(null, value);
			PropertyChanged?.Invoke(this, new PropertyChangedEventArgs(nameof(this.CurrentCulture)));

			FlowDirectionChanged?.Invoke(this, FlowDirection);
			PropertyChanged?.Invoke(this, new PropertyChangedEventArgs(nameof(FlowDirection)));
		}
	}

	public event PropertyChangedEventHandler? PropertyChanged;
}
