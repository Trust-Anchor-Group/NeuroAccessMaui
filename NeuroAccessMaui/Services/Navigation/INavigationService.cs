using System.Diagnostics.CodeAnalysis;
using Waher.Runtime.Inventory;

namespace NeuroAccessMaui.Services.Navigation
{
	/// <summary>
	/// The navigation service is a wafer-thin wrapper around the <see cref="Shell"/>'s <c>GoToAsync()</c> methods.
	/// It also provides a uniform way of passing arguments to pages.
	/// Allows for easy mocking when unit testing.
	/// </summary>
	[DefaultImplementation(typeof(NavigationService))]
	public interface INavigationService : ILoadableService
	{
		/// <summary>
		/// Navigates the AppShell to the specified route, with page arguments to match.
		/// </summary>
		/// <param name="Route">The route whose matching page to navigate to.</param>
		/// <param name="BackMethod">How to handle the back button.</param>
		/// <param name="UniqueId">Views unique ID.</param>
		Task GoToAsync(string Route, BackMethod BackMethod = BackMethod.Inherited, string? UniqueId = null);

		/// <summary>
		/// Navigates the AppShell to the specified route, with page arguments to match.
		/// </summary>
		/// <param name="Args">The specific args to pass to the page.</param>
		/// <param name="Route">The route whose matching page to navigate to.</param>
		/// <param name="BackMethod">How to handle the back button.</param>
		/// <param name="UniqueId">Views unique ID.</param>
		Task GoToAsync<TArgs>(string Route, TArgs? Args, BackMethod BackMethod = BackMethod.Inherited, string? UniqueId = null) where TArgs : NavigationArgs, new();

		/// <summary>
		/// Returns to the previous page/route.
		/// </summary>
		/// <param name="Animate">If animation should be used.</param>
		Task GoBackAsync(bool Animate = true);

		/// <summary>
		/// Returns the page's arguments from the (one-level) deep navigation stack.
		/// </summary>
		/// <param name="Args">View's navigation arguments.</param>
		/// <param name="UniqueId">View's unique ID.</param>
		bool TryGetArgs<TArgs>([NotNullWhen(true)] out TArgs? Args, string? UniqueId = null) where TArgs : NavigationArgs, new();

		/// <summary>
		/// Current page
		/// </summary>
		Page CurrentPage { get; }
	}
}
