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
    /// Custom shell hosting app content, popups and toast layers.
    /// </summary>
    public class CustomShell : ContentPage, IShellPresenter
    {
        private readonly Grid layout;
        private readonly ContentView topBar;
        private readonly ContentView navBar;
        private readonly ContentView contentHostA;
        private readonly ContentView contentHostB;
        private bool isSlotAActive = true;
        private readonly Grid popupOverlay;
        private readonly BoxView popupBackground;
        private readonly Grid popupHost;
        private readonly Stack<PopupState> popupStack = new();
        private readonly Grid toastLayer;
        private View? activeToast;
        private ToastPlacement activeToastPlacement = ToastPlacement.Top;
        private ContentView? currentScreen;
        public event EventHandler? PopupBackgroundTapped;
        public event EventHandler? PopupBackRequested;

        private class PopupState
        {
            public PopupState(ContentView view, bool isBlocking, bool disableBackgroundTap, double overlayOpacity)
            {
                this.View = view;
                this.IsBlocking = isBlocking;
                this.DisableBackgroundTap = disableBackgroundTap;
                this.OverlayOpacity = overlayOpacity;
            }
            public ContentView View { get; }
            public bool IsBlocking { get; }
            public bool DisableBackgroundTap { get; }
            public double OverlayOpacity { get; }
        }

        /// <summary>
        /// Initializes a new instance of <see cref="CustomShell"/>.
        /// </summary>
        public CustomShell(LoadingPage loadingPage)
        {
            this.layout = new Grid
            {
                RowDefinitions =
                {
                    new RowDefinition { Height = GridLength.Auto },
                    new RowDefinition { Height = new GridLength(1, GridUnitType.Star) },
                    new RowDefinition { Height = GridLength.Auto }
                }
            };
            this.layout.BackgroundColor = Colors.White;

            this.topBar = new ContentView { IsVisible = false };
            this.navBar = new ContentView { IsVisible = false };
            this.contentHostA = new ContentView();
            this.contentHostB = new ContentView();
            this.contentHostA.IsVisible = true;
            this.contentHostB.IsVisible = false;

            this.popupBackground = new BoxView { Opacity = 0, BackgroundColor = Colors.Black };
            this.popupOverlay = new Grid { IsVisible = false, InputTransparent = false };
            this.popupOverlay.Add(this.popupBackground);
            this.popupHost = new Grid { VerticalOptions = LayoutOptions.Fill, HorizontalOptions = LayoutOptions.Fill };
            this.popupOverlay.Add(this.popupHost);

            TapGestureRecognizer popupBackgroundTap = new();
            popupBackgroundTap.Tapped += this.OnPopupBackgroundTapped;
            this.popupBackground.GestureRecognizers.Add(popupBackgroundTap);

            this.toastLayer = new Grid { IsVisible = false, InputTransparent = false, Padding = new Thickness(16, 32, 16, 16), VerticalOptions = LayoutOptions.Fill, HorizontalOptions = LayoutOptions.Fill };
            this.toastLayer.RowDefinitions.Add(new RowDefinition { Height = GridLength.Auto });
            this.toastLayer.RowDefinitions.Add(new RowDefinition { Height = new GridLength(1, GridUnitType.Star) });
            this.toastLayer.RowDefinitions.Add(new RowDefinition { Height = GridLength.Auto });

            this.layout.Add(this.topBar, 0, 0);
            this.layout.Add(this.contentHostA, 0, 1);
            this.layout.Add(this.contentHostB, 0, 1);
            this.layout.Add(this.navBar, 0, 2);
            this.layout.Add(this.popupOverlay);
            Grid.SetRowSpan(this.popupOverlay, 3);
            this.layout.Add(this.toastLayer);
            Grid.SetRowSpan(this.toastLayer, 3);

            this.Content = this.layout;
            this.Padding = 0;
            this.On<iOS>().SetUseSafeArea(false);
            this.BackgroundColor = Colors.White;

            LoadingPage loadingPageInstance = ServiceHelper.GetService<LoadingPage>();
            this.contentHostA.Content = loadingPageInstance;
            this.contentHostB.Content = null;
            this.currentScreen = loadingPageInstance;

            this.Dispatcher.Dispatch(async () =>
            {
                await loadingPage.OnInitializeAsync();
                await loadingPage.OnAppearingAsync();
            });

            this.Behaviors.Add(new StatusBarBehavior { StatusBarColor = Colors.Transparent, StatusBarStyle = StatusBarStyle.Default });
        }

        private void OnPopupBackgroundTapped(object? sender, EventArgs e)
        {
            if (this.popupStack.Count == 0)
                return;
            PopupState top = this.popupStack.Peek();
            if (!top.DisableBackgroundTap)
                this.PopupBackgroundTapped?.Invoke(this, EventArgs.Empty);
        }

        public BaseContentPage? CurrentPage => this.currentScreen as BaseContentPage;

        public async Task ShowScreen(ContentView screen, TransitionType transition = TransitionType.Fade)
        {
            ContentView activeSlot = this.isSlotAActive ? this.contentHostA : this.contentHostB;
            ContentView inactiveSlot = this.isSlotAActive ? this.contentHostB : this.contentHostA;
            BaseContentPage? outgoingPage = activeSlot.Content as BaseContentPage;

            inactiveSlot.Content = screen;
            inactiveSlot.BindingContext = screen.BindingContext;
            inactiveSlot.IsVisible = true;
            this.UpdateBars(screen);

            if (transition == TransitionType.Fade)
            {
                inactiveSlot.Opacity = 0;
                activeSlot.Opacity = 1;
                const uint duration = 300;
                Task fadeIn = inactiveSlot.FadeTo(1, duration, Easing.Linear);
                Task fadeOut = activeSlot.FadeTo(0, duration, Easing.Linear);
                await Task.WhenAll(fadeIn, fadeOut);
            }
            else if (transition == TransitionType.SwipeLeft || transition == TransitionType.SwipeRight)
            {
                double width = this.Width > 0 ? this.Width : 400;
                double fromX = (transition == TransitionType.SwipeLeft) ? width : -width;
                double oldToX = (transition == TransitionType.SwipeLeft) ? -width : width;
                inactiveSlot.TranslationX = fromX;
                activeSlot.TranslationX = 0;
                Task newIn = inactiveSlot.TranslateTo(0, 0, 250, Easing.CubicOut);
                Task oldOut = activeSlot.TranslateTo(oldToX, 0, 250, Easing.CubicOut);
                await Task.WhenAll(newIn, oldOut);
                inactiveSlot.TranslationX = 0;
                activeSlot.TranslationX = 0;
            }

            if (outgoingPage is not null)
            {
                try { await outgoingPage.OnDisappearingAsync(); }
                catch (Exception ex) { ServiceRef.LogService.LogException(ex); }
            }

            activeSlot.IsVisible = false;
            activeSlot.Content = null;
            this.isSlotAActive = !this.isSlotAActive;
            this.currentScreen = screen;
        }

        public Task ShowPopup(ContentView popup, PopupTransition transition = PopupTransition.Fade, double overlayOpacity = 0.7)
            => this.ShowPopupInternal(popup, transition, overlayOpacity, isBlocking: false, disableBackgroundTap: false);

        private async Task ShowPopupInternal(ContentView popup, PopupTransition transition, double overlayOpacity, bool isBlocking, bool disableBackgroundTap)
        {
            ArgumentNullException.ThrowIfNull(popup);
            double targetOpacity = overlayOpacity;

            if (!this.popupOverlay.IsVisible)
            {
                this.popupOverlay.IsVisible = true;
                this.popupBackground.Opacity = 0;
                await this.popupBackground.FadeTo(targetOpacity, 150, Easing.CubicOut);
            }
            else
            {
                double currentOpacity = this.popupBackground.Opacity;
                if (Math.Abs(currentOpacity - targetOpacity) > 0.01)
                    await this.popupBackground.FadeTo(targetOpacity, 120, Easing.CubicOut);
            }

            popup.Opacity = 1;
            popup.Scale = 1;
            popup.TranslationY = 0;
            popup.ZIndex = this.popupStack.Count + 1;
            this.popupHost.Children.Add(popup);
            this.popupStack.Push(new PopupState(popup, isBlocking, disableBackgroundTap, overlayOpacity));

            if (transition == PopupTransition.Fade)
            {
                popup.Opacity = 0;
                await popup.FadeTo(1, 150, Easing.CubicOut);
            }
            else if (transition == PopupTransition.Scale)
            {
                popup.Scale = 0.9;
                popup.Opacity = 0;
                Task fadeTask = popup.FadeTo(1, 200, Easing.CubicOut);
                Task scaleTask = popup.ScaleTo(1, 200, Easing.CubicOut);
                await Task.WhenAll(fadeTask, scaleTask);
            }
            else if (transition == PopupTransition.SlideUp)
            {
                double fromY = this.Height > 0 ? this.Height : 400;
                popup.TranslationY = fromY * 0.5;
                popup.Opacity = 0;
                Task fadeTask = popup.FadeTo(1, 200, Easing.CubicOut);
                Task translateTask = popup.TranslateTo(0, 0, 200, Easing.CubicOut);
                await Task.WhenAll(fadeTask, translateTask);
            }
        }

        public async Task HideTopPopup(PopupTransition transition = PopupTransition.Fade)
        {
            if (this.popupStack.Count == 0)
                return;
            PopupState state = this.popupStack.Pop();
            ContentView topPopup = state.View;

            if (transition == PopupTransition.Fade)
            {
                await topPopup.FadeTo(0, 150, Easing.CubicIn);
            }
            else if (transition == PopupTransition.Scale)
            {
                Task fadeTask = topPopup.FadeTo(0, 150, Easing.CubicIn);
                Task scaleTask = topPopup.ScaleTo(0.9, 150, Easing.CubicIn);
                await Task.WhenAll(fadeTask, scaleTask);
            }
            else if (transition == PopupTransition.SlideUp)
            {
                Task fadeTask = topPopup.FadeTo(0, 150, Easing.CubicIn);
                Task translateTask = topPopup.TranslateTo(0, 50, 150, Easing.CubicIn);
                await Task.WhenAll(fadeTask, translateTask);
            }
            this.popupHost.Children.Remove(topPopup);

            if (this.popupStack.Count == 0)
            {
                await this.popupBackground.FadeTo(0, 150, Easing.CubicOut);
                this.popupOverlay.IsVisible = false;
            }
            else
            {
                PopupState newTop = this.popupStack.Peek();
                double desiredOpacity = newTop.OverlayOpacity;
                double currentOpacity = this.popupBackground.Opacity;
                if (Math.Abs(currentOpacity - desiredOpacity) > 0.01)
                    await this.popupBackground.FadeTo(desiredOpacity, 120, Easing.CubicOut);
            }
        }

        public async Task ShowToast(View toast, ToastTransition transition = ToastTransition.SlideFromTop, ToastPlacement placement = ToastPlacement.Top)
        {
            ArgumentNullException.ThrowIfNull(toast);
            if (this.activeToast is not null)
                this.toastLayer.Children.Remove(this.activeToast);
            this.activeToast = toast;
            this.activeToastPlacement = placement;
            int targetRow = placement == ToastPlacement.Top ? 0 : 2;
            Grid.SetRow(toast, targetRow);
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
                double offset = this.Height > 0 ? this.Height : 400;
                toast.TranslationY = offset * 0.1;
                toast.Opacity = 0;
                Task fadeTask = toast.FadeTo(1, 150, Easing.CubicOut);
                Task translateTask = toast.TranslateTo(0, 0, 150, Easing.CubicOut);
                await Task.WhenAll(fadeTask, translateTask);
            }
            else if (transition == ToastTransition.SlideFromTop)
            {
                double offset = this.Height > 0 ? this.Height : 400;
                toast.TranslationY = -offset * 0.1;
                toast.Opacity = 0;
                Task fadeTask = toast.FadeTo(1, 150, Easing.CubicOut);
                Task translateTask = toast.TranslateTo(0, 0, 150, Easing.CubicOut);
                await Task.WhenAll(fadeTask, translateTask);
            }
        }

        public async Task HideToast(ToastTransition transition = ToastTransition.SlideFromTop)
        {
            if (this.activeToast is null)
                return;
            View toast = this.activeToast;
            if (transition == ToastTransition.Fade)
            {
                await toast.FadeTo(0, 120, Easing.CubicIn);
            }
            else if (transition == ToastTransition.SlideFromBottom || (transition == ToastTransition.SlideFromTop && this.activeToastPlacement == ToastPlacement.Bottom))
            {
                Task fadeTask = toast.FadeTo(0, 120, Easing.CubicIn);
                Task translateTask = toast.TranslateTo(0, 40, 120, Easing.CubicIn);
                await Task.WhenAll(fadeTask, translateTask);
            }
            else if (transition == ToastTransition.SlideFromTop)
            {
                Task fadeTask = toast.FadeTo(0, 120, Easing.CubicIn);
                Task translateTask = toast.TranslateTo(0, -40, 120, Easing.CubicIn);
                await Task.WhenAll(fadeTask, translateTask);
            }
            this.toastLayer.Children.Remove(toast);
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
                NavigationService? nav = ServiceRef.Provider.GetService<INavigationService>() as NavigationService;
                if (nav is not null && nav.WouldHandleBack())
                {
                    _ = this.Dispatcher.DispatchAsync(async () => await nav.HandleBackAsync());
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
        SwipeRight
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

        public static bool GetTopBarVisible(BindableObject View) => (bool)View.GetValue(TopBarVisibleProperty);
        public static void SetTopBarVisible(BindableObject View, bool Value) => View.SetValue(TopBarVisibleProperty, Value);
        public static bool GetNavBarVisible(BindableObject View) => (bool)View.GetValue(NavBarVisibleProperty);
        public static void SetNavBarVisible(BindableObject View, bool Value) => View.SetValue(NavBarVisibleProperty, Value);
    }
}
