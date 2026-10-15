using System.Linq;

namespace NeuroAccessMaui.Services.UI
{
	/// <summary>
	/// Resolves the active MAUI page and presenter from the current application window graph.
	/// </summary>
	/// <remarks>
	/// Replaces the obsolete <c>Application.MainPage</c> lookups. The app runs a single window, so the first window that
	/// hosts a page is treated as the active one.
	/// </remarks>
	internal static class UiPageResolver
	{
		/// <summary>
		/// Gets the root page of the first application window that hosts a page.
		/// </summary>
		/// <param name="ApplicationInstance">The application to inspect, or <c>null</c> to use the current application.</param>
		/// <returns>The window's root page, or <c>null</c> when no window hosts a page yet.</returns>
		internal static Page? GetWindowPage(Application? ApplicationInstance = null)
		{
			ApplicationInstance ??= Microsoft.Maui.Controls.Application.Current;
			Window? ActiveWindow = ApplicationInstance?.Windows.FirstOrDefault(static WindowItem => WindowItem.Page is not null);
			return ActiveWindow?.Page;
		}

		/// <summary>
		/// Gets the page that is presented to the user, unwrapping a <see cref="NavigationPage"/> root when present.
		/// </summary>
		/// <param name="ApplicationInstance">The application to inspect, or <c>null</c> to use the current application.</param>
		/// <returns>The presented page, or <c>null</c> when no window hosts a page yet.</returns>
		internal static Page? GetPresentedPage(Application? ApplicationInstance = null)
		{
			Page? Page = GetWindowPage(ApplicationInstance);
			if (Page is NavigationPage NavigationPage && NavigationPage.CurrentPage is not null)
				return NavigationPage.CurrentPage;

			return Page;
		}

		/// <summary>
		/// Gets the MAUI <see cref="Shell"/> hosted by the active window, if any.
		/// </summary>
		/// <param name="ApplicationInstance">The application to inspect, or <c>null</c> to use the current application.</param>
		/// <returns>The hosted shell, or <c>null</c> when the window does not host a <see cref="Shell"/>.</returns>
		internal static Shell? GetShell(Application? ApplicationInstance = null)
		{
			Page? WindowPage = GetWindowPage(ApplicationInstance);
			if (WindowPage is Shell Shell)
				return Shell;

			return GetPresentedPage(ApplicationInstance) as Shell;
		}

		/// <summary>
		/// Gets the <see cref="IShellPresenter"/> that hosts screens, popups, and toasts.
		/// </summary>
		/// <param name="ApplicationInstance">The application to inspect, or <c>null</c> to use the current application.</param>
		/// <returns>The active shell presenter.</returns>
		/// <exception cref="InvalidOperationException">Thrown when the presented page is not a shell presenter.</exception>
		internal static IShellPresenter GetRequiredShellPresenter(Application? ApplicationInstance = null)
		{
			IShellPresenter? Presenter = GetPresentedPage(ApplicationInstance) as IShellPresenter;
			return Presenter ?? throw new InvalidOperationException("CustomShell presenter not found.");
		}
	}
}
