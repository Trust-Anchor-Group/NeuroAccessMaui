using System;
using System.Threading.Tasks;
using Microsoft.Maui.Controls;
using Waher.Runtime.Inventory;

namespace NeuroAccessMaui.Services.UI.Toasts
{
	/// <summary>
	/// Service for presenting transient toast notifications.
	/// </summary>
	[DefaultImplementation(typeof(ToastService))]
	public interface IToastService
	{
		/// <summary>
		/// Shows a toast view instance.
		/// </summary>
		/// <param name="Toast">Toast view to present.</param>
		/// <param name="Options">Optional presentation options.</param>
		Task ShowAsync(View Toast, ToastOptions? Options = null);

		/// <summary>
		/// Shows a toast view resolved from dependency injection.
		/// </summary>
		/// <typeparam name="TToast">Toast view type.</typeparam>
		/// <param name="Options">Optional presentation options.</param>
		Task ShowAsync<TToast>(ToastOptions? Options = null) where TToast : View;

		/// <summary>
		/// Hides the currently active toast, if any.
		/// </summary>
		Task HideAsync();

		/// <summary>
		/// Returns true if a toast is currently presented.
		/// </summary>
		bool HasActiveToast { get; }

		/// <summary>
		/// Event raised whenever the active toast changes.
		/// </summary>
		event EventHandler? ToastChanged;
	}
}
