using System.Threading.Tasks;
using System.Collections.Generic;
using Microsoft.Maui.Controls;
using Microsoft.Maui.Controls.PlatformConfiguration;
using Microsoft.Maui.Controls.PlatformConfiguration.iOSSpecific;
using NeuroAccessMaui.UI.Pages;
using NeuroAccessMaui.UI.Pages.Main; // For ILifeCycleView

namespace NeuroAccessMaui.Test
{
	/// <summary>
	/// Custom shell with edge-to-edge layout, dynamic top/nav bars, and transition support.
	/// Handles both normal ContentPage and ILifeCycleView pages.
	/// </summary>
	public class CustomShell : ContentPage
	{
		private readonly Grid layout;
		private readonly ContentView topBar;
		private readonly ContentView navBar;
		private readonly ContentView contentHost;  // For hosting page content
		private readonly Grid modalOverlay;
		private readonly ContentView modalHost;
		private readonly Stack<ContentPage> modalStack = new();
		private ContentPage currentPage;

		/// <summary>
		/// Initializes a new instance of <see cref="CustomShell"/>.
		/// </summary>
		public CustomShell()
		{
			this.layout = new Grid
			{
				RowDefinitions =
				{
					new RowDefinition { Height = GridLength.Auto },  // TopBar
                    new RowDefinition { Height = new GridLength(1, GridUnitType.Star) }, // Page
                    new RowDefinition { Height = GridLength.Auto }   // NavBar
                }
			};

			this.topBar = new ContentView { IsVisible = false };
			this.navBar = new ContentView { IsVisible = false };
			this.contentHost = new ContentView();
			this.modalHost = new ContentView { IsVisible = false, BackgroundColor = Color.FromArgb("#80000000") };
			this.modalOverlay = new Grid { IsVisible = false, InputTransparent = false };
			this.modalOverlay.Add(this.modalHost);


			this.layout.Add(this.topBar, 0, 0);
			this.layout.Add(this.contentHost, 0, 1);
			this.layout.Add(this.navBar, 0, 2);
			this.layout.Add(this.modalOverlay);
			Grid.SetRowSpan(this.modalOverlay, 3);

			this.Content = this.layout;
			this.Padding = 0;
			this.On<iOS>().SetUseSafeArea(false);

			AppShell.RegisterRoutes();
		}

		/// <summary>
		/// The currently displayed page.
		/// </summary>
		public ContentPage CurrentPage => this.currentPage;

		/// <summary>
		/// Swaps in a new page, with optional transition, updating bars and lifecycle events.
		/// Handles custom lifecycle interface if present.
		/// </summary>
		/// <param name="Page">The new page to show.</param>
		/// <param name="Transition">Transition type (fade, etc).</param>
		public async Task SetPageAsync(ContentPage Page, TransitionType Transition = TransitionType.None)
		{
			// Remove previous page (disappearing, dispose)
			if (this.currentPage is not null)
			{
				if (this.currentPage is ILifeCycleView oldLifeCycle)
					await oldLifeCycle.OnDisappearingAsync();
				else
					this.currentPage.SendDisappearing();

				if (this.currentPage is ILifeCycleView oldDispose) await oldDispose.OnDisposeAsync();
			}

			// Add new page as a child to the layout, in content slot
			this.contentHost.Content = Page.Content;
			this.contentHost.BindingContext = Page.BindingContext;
			this.currentPage = Page;

			// LifeCycle: OnInitializeAsync (once), then Appearing
			if (Page is ILifeCycleView newLifeCycle)
			{
				// Optionally track if OnInitializeAsync already called if you wish; for demo just call.
				await newLifeCycle.OnInitializeAsync();
				await newLifeCycle.OnAppearingAsync();
			}
			else
			{
				Page.SendAppearing();
			}

			// Manage bar visibility/content
			this.topBar.IsVisible = NavigationBars.GetTopBarVisible(Page);
			this.navBar.IsVisible = NavigationBars.GetNavBarVisible(Page);

			if (Page is IBarContentProvider barProvider)
			{
				this.topBar.Content = barProvider.TopBarContent;
				this.navBar.Content = barProvider.NavBarContent;
			}
			else
			{
				this.topBar.Content = null;
				this.navBar.Content = null;
			}

			// Transition (fade in)
			if (Transition == TransitionType.Fade)
			{
				Page.Opacity = 0;
				await Page.FadeTo(1, 150, Easing.CubicOut);
			}
		}

