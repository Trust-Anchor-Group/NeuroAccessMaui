using Microsoft.Maui.Controls;
using NeuroAccessMaui.Services;
using System.Threading.Tasks;
using Waher.Runtime.Inventory;

namespace NeuroAccessMaui.Test
{
	/// <summary>
	/// Default implementation of <see cref="INavigationService"/> that uses MAUI Routing for route-based page instantiation.
	/// </summary>
	[Singleton]
	public class NavigationService : INavigationService
	{
		private readonly Stack<ContentPage> navigationStack = new();

		/// <inheritdoc/>
		public ContentPage CurrentPage => this.navigationStack.Count > 0 ? this.navigationStack.Peek() : null;

		/// <inheritdoc/>
		public async Task NavigateToAsync(string Route)
		{
			// Set in your CustomShell
			await MainThread.InvokeOnMainThreadAsync(async () =>
			{
				var page = Routing.GetOrCreateContent(Route, ServiceRef.Provider) as ContentPage;
				if (page is null)
					throw new InvalidOperationException($"No page registered for route '{Route}'.");

				this.navigationStack.Push(page);
				if (Application.Current.MainPage is CustomShell customShell)
				{
					await customShell.SetPageAsync(page);
				}
				else
				{
					// Fallback for testing without CustomShell
					Application.Current.MainPage = page;
				}
			});
		}

		/// <inheritdoc/>
		public async Task GoBackAsync()
		{
			if (this.navigationStack.Count > 1)
			{
				// Pop current
				this.navigationStack.Pop();
				var previous = this.navigationStack.Peek();

				await MainThread.InvokeOnMainThreadAsync(async () =>
				{
					if (Application.Current.MainPage is CustomShell customShell)
					{
						await customShell.SetPageAsync(previous);
					}
					else
					{
						Application.Current.MainPage = previous;
					}
				});
			}
		}
	}
}
