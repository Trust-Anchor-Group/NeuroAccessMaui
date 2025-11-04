using Waher.Runtime.Inventory;
using NeuroAccessMaui.UI.Pages;

namespace NeuroAccessMaui.Services.UI
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
        Task GoToAsync(string Route);

        /// <summary>
        /// Navigates to the specified route and pushes the page onto the navigation stack specifying back method handling.
        /// </summary>
        /// <param name="Route">The route string.</param>
        /// <param name="BackMethod">How navigation back should be handled.</param>
        /// <param name="UniqueId">Optional unique identifier of the view instance.</param>
        Task GoToAsync(string Route, BackMethod BackMethod, string? UniqueId = null);

        /// <summary>
        /// Navigates to the specified route and pushes the page onto the navigation stack with navigation arguments.
        /// </summary>
        /// <typeparam name="TArgs">The navigation arguments type.</typeparam>
        /// <param name="Route">The route string.</param>
        /// <param name="Args">Optional navigation arguments.</param>
        Task GoToAsync<TArgs>(string Route, TArgs? Args)
            where TArgs : NavigationArgs, new();

        /// <summary>
        /// Navigates to the specified route and pushes the page onto the navigation stack with navigation arguments and back method handling.
        /// </summary>
        /// <typeparam name="TArgs">The navigation arguments type.</typeparam>
        /// <param name="Route">The route string.</param>
        /// <param name="Args">Optional navigation arguments.</param>
        /// <param name="BackMethod">How navigation back should be handled.</param>
        /// <param name="UniqueId">Optional unique identifier of the view instance.</param>
        Task GoToAsync<TArgs>(string Route, TArgs? Args, BackMethod BackMethod, string? UniqueId = null)
            where TArgs : NavigationArgs, new();

        /// <summary>
        /// Navigates directly to a shell-hosted view.
        /// </summary>
        /// <param name="Page">The view to navigate to.</param>
        Task GoToAsync(BaseContentPage Page);

        /// <summary>
        /// Replaces the entire navigation stack with the page registered for the given route. After completion, back navigation cannot return to previous pages.
        /// Optionally provides navigation arguments to the new root page.
        /// </summary>
        /// <typeparam name="TArgs">Navigation arguments type.</typeparam>
        /// <param name="Route">The route whose page should become the new root.</param>
        /// <param name="Args">Optional navigation arguments for the root page.</param>
        Task SetRootAsync<TArgs>(string Route, TArgs? Args) where TArgs : NavigationArgs, new();

        /// <summary>
        /// Replaces the entire navigation stack with the page registered for the given route. After completion, back navigation cannot return to previous pages.
        /// </summary>
        /// <param name="Route">The route whose page should become the new root.</param>
        Task SetRootAsync(string Route);

        /// <summary>
        /// Replaces the entire navigation stack with the provided page instance. After completion, back navigation cannot return to previous pages.
        /// </summary>
        /// <param name="Page">The page instance to set as the new root.</param>
        Task SetRootAsync(BaseContentPage Page);

        /// <summary>
        /// Pops the current page from the navigation stack and displays the previous page.
        /// </summary>
        Task GoBackAsync();

        /// <summary>
        /// Pops all pages until only the root page remains on the navigation stack.
        /// </summary>
        Task PopToRootAsync();

        /// <summary>
        /// Gets the current visible view.
        /// </summary>
        BaseContentPage CurrentPage { get; }

        /// <summary>
        /// Pops the latest navigation arguments if any.
        /// </summary>
        /// <typeparam name="TArgs">The navigation arguments type.</typeparam>
        /// <returns>Navigation arguments or null.</returns>
        TArgs? PopLatestArgs<TArgs>() where TArgs : NavigationArgs, new();
    }
}
