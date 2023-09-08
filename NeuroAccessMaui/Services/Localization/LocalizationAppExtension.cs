namespace NeuroAccessMaui;

public static class LocalizationAppExtension
{
	public static MauiAppBuilder UseLocalizationManager<TStringResource>(this MauiAppBuilder Builder)
	{
		return UseLocalizationManager(Builder, typeof(TStringResource));
	}

	public static MauiAppBuilder UseLocalizationManager(this MauiAppBuilder Builder, Type? StringResource = null)
	{
		LocalizationManager.DefaultStringResource = StringResource;
		Builder.Services.AddLocalization();
		return Builder;
	}
}
