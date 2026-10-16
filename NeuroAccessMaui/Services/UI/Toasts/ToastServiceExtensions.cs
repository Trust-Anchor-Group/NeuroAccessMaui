using NeuroAccessMaui.UI.Controls.Toasts;

namespace NeuroAccessMaui.Services.UI.Toasts
{
	/// <summary>
	/// Shortcuts for showing simple toasts.
	/// </summary>
	public static class ToastServiceExtensions
	{
		/// <summary>
		/// Shows a short message as a toast, when the app has a page to show it on. Errors are logged, not thrown.
		/// </summary>
		/// <param name="ToastService">The toast service.</param>
		/// <param name="Message">The message to show. Plain text.</param>
		/// <param name="Tapped">What to do when the toast is tapped, if anything. The toast is hidden first.</param>
		public static async Task ShowTextAsync(this IToastService ToastService, string Message, Func<Task>? Tapped = null)
		{
			try
			{
				NotificationToastView? Toast = await MainThread.InvokeOnMainThreadAsync(() =>
				{
					if (Application.Current is null || Application.Current.Windows.Count == 0 || ServiceRef.NavigationService.CurrentPage is null)
						return null;

					Func<Task>? OnTapped = null;
					if (Tapped is not null)
					{
						OnTapped = async () =>
						{
							await ToastService.HideAsync();
							await Tapped();
						};
					}

					return new NotificationToastView(Message, null, OnTapped);
				});

				if (Toast is null)
					return;

				await ToastService.ShowAsync(Toast);
			}
			catch (Exception Ex)
			{
				ServiceRef.LogService.LogException(Ex);
			}
		}
	}
}
