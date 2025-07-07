using Microsoft.Maui.Controls;
using System.Threading.Tasks;
using Waher.Runtime.Inventory;

namespace NeuroAccessMaui.Test
{
	/// <summary>
	/// Service for navigating between pages using route-based navigation.
	/// </summary>
	[DefaultImplementation(typeof(NavigationService))]
	public interface INavigationService
	{

		/// <summary>
		/// Navigates to the specified route and pushes the page onto the navigation stack.
		/// </summary>
		/// <param name="Route">The route string.</param>
		Task NavigateToAsync(string Route);

		/// <summary>
		/// Pops the current page from the navigation stack and displays the previous page.
		/// </summary>
                Task GoBackAsync();

                /// <summary>
                /// Presents a page modally.
                /// </summary>
                /// <param name="Route">The route string.</param>
                Task PushModalAsync(string Route);

                /// <summary>
                /// Pops the top modal page.
                /// </summary>
                Task PopModalAsync();

                /// <summary>
                /// Gets the current modal page if any.
                /// </summary>
                ContentPage CurrentModalPage { get; }

		/// <summary>
		/// Gets the current visible page.
		/// </summary>
		ContentPage CurrentPage { get; }
	}
}
