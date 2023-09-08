namespace NeuroAccessMaui.Services.Localization;

public static class LocalizationAppExtension
{
	public static MauiAppBuilder UseLocalizationManager<TStringResource>(this MauiAppBuilder Builder)
	{
		return Builder.UseLocalizationManager(typeof(TStringResource));
	}

	public static MauiAppBuilder UseLocalizationManager(this MauiAppBuilder Builder, Type? StringResource = null)
	{
		LocalizationManager.DefaultStringResource = StringResource;
		Builder.Services.AddLocalization();
		return Builder;
	}
}
