using Microsoft.Extensions.Localization;
using System.ComponentModel;
using System.Globalization;

namespace NeuroAccessMaui;

public class LocalizationManager : INotifyPropertyChanged
{
#pragma warning disable CA2211 // Non-constant fields should not be visible
	public static Type? DefaultStringResource;
#pragma warning restore CA2211 // Non-constant fields should not be visible

	private static LocalizationManager? current;
	public static LocalizationManager Current => current ??= new LocalizationManager();

	public static IStringLocalizer? GetStringLocalizer(Type? StringResource = null)
	{
#pragma warning disable CS8601 // Possible null reference assignment.
		Type[] Type = new Type[] { StringResource ?? DefaultStringResource };
#pragma warning restore CS8601 // Possible null reference assignment.

		Type GenericType = typeof(IStringLocalizer<>).MakeGenericType(Type);

		return (IStringLocalizer?)ServiceHelper.GetService(GenericType);
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

#pragma warning disable CA2211 // Non-constant fields should not be visible
	public static EventHandler<CultureInfo>? CurrentCultureChanged;
	public static EventHandler<FlowDirection>? FlowDirectionChanged;
#pragma warning restore CA2211 // Non-constant fields should not be visible

	public event PropertyChangedEventHandler? PropertyChanged;
}
