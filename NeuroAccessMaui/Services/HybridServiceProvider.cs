using System.Collections.Concurrent;
using System.Reflection;
using NeuroAccessMaui.Services.Startup;
using Waher.Runtime.Inventory;

namespace NeuroAccessMaui.Services
{
	/// <summary>
	/// Provides a hybrid service provider that resolves services from the provided IServiceProvider (typically Microsoft
	/// Dependency Injection), and falls back to the Waher type inventory (via Waher.Runtime.Inventory.Types.Instantiate)
	/// for types that are not registered.
	/// </summary>
	/// <remarks>
	/// This allows integration of both custom and standard DI service resolution in .NET MAUI applications. Services are
	/// registered in dependency injection; the fallback mainly creates popups, toasts and view models that are resolved by
	/// their own type without being registered.
	/// </remarks>
	public class HybridServiceProvider(IServiceProvider fallbackProvider) : IServiceProvider
	{
		private static readonly ConcurrentDictionary<Type, bool> reportedFallbackTypes = new ConcurrentDictionary<Type, bool>();

		private readonly IServiceProvider fallbackProvider = fallbackProvider;

		/// <summary>
		/// Gets the service object of the specified type.
		/// Tries the dependency injection container first, then falls back to the Waher type inventory.
		/// </summary>
		/// <param name="serviceType">The type of service to retrieve.</param>
		/// <returns>The service object, or null if not found.</returns>
		public object? GetService(Type serviceType)
		{
			// Prefer the built DI container so constructor injection is honored, then fall back to the inventory.
			object? Service = this.fallbackProvider.GetService(serviceType);
			if (Service is not null)
				return Service;

			// A request for the implementation type of a registered service returns the registered singleton. The inventory
			// would otherwise create a second instance of it.
			Service = this.GetRegisteredDefaultImplementation(serviceType);
			if (Service is not null)
				return Service;

			ReportFallback(serviceType);

			// The inventory is built in the background during startup. Never block the caller, often the UI thread, waiting
			// for it; registered services do not depend on it.
			if (!RuntimeTypeInventory.IsReady)
			{
				LogWarning($"Type inventory not ready; unable to create unregistered type {serviceType.FullName}.");
				return null;
			}

			return Types.Instantiate(true, serviceType);
		}

		/// <summary>
		/// Finds the registered service whose default implementation is the requested class.
		/// </summary>
		/// <param name="ImplementationType">The requested type.</param>
		/// <returns>The registered service instance, or <c>null</c> if the type is not the default implementation of a registered service.</returns>
		private object? GetRegisteredDefaultImplementation(Type ImplementationType)
		{
			if (!ImplementationType.IsClass || ImplementationType.IsAbstract)
				return null;

			foreach (Type Interface in ImplementationType.GetInterfaces())
			{
				DefaultImplementationAttribute? Attribute = Interface.GetCustomAttribute<DefaultImplementationAttribute>();
				if (Attribute?.Type != ImplementationType)
					continue;

				object? Service = this.fallbackProvider.GetService(Interface);
				if (Service is not null && Service.GetType() == ImplementationType)
					return Service;
			}

			return null;
		}

		/// <summary>
		/// Logs the first fallback resolution of each type.
		/// </summary>
		/// <param name="ServiceType">The type resolved through the fallback.</param>
		private static void ReportFallback(Type ServiceType)
		{
			if (!reportedFallbackTypes.TryAdd(ServiceType, true))
				return;

			string Message = $"Type {ServiceType.FullName} is not registered in dependency injection and is created through the type inventory.";

			// Interfaces and abstract types are services that should be registered. Pages, popups and view models resolved by
			// their own type are expected here.
			if (ServiceType.IsInterface || ServiceType.IsAbstract)
				LogWarning(Message);
			else
			{
				try
				{
					ServiceRef.LogService.LogDebug(Message);
				}
				catch (Exception Ex)
				{
					System.Diagnostics.Debug.WriteLine(Ex);
				}
			}
		}

		private static void LogWarning(string Message)
		{
			try
			{
				ServiceRef.LogService.LogWarning(Message);
			}
			catch (Exception Ex)
			{
				System.Diagnostics.Debug.WriteLine(Ex);
			}
		}
	}
}
