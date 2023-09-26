namespace NeuroAccessMaui.Pages;

/// <summary>
/// Interface for views who need to react to life-cycle events.
/// </summary>
public interface ILifeCycleView
{
	/// <summary>
	/// Method called when view is initialized for the first time. Use this method to implement registration
	/// of event handlers, processing navigation arguments, etc.
	/// </summary>
	Task Initialize();

	/// <summary>
	/// Method called when the view is disposed, and will not be used more. Use this method to unregister
	/// event handlers, etc.
	/// </summary>
	Task Dispose();

	/// <summary>
	/// Method called when view is appearing on the screen.
	/// </summary>
	Task Appearing();

	/// <summary>
	/// Method called when view is disappearing from the screen.
	/// </summary>
	Task Disappearing();
}
