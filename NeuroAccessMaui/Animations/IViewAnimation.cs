using System.Threading;
using System.Threading.Tasks;
using Microsoft.Maui.Controls;

namespace NeuroAccessMaui.Animations
{
	/// <summary>
	/// Defines a view-based animation executor.
	/// </summary>
	public interface IViewAnimation
	{
		/// <summary>
		/// Executes the animation against the specified target.
		/// </summary>
		/// <param name="Target">Visual element to animate.</param>
		/// <param name="Context">Execution context.</param>
		/// <param name="Options">Optional per-execution overrides.</param>
		/// <param name="Token">Cancellation token.</param>
		/// <returns>Task tracking execution.</returns>
		Task RunAsync(VisualElement Target, IAnimationContext Context, AnimationOptions? Options, CancellationToken Token);
	}

	/// <summary>
	/// Defines a transition animation executor.
	/// </summary>
	public interface ITransitionAnimation
	{
		/// <summary>
		/// Executes the transition animation for enter and exit elements.
		/// </summary>
		/// <param name="Entering">View entering the scene.</param>
		/// <param name="Exiting">View exiting the scene.</param>
		/// <param name="Context">Execution context.</param>
		/// <param name="Options">Optional per-execution overrides.</param>
		/// <param name="Token">Cancellation token.</param>
		/// <returns>Task tracking execution.</returns>
		Task RunAsync(VisualElement? Entering, VisualElement? Exiting, IAnimationContext Context, AnimationOptions? Options, CancellationToken Token);
	}
}
