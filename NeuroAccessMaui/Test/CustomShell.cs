using System;
using System.Collections.Generic;
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
        private readonly ContentView contentHostA;  // Slot A
        private readonly ContentView contentHostB;  // Slot B
        private bool isSlotAActive = true; // Track which slot is active
        private readonly Grid modalOverlay;
        private readonly BoxView modalBackground;
        private readonly ContentView modalHost;
        private readonly Grid popupOverlay;
        private readonly BoxView popupBackground;
        private readonly Grid popupHost;
        private readonly Stack<ContentView> popupStack = new();
        private readonly Grid toastLayer;
        private View? activeToast;
        private ToastPlacement activeToastPlacement = ToastPlacement.Top;
        private ContentView? currentScreen;
        public event EventHandler? ModalBackgroundTapped;
        public event EventHandler? PopupBackgroundTapped;
        public event EventHandler? PopupBackRequested;

        /// <summary>
        /// Initializes a new instance of <see cref="CustomShell"/>.
        /// </summary>
        public CustomShell(LoadingPage LoadingPage)
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
            this.contentHostA = new ContentView();
            this.contentHostB = new ContentView();
            this.contentHostA.IsVisible = true;
            this.contentHostB.IsVisible = false;
            this.modalHost = new ContentView { IsVisible = false };
            this.modalBackground = new BoxView { Opacity = 0 };
            this.modalOverlay = new Grid { IsVisible = false, InputTransparent = false };
            this.modalOverlay.Add(this.modalBackground);
            this.modalOverlay.Add(this.modalHost);

            TapGestureRecognizer BackgroundTap = new();
            BackgroundTap.Tapped += (_, _) => this.ModalBackgroundTapped?.Invoke(this, EventArgs.Empty);

            this.modalBackground.GestureRecognizers.Add(BackgroundTap);

            this.popupBackground = new BoxView { Opacity = 0, BackgroundColor = Colors.Black };
            this.popupOverlay = new Grid { IsVisible = false, InputTransparent = false };
            this.popupOverlay.Add(this.popupBackground);
            this.popupHost = new Grid
            {
                VerticalOptions = LayoutOptions.Center,
                HorizontalOptions = LayoutOptions.Center,
                RowSpacing = 0,
                ColumnSpacing = 0,
                Padding = 24
            };
            this.popupOverlay.Add(this.popupHost);

            TapGestureRecognizer PopupBackgroundTap = new();
            PopupBackgroundTap.Tapped += (_, _) => this.PopupBackgroundTapped?.Invoke(this, EventArgs.Empty);
            this.popupBackground.GestureRecognizers.Add(PopupBackgroundTap);

            this.toastLayer = new Grid
            {
                IsVisible = false,
                InputTransparent = false,
                Padding = new Thickness(16, 32, 16, 16),
                VerticalOptions = LayoutOptions.Fill,
                HorizontalOptions = LayoutOptions.Fill
            };
            this.toastLayer.RowDefinitions.Add(new RowDefinition { Height = GridLength.Auto });
            this.toastLayer.RowDefinitions.Add(new RowDefinition { Height = new GridLength(1, GridUnitType.Star) });
            this.toastLayer.RowDefinitions.Add(new RowDefinition { Height = GridLength.Auto });

            this.layout.Add(this.topBar, 0, 0);
            this.layout.Add(this.contentHostA, 0, 1);
            this.layout.Add(this.contentHostB, 0, 1);
            this.layout.Add(this.navBar, 0, 2);
            this.layout.Add(this.modalOverlay);
            Grid.SetRowSpan(this.modalOverlay, 3);
            this.layout.Add(this.popupOverlay);
            Grid.SetRowSpan(this.popupOverlay, 3);
            this.layout.Add(this.toastLayer);
            Grid.SetRowSpan(this.toastLayer, 3);

            this.Content = this.layout;
            this.Padding = 0;
            this.On<iOS>().SetUseSafeArea(false);


            // Shell background
            this.BackgroundColor = Colors.White;

            // Assign content synchronously so spinner is visible right away
            LoadingPage loadingPage = ServiceHelper.GetService<LoadingPage>();
            this.contentHostA.Content = loadingPage.Content;
            this.contentHostA.BindingContext = loadingPage.BindingContext;
            this.contentHostB.Content = null;
            this.currentScreen = loadingPage;

            // Trigger lifecycle and potential navigation
            this.Dispatcher.Dispatch(async () =>
            {
                await LoadingPage.OnInitializeAsync();
                await LoadingPage.OnAppearingAsync();
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
            // Determine active/inactive slot
            ContentView ActiveSlot = this.isSlotAActive ? this.contentHostA : this.contentHostB;
            ContentView InactiveSlot = this.isSlotAActive ? this.contentHostB : this.contentHostA;

            // Place new content in inactive slot
            InactiveSlot.Content = screen;
            InactiveSlot.BindingContext = screen.BindingContext;
            InactiveSlot.IsVisible = true;
            this.UpdateBars(screen);

            if (transition == TransitionType.Fade)
            {
                // Fade in new content over old
                InactiveSlot.Opacity = 0;
                ActiveSlot.Opacity = 1;
                await InactiveSlot.FadeTo(1, 200, Easing.CubicOut);
                // After fade, hide old slot
                ActiveSlot.IsVisible = false;
                ActiveSlot.Content = null;
            }
            else if (transition == TransitionType.SwipeLeft || transition == TransitionType.SwipeRight)
            {
                // Slide new content in, old out
                double width = this.Width > 0 ? this.Width : 400; // fallback width
                double fromX = (transition == TransitionType.SwipeLeft) ? width : -width;
                double toX = 0;
                double oldToX = (transition == TransitionType.SwipeLeft) ? -width : width;

                InactiveSlot.TranslationX = fromX;
                ActiveSlot.TranslationX = 0;

                Task newIn = InactiveSlot.TranslateTo(toX, 0, 250, Easing.CubicOut);
                Task oldOut = ActiveSlot.TranslateTo(oldToX, 0, 250, Easing.CubicOut);
                await Task.WhenAll(newIn, oldOut);

                // Reset transforms and cleanup
                InactiveSlot.TranslationX = 0;
                ActiveSlot.TranslationX = 0;
                ActiveSlot.IsVisible = false;
                ActiveSlot.Content = null;
            }
            else
            {
                // No transition: just swap
                ActiveSlot.IsVisible = false;
                ActiveSlot.Content = null;
            }

            // Swap active slot
            this.isSlotAActive = !this.isSlotAActive;
            this.currentScreen = screen;
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

        public async Task ShowPopup(ContentView popup, PopupTransition transition = PopupTransition.Fade, double overlayOpacity = 0.7)
        {
            ArgumentNullException.ThrowIfNull(popup);

            if (!this.popupOverlay.IsVisible)
            {
                this.popupOverlay.IsVisible = true;
                this.popupBackground.Opacity = 0;
                await this.popupBackground.FadeTo(overlayOpacity, 150, Easing.CubicOut);
            }
            else
            {
                await this.popupBackground.FadeTo(overlayOpacity, 120, Easing.CubicOut);
            }

            popup.Opacity = 1;
            popup.Scale = 1;
            popup.TranslationY = 0;
            popup.ZIndex = this.popupStack.Count + 1;
            this.popupHost.Children.Add(popup);
            this.popupStack.Push(popup);

            if (transition == PopupTransition.Fade)
            {
                popup.Opacity = 0;
                await popup.FadeTo(1, 150, Easing.CubicOut);
            }
            else if (transition == PopupTransition.Scale)
            {
                popup.Scale = 0.9;
                popup.Opacity = 0;
                Task FadeTask = popup.FadeTo(1, 200, Easing.CubicOut);
                Task ScaleTask = popup.ScaleTo(1, 200, Easing.CubicOut);
                await Task.WhenAll(FadeTask, ScaleTask);
            }
            else if (transition == PopupTransition.SlideUp)
            {
                double FromY = this.Height > 0 ? this.Height : 400;
                popup.TranslationY = FromY * 0.5;
                popup.Opacity = 0;
                Task FadeTask = popup.FadeTo(1, 200, Easing.CubicOut);
                Task TranslateTask = popup.TranslateTo(0, 0, 200, Easing.CubicOut);
                await Task.WhenAll(FadeTask, TranslateTask);
            }
        }

        public async Task HideTopPopup(PopupTransition transition = PopupTransition.Fade)
        {
            if (this.popupStack.Count == 0)
                return;

            ContentView TopPopup = this.popupStack.Pop();

            if (transition == PopupTransition.Fade)
            {
                await TopPopup.FadeTo(0, 150, Easing.CubicIn);
            }
            else if (transition == PopupTransition.Scale)
            {
                Task FadeTask = TopPopup.FadeTo(0, 150, Easing.CubicIn);
                Task ScaleTask = TopPopup.ScaleTo(0.9, 150, Easing.CubicIn);
                await Task.WhenAll(FadeTask, ScaleTask);
            }
            else if (transition == PopupTransition.SlideUp)
            {
                Task FadeTask = TopPopup.FadeTo(0, 150, Easing.CubicIn);
                Task TranslateTask = TopPopup.TranslateTo(0, 50, 150, Easing.CubicIn);
                await Task.WhenAll(FadeTask, TranslateTask);
            }

            this.popupHost.Children.Remove(TopPopup);

            if (this.popupStack.Count == 0)
            {
                await this.popupBackground.FadeTo(0, 150, Easing.CubicOut);
                this.popupOverlay.IsVisible = false;
            }
        }

        public async Task ShowToast(View toast, ToastTransition transition = ToastTransition.SlideFromTop, ToastPlacement placement = ToastPlacement.Top)
        {
            ArgumentNullException.ThrowIfNull(toast);

            if (this.activeToast is not null)
            {
                this.toastLayer.Children.Remove(this.activeToast);
            }

            this.activeToast = toast;
            this.activeToastPlacement = placement;
            int TargetRow = placement == ToastPlacement.Top ? 0 : 2;
            Grid.SetRow(toast, TargetRow);
            if (!this.toastLayer.Children.Contains(toast))
                this.toastLayer.Children.Add(toast);

            this.toastLayer.IsVisible = true;
            toast.Opacity = 1;
            toast.TranslationY = 0;

            if (transition == ToastTransition.Fade)
            {
                toast.Opacity = 0;
                await toast.FadeTo(1, 150, Easing.CubicOut);
            }
            else if (transition == ToastTransition.SlideFromBottom || (transition == ToastTransition.SlideFromTop && placement == ToastPlacement.Bottom))
            {
                double Offset = this.Height > 0 ? this.Height : 400;
                toast.TranslationY = Offset * 0.1;
                toast.Opacity = 0;
                Task FadeTask = toast.FadeTo(1, 150, Easing.CubicOut);
                Task TranslateTask = toast.TranslateTo(0, 0, 150, Easing.CubicOut);
                await Task.WhenAll(FadeTask, TranslateTask);
            }
            else if (transition == ToastTransition.SlideFromTop)
            {
                double Offset = this.Height > 0 ? this.Height : 400;
                toast.TranslationY = -Offset * 0.1;
                toast.Opacity = 0;
                Task FadeTask = toast.FadeTo(1, 150, Easing.CubicOut);
                Task TranslateTask = toast.TranslateTo(0, 0, 150, Easing.CubicOut);
                await Task.WhenAll(FadeTask, TranslateTask);
            }
        }

        public async Task HideToast(ToastTransition transition = ToastTransition.SlideFromTop)
        {
            if (this.activeToast is null)
                return;

            View Toast = this.activeToast;

            if (transition == ToastTransition.Fade)
            {
                await Toast.FadeTo(0, 120, Easing.CubicIn);
            }
            else if (transition == ToastTransition.SlideFromBottom || (transition == ToastTransition.SlideFromTop && this.activeToastPlacement == ToastPlacement.Bottom))
            {
                Task FadeTask = Toast.FadeTo(0, 120, Easing.CubicIn);
                Task TranslateTask = Toast.TranslateTo(0, 40, 120, Easing.CubicIn);
                await Task.WhenAll(FadeTask, TranslateTask);
            }
            else if (transition == ToastTransition.SlideFromTop)
            {
                Task FadeTask = Toast.FadeTo(0, 120, Easing.CubicIn);
                Task TranslateTask = Toast.TranslateTo(0, -40, 120, Easing.CubicIn);
                await Task.WhenAll(FadeTask, TranslateTask);
            }

            this.toastLayer.Children.Remove(Toast);
            this.activeToast = null;
            this.activeToastPlacement = ToastPlacement.Top;
            if (this.toastLayer.Children.Count == 0)
                this.toastLayer.IsVisible = false;
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
                if (this.popupStack.Count > 0)
                {
                    this.PopupBackRequested?.Invoke(this, EventArgs.Empty);
                    return true;
                }

                if (this.activeToast is not null)
                {
                    _ = this.Dispatcher.DispatchAsync(async () => await this.HideToast());
                    return true;
                }

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
        SwipeLeft,
        SwipeRight,
        // Extend: Scale, Flip, etc.
    }

    public enum PopupTransition
    {
        None,
        Fade,
        Scale,
        SlideUp
    }

    public enum ToastTransition
    {
        Fade,
        SlideFromTop,
        SlideFromBottom
    }

    public enum ToastPlacement
    {
        Top,
        Bottom
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