		/// <summary>
		/// Displays a page modally on top of the current page.
		/// </summary>
		/// <param name="Page">The modal page to show.</param>
		/// <param name="Transition">Optional transition.</param>
		public async Task PushModalAsync(ContentPage Page, TransitionType Transition = TransitionType.Fade)
		{
			this.modalHost.Content = Page.Content;
			this.modalHost.BindingContext = Page.BindingContext;
			this.modalStack.Push(Page);

			this.modalHost.IsVisible = true;
			this.modalOverlay.IsVisible = true;

			if (Page is ILifeCycleView lifeCycle)
			{
				await lifeCycle.OnInitializeAsync();
				await lifeCycle.OnAppearingAsync();
			}
			else
				Page.SendAppearing();

			if (Transition == TransitionType.Fade)
			{
				this.modalOverlay.Opacity = 0;
				await this.modalOverlay.FadeTo(1, 150, Easing.CubicOut);
			}
		}

		/// <summary>
		/// Pops the top most modal page.
		/// </summary>
		public async Task PopModalAsync()
		{
			if (this.modalStack.Count == 0)
				return;

			ContentPage Page = this.modalStack.Pop();

			if (Page is ILifeCycleView lifeCycle)
				await lifeCycle.OnDisappearingAsync();
			else
				Page.SendDisappearing();

			if (Page is ILifeCycleView dispose)
				await dispose.OnDisposeAsync();

			this.modalHost.Content = null;
			this.modalHost.BindingContext = null;

			if (this.modalStack.Count == 0)
			{
				this.modalHost.IsVisible = false;
				this.modalOverlay.IsVisible = false;
			}
			else
			{
				ContentPage next = this.modalStack.Peek();
				this.modalHost.Content = next.Content;
				this.modalHost.BindingContext = next.BindingContext;
			}
		}
	}

	/// <summary>
	/// Transition types for page navigation in CustomShell.
	/// </summary>
	public enum TransitionType
	{
		None,
		Fade,
		// Extend: SlideLeft, SlideRight, Scale, etc.
	}

	/// <summary>
	/// Optional interface for pages that provide their own bar content.
	/// </summary>
	public interface IBarContentProvider
	{
		View TopBarContent { get; }
		View NavBarContent { get; }
	}

	/// <summary>
	/// Helper class for attached properties to show/hide bars.
	/// </summary>
	public static class NavigationBars
	{
		public static readonly BindableProperty TopBarVisibleProperty =
			BindableProperty.CreateAttached(
				"TopBarVisible",
				typeof(bool),
				typeof(NavigationBars),
				false);

		public static readonly BindableProperty NavBarVisibleProperty =
			BindableProperty.CreateAttached(
				"NavBarVisible",
				typeof(bool),
				typeof(NavigationBars),
				false);

		public static bool GetTopBarVisible(BindableObject view) =>
			(bool)view.GetValue(TopBarVisibleProperty);

		public static void SetTopBarVisible(BindableObject view, bool value) =>
			view.SetValue(TopBarVisibleProperty, value);

		public static bool GetNavBarVisible(BindableObject view) =>
			(bool)view.GetValue(NavBarVisibleProperty);

		public static void SetNavBarVisible(BindableObject view, bool value) =>
			view.SetValue(NavBarVisibleProperty, value);
	}
}
