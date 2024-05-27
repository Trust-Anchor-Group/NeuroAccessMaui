﻿using System.Collections.ObjectModel;
using System.Diagnostics.CodeAnalysis;
using Mopups.Pages;
using NeuroAccessMaui.UI.Popups;
using Waher.Runtime.Inventory;

namespace NeuroAccessMaui.Services.UI
{
	/// <summary>
	/// Service serializing and managing UI-related tasks.
	/// </summary>
	[DefaultImplementation(typeof(UiService))]
	public interface IUiService : ILoadableService
	{
		#region DisplayAlert

		/// <summary>
		/// Displays an alert/message box to the user.
		/// </summary>
		/// <param name="Title">The title to display.</param>
		/// <param name="Message">The message to display.</param>
		/// <param name="Accept">The accept/ok button text.</param>
		/// <param name="Cancel">The cancel button text.</param>
		/// <returns>If Accept or Cancel was pressed</returns>
		Task<bool> DisplayAlert(string Title, string Message, string? Accept = null, string? Cancel = null);

		/// <summary>
		/// Displays an alert/message box to the user.
		/// </summary>
		/// <param name="Exception">The exception to display.</param>
		/// <param name="Title">The title to display.</param>
		Task DisplayException(Exception Exception, string? Title = null);

		#endregion

		#region DisplayPrompt

		/// <summary>
		/// Prompts the user for some input.
		/// </summary>
		/// <param name="Title">The title to display.</param>
		/// <param name="Message">The message to display.</param>
		/// <param name="Accept">The accept/ok button text.</param>
		/// <param name="Cancel">The cancel button text.</param>
		/// <returns>User input</returns>
		Task<string?> DisplayPrompt(string Title, string Message, string? Accept, string? Cancel);

		#endregion

		#region Screen shots

		/// <summary>
		/// Takes a screen-shot.
		/// </summary>
		/// <returns>Screen shot, if able.</returns>
		Task<ImageSource?> TakeScreenshotAsync();

		/// <summary>
		/// Takes a blurred screen shot
		/// </summary>
		/// <returns>Screenshot, if able to create a screen shot.</returns>
		Task<ImageSource?> TakeBlurredScreenshotAsync();

		#endregion

		#region Navigation

		// TEMP 
		Task WaitForNavigationToCompleteAsync();

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
		/// Pops the latests navigation arguments. Can only be used once to get the navigation arguments. Called by constructors to find
		/// associated navigation arguments for a page being constructed.
		/// </summary>
		/// <returns>Latest navigation arguments, or null if not found.</returns>
		TArgs? PopLatestArgs<TArgs>()
			where TArgs : NavigationArgs, new();

		/// <summary>
		/// Returns the page's arguments from the (one-level) deep navigation stack.
		/// </summary>
		/// <param name="UniqueId">View's unique ID.</param>
		/// <returns>View's navigation arguments, or null if not found.</returns>
		TArgs? TryGetArgs<TArgs>(string? UniqueId = null)
			where TArgs : NavigationArgs, new();

		/// <summary>
		/// Returns the page's arguments from the (one-level) deep navigation stack.
		/// </summary>
		/// <param name="Args">View's navigation arguments.</param>
		/// <param name="UniqueId">View's unique ID.</param>
		bool TryGetArgs<TArgs>([NotNullWhen(true)] out TArgs? Args, string? UniqueId = null)
			where TArgs : NavigationArgs, new();

		/// <summary>
		/// Current page
		/// </summary>
		Page CurrentPage { get; }

		#endregion

		#region Popups

		/// <summary>
		/// The current stack of popup pages.
		/// </summary>
		public ReadOnlyCollection<PopupPage?> PopupStack { get; }

		/// <summary>
		/// Pushes a popup onto the current view
		/// </summary>
		/// <typeparam name="TPage">The type of the page view</typeparam>
		/// <typeparam name="TViewModel">The type of the viewmodel to bind to the page view</typeparam>
		Task PushAsync<TPage, TViewModel>()
			where TViewModel : BasePopupViewModel, new()
			where TPage : BasePopup, new();

		/// <summary>
		/// Pushes a popup onto the current view
		/// </summary>
		/// <typeparam name="TPage">The type of the page view</typeparam>
		/// <typeparam name="TViewModel">The type of the viewmodel to bind to the page view</typeparam>
		/// <param name="page">An instance of the page view</param>
		/// <param name="viewModel">An instance of the viewmodel</param>
		Task PushAsync<TPage, TViewModel>(TPage page, TViewModel viewModel)
			where TViewModel : BasePopupViewModel
			where TPage : BasePopup;

