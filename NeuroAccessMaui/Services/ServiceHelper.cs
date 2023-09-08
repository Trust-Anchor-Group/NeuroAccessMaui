namespace NeuroAccessMaui;

public static class ServiceHelper
{
#pragma warning disable CS8603 // Possible null reference return.
	public static TService GetService<TService>() => Current.GetService<TService>();
#pragma warning restore CS8603 // Possible null reference return.

	public static object? GetService(Type ServiceType) => Current.GetService(ServiceType);

	public static IServiceProvider Current =>
#if WINDOWS
		MauiWinUIApplication.Current.Services;
#elif ANDROID
		MauiApplication.Current.Services;
#elif IOS || MACCATALYST
		MauiUIApplicationDelegate.Current.Services;
#else
		null;
#endif
}
