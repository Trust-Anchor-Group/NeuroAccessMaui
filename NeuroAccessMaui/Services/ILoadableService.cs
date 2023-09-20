using Waher.Runtime.Inventory;

namespace NeuroAccessMaui.Services;

/// <summary>
/// A service that can be loaded and unloaded at will. Typically during startup and shutdown of an application.
/// </summary>
[DefaultImplementation(typeof(LoadableService))]
public interface ILoadableService
{
	/// <summary>
	/// Loads the specified service.
	/// </summary>
	/// <param name="isResuming">Set to <c>true</c> to indicate the app is resuming as opposed to starting cold.</param>
	/// <param name="cancellationToken">Will stop the service load if the token is set.</param>
	Task Load(bool isResuming, CancellationToken cancellationToken);

	/// <summary>
	/// Unloads the specified service.
	/// </summary>
	Task Unload();

	/// <summary>
	/// Fires whenever the loading state of the service changes.
	/// </summary>
	event EventHandler<LoadedEventArgs> Loaded;
}