		/// <summary>
		/// Pushes a popup onto the current view
		/// </summary>
		/// <typeparam name="TPage">The type of the page view</typeparam>
		/// <typeparam name="TViewModel">The type of the viewmodel to bind to the page view</typeparam>
		/// <param name="page">An instance of the page view</param>
		Task PushAsync<TPage, TViewModel>(TPage page)
			where TViewModel : BasePopupViewModel, new()
			where TPage : BasePopup;

		/// <summary>
		/// Pushes a popup onto the current view
		/// </summary>
		/// <typeparam name="TPage">The type of the page view</typeparam>
		/// <typeparam name="TViewModel">The type of the viewmodel to bind to the page view</typeparam>
		/// <param name="viewModel">An instance of the viewmodel</param>
		Task PushAsync<TPage, TViewModel>(TViewModel viewModel)
			where TViewModel : BasePopupViewModel
			where TPage : BasePopup, new();

		/// <summary>
		/// Pushes a popup, without any viewmodel binding, onto the current view
		/// </summary>
		/// <typeparam name="TPage">The type of the page view</typeparam>
		Task PushAsync<TPage>()
			where TPage : BasePopup, new();

		/// <summary>
		/// Pushes a popup, without any viewmodel binding, onto the current view
		/// </summary>
		/// <typeparam name="TPage">The type of the page view</typeparam>
		Task PushAsync<TPage>(TPage page)
			where TPage : BasePopup;

		/// <summary>
		/// Pushes a popup onto the current view
		/// </summary>
		/// <typeparam name="TPage">The type of the page view</typeparam>
		/// <typeparam name="TViewModel">The type of the viewmodel to bind to the page view</typeparam>
		/// <typeparam name="TReturn">The return value of the TViewModel</typeparam>
		/// <returns>Return value, or null if cancelled.</returns>
		Task<TReturn?> PushAsync<TPage, TViewModel, TReturn>()
			where TViewModel : ReturningPopupViewModel<TReturn>, new()
			where TPage : BasePopup, new();

		/// <summary>
		/// Pushes a popup onto the current view
		/// </summary>
		/// <typeparam name="TPage">The type of the page view</typeparam>
		/// <typeparam name="TViewModel">The type of the viewmodel to bind to the page view</typeparam>
		/// <typeparam name="TReturn">The return value of the TViewModel</typeparam>
		/// <param name="page">An instance of the page view</param>
		/// <param name="viewModel">An instance of the viewmodel</param>
		/// <returns>Return value, or null if cancelled.</returns>
		Task<TReturn?> PushAsync<TPage, TViewModel, TReturn>(TPage page, TViewModel viewModel)
			where TViewModel : ReturningPopupViewModel<TReturn>
			where TPage : BasePopup;

		/// <summary>
		/// Pushes a popup onto the current view
		/// </summary>
		/// <typeparam name="TPage">The type of the page view</typeparam>
		/// <typeparam name="TViewModel">The type of the viewmodel to bind to the page view</typeparam>
		/// <typeparam name="TReturn">The return value of the TViewModel</typeparam>
		/// <param name="page">An instance of the page view</param>
		/// <returns>Return value, or null if cancelled.</returns>
		Task<TReturn?> PushAsync<TPage, TViewModel, TReturn>(TPage page)
			where TViewModel : ReturningPopupViewModel<TReturn>, new()
			where TPage : BasePopup;

		/// <summary>
		/// Pushes a popup onto the current view
		/// </summary>
		/// <typeparam name="TPage">The type of the page view</typeparam>
		/// <typeparam name="TViewModel">The type of the viewmodel to bind to the page view</typeparam>
		/// <typeparam name="TReturn">The return value of the TViewModel</typeparam>
		/// <param name="viewModel">An instance of the viewmodel</param>
		/// <returns>Return value, or null if cancelled.</returns>
		Task<TReturn?> PushAsync<TPage, TViewModel, TReturn>(TViewModel viewModel)
			where TViewModel : ReturningPopupViewModel<TReturn>
			where TPage : BasePopup, new();

		/// <summary>
		/// Closes the current popup.
		/// </summary>
		Task PopAsync();

		#endregion

		#region Images
		/// <summary>
		/// Fetches a SVG and converts it to a PNG image source.
		/// </summary>
		/// <param name="svgUri">A string uri to the svg resource</param>
		/// <returns>An image Source representing the SVG file or null</returns>
		public Task<ImageSource?> ConvertSvgUriToImageSource(string svgUri);

		#endregion
	}
}
