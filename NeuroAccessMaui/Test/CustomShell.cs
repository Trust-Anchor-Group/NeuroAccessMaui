using System;
using System.Threading.Tasks;
using CommunityToolkit.Maui.Behaviors;
using CommunityToolkit.Maui.Core;
using Microsoft.Maui.Controls;
using Microsoft.Maui.Controls.PlatformConfiguration;
using Microsoft.Maui.Controls.PlatformConfiguration.iOSSpecific;
using Microsoft.Maui.Graphics;
using NeuroAccessMaui.Services;                     // for ServiceHelper
using NeuroAccessMaui.UI.Pages;
using NeuroAccessMaui.UI.Pages.Main; // for Colors
using NeuroAccessMaui.UI.Pages.Startup;             // for LoadingPage
using NeuroAccessMaui.Services.UI; // for back handling

namespace NeuroAccessMaui.Test
{
    /// <summary>
    /// Custom shell with edge-to-edge layout, dynamic top/nav bars, and transition support.
    /// Handles both normal ContentPage and ILifeCycleView pages.
    /// </summary>
    public class CustomShell : ContentPage, IShellPresenter
    {
        private readonly Grid layout;
        private readonly ContentView topBar;
        private readonly ContentView navBar;
        private readonly ContentView contentHost;  // For hosting page content
        private readonly Grid modalOverlay;
        private readonly BoxView modalBackground;
        private readonly ContentView modalHost;
        private ContentView? currentScreen;
        public event EventHandler? ModalBackgroundTapped;

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
            BackgroundTap.Tapped += (_, _) => this.ModalBackgroundTapped?.Invoke(this, EventArgs.Empty);

            this.modalBackground.GestureRecognizers.Add(BackgroundTap);


            this.layout.Add(this.topBar, 0, 0);
            this.layout.Add(this.contentHost, 0, 1);
            this.layout.Add(this.navBar, 0, 2);
            this.layout.Add(this.modalOverlay);
            Grid.SetRowSpan(this.modalOverlay, 3);

            this.Content = this.layout;
            this.Padding = 0;
            this.On<iOS>().SetUseSafeArea(false);


            // Shell background
            this.BackgroundColor = Colors.White;
            // Show initial loading page immediately
            LoadingPage loadingPage = ServiceHelper.GetService<LoadingPage>();

            // Assign content synchronously so spinner is visible right away
            this.contentHost.Content = loadingPage.Content;
            this.contentHost.BindingContext = loadingPage.BindingContext;
            this.currentScreen = loadingPage;

            // Trigger lifecycle and potential navigation
            this.Dispatcher.Dispatch(async () =>
            {
                await loadingPage.OnInitializeAsync();
                await loadingPage.OnAppearingAsync();
            });

            this.Behaviors.Add(new StatusBarBehavior
            {
                StatusBarColor = Colors.Transparent,
                StatusBarStyle = StatusBarStyle.Default
            });
        }

        /// <summary>
        /// The currently displayed page.
        /// </summary>
        public BaseContentPage? CurrentPage => this.currentScreen as BaseContentPage;

        /// <summary>
        /// Swaps in a new page, with optional transition, updating bars and lifecycle events.
        /// Handles custom lifecycle interface if present.
        /// </summary>
        /// <param name="Page">The new page to show.</param>
        /// <param name="Transition">Transition type (fade, etc).</param>
        // Backwards compatibility for existing code still calling SetPageAsync
        public Task SetPageAsync(BaseContentPage Page, TransitionType Transition = TransitionType.None) => this.ShowScreen(Page, Transition);

        public async Task ShowScreen(ContentView screen, TransitionType transition = TransitionType.None)
        {
            this.contentHost.Content = screen;
            this.currentScreen = screen;
            this.UpdateBars(screen);
            if (transition == TransitionType.Fade)
            {
                screen.Opacity = 0;
                await screen.FadeTo(1, 150, Easing.CubicOut);
            }
        }

        /// <summary>
        /// (Legacy doc removed) Use ShowModal instead via IShellPresenter.
        /// </summary>
        public async Task ShowModal(ContentView screen, TransitionType transition = TransitionType.Fade)
        {
            this.modalHost.Content = screen;
            this.modalBackground.Opacity = 0;
            this.modalHost.IsVisible = true;
            this.modalOverlay.IsVisible = true;
            if (transition == TransitionType.Fade)
                await this.modalBackground.FadeTo(1, 150, Easing.CubicOut);
            else
                this.modalBackground.Opacity = 1;
        }

        /// <summary>
        /// Pops the top most modal page.
        /// </summary>
        public async Task HideTopModal(TransitionType transition = TransitionType.Fade)
        {
            if (!this.modalOverlay.IsVisible)
                return;
            this.modalHost.Content = null;
            this.modalHost.BindingContext = null;
            if (transition == TransitionType.Fade)
                await this.modalBackground.FadeTo(0, 150, Easing.CubicOut);
            this.modalHost.IsVisible = false;
            this.modalOverlay.IsVisible = false;
        }

        public void UpdateBars(BindableObject screen)
        {
            bool topVisible = NavigationBars.GetTopBarVisible(screen);
            bool navVisible = NavigationBars.GetNavBarVisible(screen);
            this.topBar.IsVisible = topVisible;
            this.navBar.IsVisible = navVisible;
            if (screen is IBarContentProvider provider)
            {
                this.topBar.Content = provider.TopBarContent;
                this.navBar.Content = provider.NavBarContent;
            }
            else
            {
                this.topBar.Content = null;
                this.navBar.Content = null;
            }
        }

        protected override bool OnBackButtonPressed()
        {
            try
            {
                NeuroAccessMaui.Test.NavigationService? nav = ServiceRef.Provider.GetService<INavigationService>() as NeuroAccessMaui.Test.NavigationService;
                if (nav is not null && nav.WouldHandleBack())
                {
                    _ = this.Dispatcher.DispatchAsync(async () =>
                    {
                        bool handled = await nav.HandleBackAsync();
                        if (!handled)
                        {
#if ANDROID || WINDOWS
                            try { Microsoft.Maui.Controls.Application.Current?.Quit(); } catch { }
#endif
                        }
                    });
                    return true;
                }
            }
            catch (Exception ex)
            {
                ServiceRef.LogService.LogException(ex);
            }
            return base.OnBackButtonPressed();
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
