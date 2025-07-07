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
			layout = new Grid
			{
				RowDefinitions =
				{
					new RowDefinition { Height = GridLength.Auto },  // TopBar
                    new RowDefinition { Height = new GridLength(1, GridUnitType.Star) }, // Page
                    new RowDefinition { Height = GridLength.Auto }   // NavBar
                }
			};

                        topBar = new ContentView { IsVisible = false };
                        navBar = new ContentView { IsVisible = false };
                        contentHost = new ContentView();
                        modalHost = new ContentView { IsVisible = false, BackgroundColor = Color.FromArgb("#80000000") };
                        modalOverlay = new Grid { IsVisible = false, InputTransparent = false };
                        modalOverlay.Add(modalHost);


                        layout.Add(topBar, 0, 0);
                        layout.Add(contentHost, 0, 1);
                        layout.Add(navBar, 0, 2);
                        layout.Add(modalOverlay);
                        Grid.SetRowSpan(modalOverlay, 3);

			Content = layout;
			Padding = 0;
			On<iOS>().SetUseSafeArea(false);

			AppShell.RegisterRoutes();
		}

		/// <summary>
		/// The currently displayed page.
		/// </summary>
		public ContentPage CurrentPage => currentPage;

		/// <summary>
		/// Swaps in a new page, with optional transition, updating bars and lifecycle events.
		/// Handles custom lifecycle interface if present.
		/// </summary>
		/// <param name="Page">The new page to show.</param>
		/// <param name="Transition">Transition type (fade, etc).</param>
        public async Task SetPageAsync(ContentPage Page, TransitionType Transition = TransitionType.None)
        {
			// Remove previous page (disappearing, dispose)
			if (currentPage is not null)
			{
				if (currentPage is ILifeCycleView oldLifeCycle)
					await oldLifeCycle.OnDisappearingAsync();
				else
					currentPage.SendDisappearing();

				if (currentPage is ILifeCycleView oldDispose) await oldDispose.OnDisposeAsync();
			}

			// Add new page as a child to the layout, in content slot
			contentHost.Content = Page.Content;
			contentHost.BindingContext = Page.BindingContext;
			currentPage = Page;

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
			topBar.IsVisible = NavigationBars.GetTopBarVisible(Page);
			navBar.IsVisible = NavigationBars.GetNavBarVisible(Page);

			if (Page is IBarContentProvider barProvider)
			{
				topBar.Content = barProvider.TopBarContent;
				navBar.Content = barProvider.NavBarContent;
			}
			else
			{
				topBar.Content = null;
				navBar.Content = null;
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
                        modalHost.Content = Page.Content;
                        modalHost.BindingContext = Page.BindingContext;
                        modalStack.Push(Page);

                        modalHost.IsVisible = true;
                        modalOverlay.IsVisible = true;

                        if (Page is ILifeCycleView lifeCycle)
                        {
                                await lifeCycle.OnInitializeAsync();
                                await lifeCycle.OnAppearingAsync();
                        }
                        else
                                Page.SendAppearing();

                        if (Transition == TransitionType.Fade)
                        {
                                modalOverlay.Opacity = 0;
                                await modalOverlay.FadeTo(1, 150, Easing.CubicOut);
                        }
                }

                /// <summary>
                /// Pops the top most modal page.
                /// </summary>
                public async Task PopModalAsync()
                {
                        if (modalStack.Count == 0)
                                return;

                        ContentPage Page = modalStack.Pop();

                        if (Page is ILifeCycleView lifeCycle)
                                await lifeCycle.OnDisappearingAsync();
                        else
                                Page.SendDisappearing();

                        if (Page is ILifeCycleView dispose)
                                await dispose.OnDisposeAsync();

                        modalHost.Content = null;
                        modalHost.BindingContext = null;

                        if (modalStack.Count == 0)
                        {
                                modalHost.IsVisible = false;
                                modalOverlay.IsVisible = false;
                        }
                        else
                        {
                                var next = modalStack.Peek();
                                modalHost.Content = next.Content;
                                modalHost.BindingContext = next.BindingContext;
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
