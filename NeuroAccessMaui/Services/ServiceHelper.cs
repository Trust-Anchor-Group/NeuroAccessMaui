namespace NeuroAccessMaui.Services;

public static class ServiceHelper
{
	public static T GetService<T>()
	{
		T? Service = Current.GetService<T>();

		if (Service is not null)
		{
			return Service;
		}
		else
		{
			throw new ArgumentException("Service not found: " + nameof(T));
		}
	}

	public static object GetService(Type ServiceType)
	{
		object? Service = Current.GetService(ServiceType);

		if (Service is not null)
		{
			return Service;
		}
		else
		{
			throw new ArgumentException("Service not found: " + ServiceType);
		}
	}

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
