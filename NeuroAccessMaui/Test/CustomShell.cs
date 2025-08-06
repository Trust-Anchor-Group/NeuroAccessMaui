using System.Threading.Tasks;
using System.Collections.Generic;
using Microsoft.Maui.Controls;
using Microsoft.Maui.Controls.PlatformConfiguration;
using Microsoft.Maui.Controls.PlatformConfiguration.iOSSpecific;
using NeuroAccessMaui.UI.Pages;
using NeuroAccessMaui.Services;                     // for ServiceHelper
using NeuroAccessMaui.UI.Pages.Startup;             // for LoadingPage
using NeuroAccessMaui.Test;                         // for CustomShell registration context
using NeuroAccessMaui.UI.Pages.Main; // For ILifeCycleView
using Microsoft.Maui.Graphics; // for Colors

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
        private readonly BoxView modalBackground;
        private readonly ContentView modalHost;
private readonly Stack<BaseContentPage> modalStack = new();
private BaseContentPage? currentPage;

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
            // Ensure background is white to avoid default purple
            this.layout.BackgroundColor = Colors.White;

            this.topBar = new ContentView { IsVisible = false };
            this.navBar = new ContentView { IsVisible = false };
            this.contentHost = new ContentView();
            this.modalHost = new ContentView { IsVisible = false };
            this.modalBackground = new BoxView { Opacity = 0 };
            this.modalOverlay = new Grid { IsVisible = false, InputTransparent = false };
            this.modalOverlay.Add(this.modalBackground);
            this.modalOverlay.Add(this.modalHost);

            TapGestureRecognizer BackgroundTap = new();
            BackgroundTap.Tapped += async (_, _) =>
            {
                if (this.modalStack.Count == 0)
                    return;

                BaseContentPage TopPage = this.modalStack.Peek();
                if (BaseModalPage.GetPopOnBackgroundPress(TopPage))
                    await this.PopModalAsync();
            };

            this.modalBackground.GestureRecognizers.Add(BackgroundTap);


            this.layout.Add(this.topBar, 0, 0);
            this.layout.Add(this.contentHost, 0, 1);
            this.layout.Add(this.navBar, 0, 2);
            this.layout.Add(this.modalOverlay);
            Grid.SetRowSpan(this.modalOverlay, 3);

            this.Content = this.layout;
            this.Padding = 0;
            this.On<iOS>().SetUseSafeArea(false);

            AppShell.RegisterRoutes();
            // Shell background
            this.BackgroundColor = Colors.White;
            // Show initial loading page immediately
            LoadingPage loadingPage = ServiceHelper.GetService<LoadingPage>();

            // Assign content synchronously so spinner is visible right away
            this.contentHost.Content = loadingPage.Content;
            this.contentHost.BindingContext = loadingPage.BindingContext;
            this.currentPage = loadingPage;

            // Trigger lifecycle and potential navigation
            this.Dispatcher.Dispatch(async () =>
            {
                await loadingPage.OnInitializeAsync();
                await loadingPage.OnAppearingAsync();
            });
        }

        /// <summary>
        /// The currently displayed page.
        /// </summary>
        public BaseContentPage? CurrentPage => this.currentPage;

        /// <summary>
        /// Swaps in a new page, with optional transition, updating bars and lifecycle events.
        /// Handles custom lifecycle interface if present.
        /// </summary>
        /// <param name="Page">The new page to show.</param>
        /// <param name="Transition">Transition type (fade, etc).</param>
        public async Task SetPageAsync(BaseContentPage Page, TransitionType Transition = TransitionType.None)
        {
            // Remove previous page (disappearing, dispose)
            if (this.currentPage is not null)
            {
                if (this.currentPage is ILifeCycleView oldLifeCycle)
                    await oldLifeCycle.OnDisappearingAsync();
                // object cleanup moved to NavigationService
            }

            // Host the shell view in the content slot
            this.contentHost.Content = Page;
            this.currentPage = Page;

            // Fire custom appearing lifecycle
            await Page.OnAppearingAsync();

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
        public async Task PushModalAsync(BaseContentPage Page, TransitionType Transition = TransitionType.Fade)
        {
            this.modalHost.Content = Page;
            this.modalStack.Push(Page);

            this.modalBackground.BackgroundColor = BaseModalPage.GetOverlayColor(Page);

            this.modalHost.IsVisible = true;
            this.modalOverlay.IsVisible = true;

            // Fire custom appearing lifecycle
            await Page.OnAppearingAsync();

            if (Transition == TransitionType.Fade)
            {
                this.modalBackground.Opacity = 0;
                await this.modalBackground.FadeTo(1, 150, Easing.CubicOut);
            }
            else
            {
                this.modalBackground.Opacity = 1;
            }
        }

        /// <summary>
        /// Pops the top most modal page.
        /// </summary>
        public async Task PopModalAsync()
        {
            if (this.modalStack.Count == 0)
                return;

            BaseContentPage Page = this.modalStack.Pop();

            // Fire custom disappearing lifecycle
            await Page.OnDisappearingAsync();

            this.modalHost.Content = null;
            this.modalHost.BindingContext = null;

            if (this.modalStack.Count == 0)
            {
                await this.modalBackground.FadeTo(0, 150, Easing.CubicOut);
                this.modalHost.IsVisible = false;
                this.modalOverlay.IsVisible = false;
            }
            else
            {
                BaseContentPage Next = this.modalStack.Peek();
                this.modalHost.Content = Next;
                this.modalBackground.BackgroundColor = BaseModalPage.GetOverlayColor(Next);
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
