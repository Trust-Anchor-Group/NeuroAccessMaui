using NeuroAccessMaui.UI.Popups;
using Waher.Runtime.Inventory;
using Waher.Script.Content.Functions.InputOutput;

namespace NeuroAccessMaui.Services.Popup
{

	/// <summary>
	/// A wrapper allowing the displaying of and retrieving data from popups.
	/// An effort to decouple implementation from the use of libraries.
	/// </summary>
	[DefaultImplementation(typeof(PopupService))]
	public interface IPopupService
	{

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
		/// <returns></returns>
		Task PushAsync<TPage, TViewModel>(TPage page, TViewModel viewModel)
			where TViewModel : BasePopupViewModel
			where TPage : BasePopup;

		/// <summary>
		/// Pushes a popup onto the current view
		/// </summary>
		/// <typeparam name="TPage">The type of the page view</typeparam>
		/// <typeparam name="TViewModel">The type of the viewmodel to bind to the page view</typeparam>
		/// <param name="page">An instance of the page view</param>
		/// <returns></returns>
		Task PushAsync<TPage, TViewModel>(TPage page)
			where TViewModel : BasePopupViewModel, new()
			where TPage : BasePopup;

		/// <summary>
		/// Pushes a popup onto the current view
		/// </summary>
		/// <typeparam name="TPage">The type of the page view</typeparam>
		/// <typeparam name="TViewModel">The type of the viewmodel to bind to the page view</typeparam>
		/// <param name="viewModel">An instance of the viewmodel</param>
		/// <returns></returns>
		Task PushAsync<TPage, TViewModel>(TViewModel viewModel)
			where TViewModel : BasePopupViewModel
			where TPage : BasePopup, new();

		/// <summary>
		/// Pushes a popup, without any viewmodel binding, onto the current view
		/// </summary>
		/// <typeparam name="TPage">The type of the page view</typeparam>
		/// <returns></returns>
		Task PushAsync<TPage>()
			where TPage : BasePopup, new();

		/// <summary>
		/// Pushes a popup onto the current view
		/// </summary>
		/// <typeparam name="TPage">The type of the page view</typeparam>
		/// <typeparam name="TViewModel">The type of the viewmodel to bind to the page view</typeparam>
		/// <typeparam name="TReturn">The return value of the TViewModel</typeparam>
		/// <returns></returns>
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
		/// <returns></returns>
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
		/// <returns></returns>
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
		/// <returns></returns>
		Task<TReturn?> PushAsync<TPage, TViewModel, TReturn>(TViewModel viewModel)
			where TViewModel : ReturningPopupViewModel<TReturn>
			where TPage : BasePopup, new();

		Task PopAsync();

	}
}
