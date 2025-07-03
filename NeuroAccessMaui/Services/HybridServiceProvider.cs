namespace NeuroAccessMaui.Services
{
	/// <summary>
	/// Provides a hybrid service provider that first attempts to resolve services using a custom system
	/// (via Waher.Runtime.Inventory.Types.Instantiate), and falls back to the provided IServiceProvider
	/// (typically Microsoft Dependency Injection) if the custom system does not resolve the service.
	/// </summary>
	/// <remarks>
	/// This allows integration of both custom and standard DI service resolution in .NET MAUI applications.
	/// </remarks>
	public class HybridServiceProvider(IServiceProvider fallbackProvider) : IServiceProvider
	{
		private readonly IServiceProvider fallbackProvider = fallbackProvider;

		/// <summary>
		/// Gets the service object of the specified type.
		/// Tries the custom system first, then falls back to the provided IServiceProvider.
		/// </summary>
		/// <param name="serviceType">The type of service to retrieve.</param>
		/// <returns>The service object, or null if not found.</returns>
		public object? GetService(Type serviceType)
		{
			// Try custom system first
			object? Service = Waher.Runtime.Inventory.Types.Instantiate(true, serviceType);
			if (Service is not null)
				return Service;
			// Fallback to MS DI
			return this.fallbackProvider.GetService(serviceType);
		}


	}
}
