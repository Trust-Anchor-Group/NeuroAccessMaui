using System;
using System.Threading.Tasks;
using Microsoft.Maui.Controls;
using NeuroAccessMaui.UI.Popups;
using Waher.Runtime.Inventory;

namespace NeuroAccessMaui.Services.UI.Popups
{
	/// <summary>
	/// Service responsible for presenting and dismissing application popups.
	/// </summary>
	[DefaultImplementation(typeof(PopupService))]
	public interface IPopupService
	{
		/// <summary>
		/// Pushes a popup view resolved from dependency injection.
		/// </summary>
		/// <typeparam name="TPopup">Popup view type.</typeparam>
		/// <param name="Options">Optional presentation options.</param>
		Task PushAsync<TPopup>(PopupOptions? Options = null) where TPopup : ContentView;

		/// <summary>
		/// Pushes a popup view resolved from dependency injection together with its view model.
		/// </summary>
		/// <typeparam name="TPopup">Popup view type.</typeparam>
		/// <typeparam name="TViewModel">View model type.</typeparam>
		/// <param name="Options">Optional presentation options.</param>
		Task PushAsync<TPopup, TViewModel>(PopupOptions? Options = null)
			where TPopup : ContentView
			where TViewModel : class;

		/// <summary>
		/// Pushes a popup view resolved from dependency injection that returns a value when closed.
		/// </summary>
		/// <typeparam name="TPopup">Popup view type.</typeparam>
		/// <typeparam name="TViewModel">View model type.</typeparam>
		/// <typeparam name="TReturn">Return value type.</typeparam>
		/// <param name="Options">Optional presentation options.</param>
		/// <returns>Result value provided by the popup view model.</returns>
        Task<TReturn?> PushAsync<TPopup, TViewModel, TReturn>(PopupOptions? Options = null)
            where TPopup : ContentView
            where TViewModel : ReturningPopupViewModel<TReturn>;

        /// <summary>
        /// Pushes a popup with an explicitly provided view model that returns a value.
        /// </summary>
        /// <typeparam name="TPopup">Popup view type.</typeparam>
        /// <typeparam name="TViewModel">View model type.</typeparam>
        /// <typeparam name="TReturn">Return value type.</typeparam>
        /// <param name="ViewModel">View model instance to bind to the popup.</param>
        /// <param name="Options">Optional presentation options.</param>
        /// <returns>Result value provided by the popup view model.</returns>
        Task<TReturn?> PushAsync<TPopup, TViewModel, TReturn>(TViewModel ViewModel, PopupOptions? Options = null)
            where TPopup : ContentView
            where TViewModel : ReturningPopupViewModel<TReturn>;

        /// <summary>
        /// Pushes an already created popup instance.
        /// </summary>
        /// <param name="Popup">Popup view instance.</param>
        /// <param name="Options">Optional presentation options.</param>
		Task PushAsync(ContentView Popup, PopupOptions? Options = null);

		/// <summary>
		/// Dismisses the top-most popup if present.
		/// </summary>
		Task PopAsync();

		/// <summary>
		/// Returns true if at least one popup is currently presented.
		/// </summary>
		bool HasOpenPopups { get; }

		/// <summary>
		/// Event raised whenever the popup stack changes.
		/// </summary>
		event EventHandler? PopupStackChanged;
	}
}
