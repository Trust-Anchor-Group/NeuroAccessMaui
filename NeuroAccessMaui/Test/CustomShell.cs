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
            this.BackgroundColor = Colors.White;

            // Assign the page instance directly (avoids reparenting its internal Content tree)
            LoadingPage loadingPage = ServiceHelper.GetService<LoadingPage>();
            this.contentHostA.Content = loadingPage; // changed: use page instance
            this.contentHostB.Content = null;
            this.currentScreen = loadingPage;

            // Trigger lifecycle
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

        /// <inheritdoc/>
        public Task SetPageAsync(BaseContentPage Page, TransitionType Transition = TransitionType.Fade) => this.ShowScreen(Page, Transition);

        /// <summary>
        /// Shows a screen with transition, calling lifecycle methods on outgoing page.
        /// </summary>
        public async Task ShowScreen(ContentView screen, TransitionType transition = TransitionType.Fade)
        {
            ContentView ActiveSlot = this.isSlotAActive ? this.contentHostA : this.contentHostB;
            ContentView InactiveSlot = this.isSlotAActive ? this.contentHostB : this.contentHostA;
            BaseContentPage? OutgoingPage = ActiveSlot.Content as BaseContentPage;

            InactiveSlot.Content = screen;
            InactiveSlot.BindingContext = screen.BindingContext;
            InactiveSlot.IsVisible = true;
            this.UpdateBars(screen);

            if (transition == TransitionType.Fade)
            {
                InactiveSlot.Opacity = 0;
                ActiveSlot.Opacity = 1;
                const uint Dur = 300;

                Task FadeIn  = InactiveSlot.FadeTo(1, Dur, Easing.Linear); 
                Task FadeOut = ActiveSlot.FadeTo(0, Dur, Easing.Linear);   

                await Task.WhenAll(FadeIn, FadeOut);
            }
            else if (transition == TransitionType.SwipeLeft || transition == TransitionType.SwipeRight)
            {
                double Width = this.Width > 0 ? this.Width : 400;
                double FromX = (transition == TransitionType.SwipeLeft) ? Width : -Width;
                double OldToX = (transition == TransitionType.SwipeLeft) ? -Width : Width;
                InactiveSlot.TranslationX = FromX;
                ActiveSlot.TranslationX = 0;
                Task NewIn = InactiveSlot.TranslateTo(0, 0, 250, Easing.CubicOut);
                Task OldOut = ActiveSlot.TranslateTo(OldToX, 0, 250, Easing.CubicOut);
                await Task.WhenAll(NewIn, OldOut);
                InactiveSlot.TranslationX = 0;
                ActiveSlot.TranslationX = 0;
            }
            // else None: no animation

            // Call disappearing on outgoing page before removing visuals
            if (OutgoingPage is not null)
            {
                try
                {
                    await OutgoingPage.OnDisappearingAsync();
                }
                catch (Exception Ex)
                {
                    ServiceRef.LogService.LogException(Ex);
                }
            }

            ActiveSlot.IsVisible = false;
            ActiveSlot.Content = null;

            this.isSlotAActive = !this.isSlotAActive;
            this.currentScreen = screen;
        }

        /// <inheritdoc/>
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

        /// <inheritdoc/>
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

        /// <inheritdoc/>
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

        /// <inheritdoc/>
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

        /// <inheritdoc/>
        public async Task ShowToast(View toast, ToastTransition transition = ToastTransition.SlideFromTop, ToastPlacement placement = ToastPlacement.Top)
        {
            ArgumentNullException.ThrowIfNull(toast);
            if (this.activeToast is not null)
                this.toastLayer.Children.Remove(this.activeToast);
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

        /// <inheritdoc/>
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

        /// <inheritdoc/>
        public void UpdateBars(BindableObject screen)
        {
            bool TopVisible = NavigationBars.GetTopBarVisible(screen);
            bool NavVisible = NavigationBars.GetNavBarVisible(screen);
            this.topBar.IsVisible = TopVisible;
            this.navBar.IsVisible = NavVisible;
            if (screen is IBarContentProvider Provider)
            {
                this.topBar.Content = Provider.TopBarContent;
                this.navBar.Content = Provider.NavBarContent;
            }
            else
            {
                this.topBar.Content = null;
                this.navBar.Content = null;
            }
        }

        /// <inheritdoc/>
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
                NavigationService? Nav = ServiceRef.Provider.GetService<INavigationService>() as NavigationService;
                if (Nav is not null && Nav.WouldHandleBack())
                {
                    _ = this.Dispatcher.DispatchAsync(async () =>
                    {
                        bool Handled = await Nav.HandleBackAsync();
                        if (!Handled)
                        {
#if ANDROID || WINDOWS
                            try { Microsoft.Maui.Controls.Application.Current?.Quit(); } catch { }
#endif
                        }
                    });
                    return true;
                }
            }
            catch (Exception Ex)
            {
                ServiceRef.LogService.LogException(Ex);
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
