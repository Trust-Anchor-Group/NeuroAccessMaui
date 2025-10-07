using Microsoft.Maui.Controls;
using System.Threading.Tasks;
using Waher.Runtime.Inventory;
using NeuroAccessMaui.UI.Pages;

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
        Task GoToAsync(string Route);

        /// <summary>
        /// Navigates to the specified route and pushes the page onto the navigation stack specifying back method handling.
        /// </summary>
        /// <param name="Route">The route string.</param>
        /// <param name="BackMethod">How navigation back should be handled.</param>
        /// <param name="UniqueId">Optional unique identifier of the view instance.</param>
        Task GoToAsync(string Route, Services.UI.BackMethod BackMethod, string? UniqueId = null);

        /// <summary>
        /// Navigates to the specified route and pushes the page onto the navigation stack with navigation arguments.
        /// </summary>
        /// <typeparam name="TArgs">The navigation arguments type.</typeparam>
        /// <param name="Route">The route string.</param>
        /// <param name="Args">Optional navigation arguments.</param>
        Task GoToAsync<TArgs>(string Route, TArgs? Args)
            where TArgs : Services.UI.NavigationArgs, new();

        /// <summary>
        /// Navigates to the specified route and pushes the page onto the navigation stack with navigation arguments and back method handling.
        /// </summary>
        /// <typeparam name="TArgs">The navigation arguments type.</typeparam>
        /// <param name="Route">The route string.</param>
        /// <param name="Args">Optional navigation arguments.</param>
        /// <param name="BackMethod">How navigation back should be handled.</param>
        /// <param name="UniqueId">Optional unique identifier of the view instance.</param>
        Task GoToAsync<TArgs>(string Route, TArgs? Args, Services.UI.BackMethod BackMethod, string? UniqueId = null)
            where TArgs : Services.UI.NavigationArgs, new();

        /// <summary>
        /// Navigates directly to a shell-hosted view.
        /// </summary>
        /// <param name="Page">The view to navigate to.</param>
        Task GoToAsync(BaseContentPage Page);

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
        /// Presents a view modally using dependency injection.
        /// </summary>
        /// <typeparam name="TPage">The view type.</typeparam>
        Task PushModalAsync<TPage>() where TPage : BaseContentPage;

        /// <summary>
        /// Presents a view modally using dependency injection with a view model.
        /// </summary>
        /// <typeparam name="TPage">The view type.</typeparam>
        /// <typeparam name="TViewModel">The view model type.</typeparam>
        Task PushModalAsync<TPage, TViewModel>()
            where TPage : BaseContentPage
            where TViewModel : BaseViewModel;

        /// <summary>
        /// Pops the top modal page.
        /// </summary>
        Task PopModalAsync();

        /// <summary>
        /// Gets the current modal view if any.
        /// </summary>
        BaseContentPage CurrentModalPage { get; }

        /// <summary>
        /// Gets the current visible view.
        /// </summary>
        BaseContentPage CurrentPage { get; }

        /// <summary>
        /// Pops the latest navigation arguments if any.
        /// </summary>
        /// <typeparam name="TArgs">The navigation arguments type.</typeparam>
        /// <returns>Navigation arguments or null.</returns>
        TArgs? PopLatestArgs<TArgs>() where TArgs : Services.UI.NavigationArgs, new();
    }
}
