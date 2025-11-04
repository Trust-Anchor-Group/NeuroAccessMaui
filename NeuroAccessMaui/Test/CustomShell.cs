using System;
using System.Collections.Generic;
using System.Threading.Tasks;
using CommunityToolkit.Maui.Behaviors;
using CommunityToolkit.Maui.Core;
using Microsoft.Maui.Controls;
using Microsoft.Maui.Controls.PlatformConfiguration;
using Microsoft.Maui.Controls.PlatformConfiguration.iOSSpecific;
using Microsoft.Maui.Controls.Shapes;
using Microsoft.Maui.Graphics;
using NeuroAccessMaui.Services;                     // for ServiceHelper
using NeuroAccessMaui.UI.Pages;
using NeuroAccessMaui.UI.Pages.Main; // for Colors
using NeuroAccessMaui.UI.Pages.Startup;             // for LoadingPage
using NeuroAccessMaui.Services.UI; // for back handling
using NeuroAccessMaui.UI;
using NeuroAccessMaui.UI.Popups;
using ControlsVisualElement = Microsoft.Maui.Controls.VisualElement;

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
        private PopupVisualState? currentPopupState;
        private readonly Grid toastLayer;
        private readonly Thickness toastBasePadding = new Thickness(16, 32, 16, 16);
        private readonly IKeyboardInsetsService keyboardInsetsService;
        private View? activeToast;
        private ToastPlacement activeToastPlacement = ToastPlacement.Top;
        private ContentView? currentScreen;
        private double currentKeyboardInset;
        private bool isKeyboardVisible;
        public event EventHandler? PopupBackgroundTapped;
        public event EventHandler? PopupBackRequested;

        /// <summary>
        /// Initializes a new instance of <see cref="CustomShell"/>.
        /// </summary>
        public CustomShell(LoadingPage loadingPage, IKeyboardInsetsService keyboardInsetsService)
        {
            ArgumentNullException.ThrowIfNull(keyboardInsetsService);
            this.keyboardInsetsService = keyboardInsetsService;
            this.currentKeyboardInset = this.keyboardInsetsService.KeyboardHeight;
            this.isKeyboardVisible = this.keyboardInsetsService.IsKeyboardVisible;
            this.keyboardInsetsService.KeyboardInsetChanged += this.OnKeyboardInsetChanged;

            this.layout = new Grid
            {
                RowDefinitions =
                {
                    new RowDefinition { Height = GridLength.Auto },
                    new RowDefinition { Height = new GridLength(1, GridUnitType.Star) },
                    new RowDefinition { Height = GridLength.Auto }
                }
            };
			this.layout.SetDynamicResource(Grid.BackgroundColorProperty, "SurfaceBackgroundWL");

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

            this.toastLayer = new Grid { IsVisible = false, InputTransparent = false, Padding = this.toastBasePadding, VerticalOptions = LayoutOptions.Fill, HorizontalOptions = LayoutOptions.Fill };
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
            Thickness initialSafeArea = this.ApplyInsetsToHost(this.contentHostA, loadingPageInstance, false, false);
            this.UpdateOverlayInsets(initialSafeArea, this.currentKeyboardInset, false);

            this.Dispatcher.Dispatch(async () =>
            {
                await loadingPage.OnInitializeAsync();
                await loadingPage.OnAppearingAsync();
            });

            this.Behaviors.Add(new StatusBarBehavior { StatusBarColor = Colors.Transparent, StatusBarStyle = StatusBarStyle.Default });
            this.SizeChanged += this.OnShellSizeChanged;
        }

        private void OnPopupBackgroundTapped(object? sender, EventArgs e)
        {
            if (this.currentPopupState is null)
                return;
            if (!this.currentPopupState.AllowBackgroundTap)
                return;
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
            _ = this.ApplyInsetsToHost(inactiveSlot, screen, false, false);
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
            ContentView newlyActiveSlot = this.isSlotAActive ? this.contentHostA : this.contentHostB;
            Thickness safeArea = this.ApplyInsetsToHost(newlyActiveSlot, screen, false, true);
            this.UpdateOverlayInsets(safeArea, this.currentKeyboardInset, false);
        }

        public async Task ShowPopup(ContentView popup, PopupTransition transition, PopupVisualState visualState)
        {
            ArgumentNullException.ThrowIfNull(popup);
            ArgumentNullException.ThrowIfNull(visualState);

            bool overlayWasVisible = this.popupOverlay.IsVisible;
            if (!overlayWasVisible)
            {
                this.popupOverlay.IsVisible = true;
                this.popupBackground.Opacity = 0;
            }

            this.ApplyOverlayInteractionState(visualState);

            double targetOpacity = visualState.OverlayOpacity;
            double currentOpacity = this.popupBackground.Opacity;
            if (!overlayWasVisible)
            {
                await this.popupBackground.FadeTo(targetOpacity, 150, Easing.CubicOut);
            }
            else if (Math.Abs(currentOpacity - targetOpacity) > 0.01)
            {
                await this.popupBackground.FadeTo(targetOpacity, 120, Easing.CubicOut);
            }

            if (popup is BasePopupView popupView)
            {
                popupView.BackgroundTapped -= this.OnPopupBackgroundTapped;
                popupView.BackgroundTapped += this.OnPopupBackgroundTapped;
            }

            this.popupHost.Children.Add(popup);
            this.NotifyKeyboardAwareTargets(popup);
            popup.ZIndex = this.popupHost.Children.Count;

            await this.RunShowAnimationAsync(popup, transition);

            this.currentPopupState = visualState;
        }

        public async Task HidePopup(ContentView popup, PopupTransition transition, PopupVisualState? nextVisualState)
        {
            ArgumentNullException.ThrowIfNull(popup);

            await this.RunHideAnimationAsync(popup, transition);
            if (popup is BasePopupView popupView)
                popupView.BackgroundTapped -= this.OnPopupBackgroundTapped;

            this.popupHost.Children.Remove(popup);

            if (nextVisualState is null)
            {
                await this.popupBackground.FadeTo(0, 150, Easing.CubicOut);
                this.popupOverlay.IsVisible = false;
                this.popupOverlay.InputTransparent = true;
                this.currentPopupState = null;
            }
            else
            {
                this.ApplyOverlayInteractionState(nextVisualState);
                double targetOpacity = nextVisualState.OverlayOpacity;
                double currentOpacity = this.popupBackground.Opacity;
                if (!this.popupOverlay.IsVisible)
                    this.popupOverlay.IsVisible = true;
                if (Math.Abs(currentOpacity - targetOpacity) > 0.01)
                    await this.popupBackground.FadeTo(targetOpacity, 120, Easing.CubicOut);
                this.currentPopupState = nextVisualState;
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
            if (toast is BindableObject toastBindable)
                this.NotifyKeyboardAwareTargets(toastBindable);
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

        private async Task RunShowAnimationAsync(View popup, PopupTransition transition)
        {
            if (transition == PopupTransition.None)
            {
                popup.Opacity = 1;
                popup.Scale = 1;
                popup.TranslationY = 0;
                return;
            }
            if (transition == PopupTransition.Fade)
            {
                popup.Opacity = 0;
                await popup.FadeTo(1, 150, Easing.CubicOut);
                return;
            }
            if (transition == PopupTransition.Scale)
            {
                popup.Scale = 0.9;
                popup.Opacity = 0;
                Task fadeTask = popup.FadeTo(1, 200, Easing.CubicOut);
                Task scaleTask = popup.ScaleTo(1, 200, Easing.CubicOut);
                await Task.WhenAll(fadeTask, scaleTask);
                return;
            }
            if (transition == PopupTransition.SlideUp)
            {
                double fromY = this.Height > 0 ? this.Height : 400;
                popup.TranslationY = fromY * 0.5;
                popup.Opacity = 0;
                Task fadeTask = popup.FadeTo(1, 200, Easing.CubicOut);
                Task translateTask = popup.TranslateTo(0, 0, 200, Easing.CubicOut);
                await Task.WhenAll(fadeTask, translateTask);
            }
        }

        private async Task RunHideAnimationAsync(View popup, PopupTransition transition)
        {
            if (transition == PopupTransition.None)
            {
                popup.Opacity = 0;
                return;
            }
            if (transition == PopupTransition.Fade)
            {
                await popup.FadeTo(0, 150, Easing.CubicIn);
                return;
            }
            if (transition == PopupTransition.Scale)
            {
                Task fadeTask = popup.FadeTo(0, 150, Easing.CubicIn);
                Task scaleTask = popup.ScaleTo(0.9, 150, Easing.CubicIn);
                await Task.WhenAll(fadeTask, scaleTask);
                return;
            }
            if (transition == PopupTransition.SlideUp)
            {
                Task fadeTask = popup.FadeTo(0, 150, Easing.CubicIn);
                Task translateTask = popup.TranslateTo(0, 50, 150, Easing.CubicIn);
                await Task.WhenAll(fadeTask, translateTask);
            }
        }

        private void ApplyOverlayInteractionState(PopupVisualState visualState)
        {
            this.popupOverlay.InputTransparent = false;
        }

        private void OnShellSizeChanged(object? sender, EventArgs e)
        {
            if (this.currentScreen is null)
                return;

            ContentView activeSlot = this.isSlotAActive ? this.contentHostA : this.contentHostB;
            Thickness safeArea = this.ApplyInsetsToHost(activeSlot, this.currentScreen, false, true);
            this.UpdateOverlayInsets(safeArea, this.currentKeyboardInset, false);
        }

        private Thickness ApplyInsetsToHost(ContentView host, BindableObject screen, bool animate, bool notify)
        {
            ArgumentNullException.ThrowIfNull(screen);
            ArgumentNullException.ThrowIfNull(host);

            Thickness safeArea = SafeArea.ResolveInsetsFor(screen);
            KeyboardInsetMode mode = KeyboardInsets.GetMode(screen);
            double keyboardInsetForPadding = mode == KeyboardInsetMode.Automatic ? this.currentKeyboardInset : 0;
            Thickness targetPadding = new Thickness(
                safeArea.Left,
                safeArea.Top,
                safeArea.Right,
                safeArea.Bottom + keyboardInsetForPadding);

            this.SetViewPadding(host, targetPadding, animate);

            if (notify)
                this.NotifyKeyboardAwareTargets(screen);

            return safeArea;
        }

        private void UpdateOverlayInsets(Thickness safeArea, double keyboardInset, bool animate)
        {
            Thickness overlayPadding = new Thickness(safeArea.Left, safeArea.Top, safeArea.Right, safeArea.Bottom + keyboardInset);
            this.SetViewPadding(this.popupOverlay, overlayPadding, animate);

            Thickness toastPadding = new Thickness(
                this.toastBasePadding.Left + safeArea.Left,
                this.toastBasePadding.Top + safeArea.Top,
                this.toastBasePadding.Right + safeArea.Right,
                this.toastBasePadding.Bottom + safeArea.Bottom + keyboardInset);
            this.SetViewPadding(this.toastLayer, toastPadding, animate);

            Thickness topBarPadding = new Thickness(safeArea.Left, safeArea.Top, safeArea.Right, 0);
            this.SetViewPadding(this.topBar, topBarPadding, animate);

            Thickness navBarPadding = new Thickness(safeArea.Left, 0, safeArea.Right, safeArea.Bottom);
            this.SetViewPadding(this.navBar, navBarPadding, animate);
        }

        private void SetViewPadding(ControlsVisualElement view, Thickness targetPadding, bool animate)
        {
            if (!this.TryGetPadding(view, out Thickness currentPadding))
                return;

            if (!animate)
            {
                this.AssignPadding(view, targetPadding);
                return;
            }

            if (this.AreThicknessClose(currentPadding, targetPadding))
            {
                this.AssignPadding(view, targetPadding);
                return;
            }

            const uint duration = 180;
            Microsoft.Maui.Controls.ViewExtensions.CancelAnimations(view);
            view.Animate(
                "KeyboardPadding",
                progress => this.AssignPadding(view, this.InterpolateThickness(currentPadding, targetPadding, progress)),
                rate: 16,
                length: duration,
                easing: Easing.CubicInOut,
                finished: (v, cancelled) => this.AssignPadding(view, targetPadding));
        }

        private bool TryGetPadding(ControlsVisualElement element, out Thickness padding)
        {
            switch (element)
            {
                case Layout layout:
                    padding = layout.Padding;
                    return true;
                case ContentView contentView:
                    padding = contentView.Padding;
                    return true;
                case TemplatedView templatedView:
                    padding = templatedView.Padding;
                    return true;
                case Border border:
                    padding = border.Padding;
                    return true;
                default:
                    padding = new Thickness(0);
                    return false;
            }
        }

        private void AssignPadding(ControlsVisualElement element, Thickness padding)
        {
            switch (element)
            {
                case Layout layout:
                    layout.Padding = padding;
                    break;
                case ContentView contentView:
                    contentView.Padding = padding;
                    break;
                case TemplatedView templatedView:
                    templatedView.Padding = padding;
                    break;
                case Border border:
                    border.Padding = padding;
                    break;
            }
        }

        private Thickness InterpolateThickness(Thickness from, Thickness to, double progress)
        {
            return new Thickness(
                this.InterpolateDouble(from.Left, to.Left, progress),
                this.InterpolateDouble(from.Top, to.Top, progress),
                this.InterpolateDouble(from.Right, to.Right, progress),
                this.InterpolateDouble(from.Bottom, to.Bottom, progress));
        }

        private double InterpolateDouble(double from, double to, double progress)
        {
            return from + ((to - from) * progress);
        }

        private bool AreThicknessClose(Thickness a, Thickness b)
        {
            return Math.Abs(a.Left - b.Left) < 0.5 &&
                   Math.Abs(a.Top - b.Top) < 0.5 &&
                   Math.Abs(a.Right - b.Right) < 0.5 &&
                   Math.Abs(a.Bottom - b.Bottom) < 0.5;
        }

        private void NotifyKeyboardAwareTargets(BindableObject screen)
        {
            KeyboardInsetChangedEventArgs args = new KeyboardInsetChangedEventArgs(this.currentKeyboardInset, this.isKeyboardVisible);

            if (screen is IKeyboardInsetAware awareView)
                awareView.OnKeyboardInsetChanged(args);

            if (screen.BindingContext is IKeyboardInsetAware awareContext)
                awareContext.OnKeyboardInsetChanged(args);
        }

        private void OnKeyboardInsetChanged(object? sender, KeyboardInsetChangedEventArgs e)
        {
            this.currentKeyboardInset = e.KeyboardHeight;
            this.isKeyboardVisible = e.IsVisible;

            if (this.currentScreen is not null)
            {
                ContentView activeSlot = this.isSlotAActive ? this.contentHostA : this.contentHostB;
                Thickness safeArea = this.ApplyInsetsToHost(activeSlot, this.currentScreen, true, true);
                this.UpdateOverlayInsets(safeArea, this.currentKeyboardInset, true);
            }

            foreach (Microsoft.Maui.IView child in this.popupHost.Children)
            {
                if (child is BindableObject bindableChild)
                    this.NotifyKeyboardAwareTargets(bindableChild);
            }

            if (this.activeToast is BindableObject toastBindable)
                this.NotifyKeyboardAwareTargets(toastBindable);
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

            if (ReferenceEquals(screen, this.currentScreen))
            {
                ContentView activeSlot = this.isSlotAActive ? this.contentHostA : this.contentHostB;
                Thickness safeArea = this.ApplyInsetsToHost(activeSlot, screen, false, true);
                this.UpdateOverlayInsets(safeArea, this.currentKeyboardInset, false);
            }
        }

        protected override bool OnBackButtonPressed()
        {
            try
            {
                if (this.popupHost.Children.Count > 0)
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
