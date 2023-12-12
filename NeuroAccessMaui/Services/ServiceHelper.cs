namespace NeuroAccessMaui.Services
{
	public static class ServiceHelper
	{
		public static T GetService<T>()
			where T : class
		{
			T? Service;

			try
			{
				Service = CurrentServiceProvider?.GetService<T>();
			}
			catch (Exception)
			{
				throw new ArgumentException("Service not found: " + nameof(T));
			}

			if (Service is not null)
				return Service;
			else
				throw new ArgumentException("Service not found: " + nameof(T));
		}

		public static object GetService(Type ServiceType)
		{
			object? Service = CurrentServiceProvider?.GetService(ServiceType);

			if (Service is not null)
				return Service;
			else
				throw new ArgumentException("Service not found: " + ServiceType);
		}

		public static IServiceProvider? CurrentServiceProvider => (AppInfo.Current as IPlatformApplication)?.Services;
	}
}
