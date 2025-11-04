using Microsoft.Maui.Controls.PlatformConfiguration;
using Microsoft.Maui.Controls.PlatformConfiguration.iOSSpecific;
using NeuroAccessMaui.UI.Pages;
using NeuroAccessMaui.UI.Pages.Startup;             // for LoadingPage
using NeuroAccessMaui.UI;
using NeuroAccessMaui.UI.Popups;
using ControlsVisualElement = Microsoft.Maui.Controls.VisualElement;
using CommunityToolkit.Maui.Core;
using CommunityToolkit.Maui.Behaviors;

namespace NeuroAccessMaui.Services.UI
{
    /// <summary>
    /// Custom shell hosting app content, popups and Toast layers.
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

#if !WINDOWS
			AppTheme CurrentTheme = Microsoft.Maui.Controls.Application.Current?.RequestedTheme ?? AppTheme.Light;
			StatusBarStyle StatusStyle = CurrentTheme == AppTheme.Dark ? StatusBarStyle.LightContent : StatusBarStyle.DarkContent;
			StatusBarBehavior StatusBehaviour = new() { StatusBarColor = Colors.Transparent, StatusBarStyle = StatusStyle };
			this.Behaviors.Add(StatusBehaviour);

			Microsoft.Maui.Controls.Application.Current?.RequestedThemeChanged += (s, e) =>
			{
				StatusBarStyle StatusStyle = CurrentTheme == AppTheme.Dark ? StatusBarStyle.LightContent : StatusBarStyle.DarkContent;
				StatusBehaviour.StatusBarStyle = StatusStyle;
			};
#endif
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

        public async Task ShowScreen(ContentView Screen, TransitionType Transition = TransitionType.Fade)
        {
            ContentView ActiveSlot = this.isSlotAActive ? this.contentHostA : this.contentHostB;
            ContentView InactiveSlot = this.isSlotAActive ? this.contentHostB : this.contentHostA;
            BaseContentPage? OutgoingPage = ActiveSlot.Content as BaseContentPage;

            InactiveSlot.Content = Screen;
            InactiveSlot.BindingContext = Screen.BindingContext;
            InactiveSlot.IsVisible = true;
            _ = this.ApplyInsetsToHost(InactiveSlot, Screen, false, false);
            this.UpdateBars(Screen);

            if (Transition == TransitionType.Fade)
            {
                InactiveSlot.Opacity = 0;
                ActiveSlot.Opacity = 1;
                const uint Duration = 300;
                Task<bool> FadeIn = InactiveSlot.FadeTo(1, Duration, Easing.Linear);
                Task<bool> FadeOut = ActiveSlot.FadeTo(0, Duration, Easing.Linear);
                await Task.WhenAll(FadeIn, FadeOut);
            }
            else if (Transition == TransitionType.SwipeLeft || Transition == TransitionType.SwipeRight)
            {
                double Width = this.Width > 0 ? this.Width : 400;
                double FromX = (Transition == TransitionType.SwipeLeft) ? Width : -Width;
                double OldToX = (Transition == TransitionType.SwipeLeft) ? -Width : Width;
                InactiveSlot.TranslationX = FromX;
                ActiveSlot.TranslationX = 0;
                Task<bool> NewIn = InactiveSlot.TranslateTo(0, 0, 250, Easing.CubicOut);
                Task<bool> OldOut = ActiveSlot.TranslateTo(OldToX, 0, 250, Easing.CubicOut);
                await Task.WhenAll(NewIn, OldOut);
                InactiveSlot.TranslationX = 0;
                ActiveSlot.TranslationX = 0;
            }

            if (OutgoingPage is not null)
            {
                try { await OutgoingPage.OnDisappearingAsync(); }
                catch (Exception ex) { ServiceRef.LogService.LogException(ex); }
            }

            ActiveSlot.IsVisible = false;
            ActiveSlot.Content = null;
            this.isSlotAActive = !this.isSlotAActive;
            this.currentScreen = Screen;
            ContentView NewlyActiveSlot = this.isSlotAActive ? this.contentHostA : this.contentHostB;
            Thickness SafeAreaThickness = this.ApplyInsetsToHost(NewlyActiveSlot, Screen, false, true);
            this.UpdateOverlayInsets(SafeAreaThickness, this.currentKeyboardInset, false);
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

        public async Task ShowToast(View Toast, ToastTransition Transition = ToastTransition.SlideFromTop, ToastPlacement Placement = ToastPlacement.Top)
        {
            ArgumentNullException.ThrowIfNull(Toast);
            if (this.activeToast is not null)
                this.toastLayer.Children.Remove(this.activeToast);
            this.activeToast = Toast;
            this.activeToastPlacement = Placement;
            int TargetRow = Placement == ToastPlacement.Top ? 0 : 2;
            Grid.SetRow(Toast, TargetRow);
            if (!this.toastLayer.Children.Contains(Toast))
                this.toastLayer.Children.Add(Toast);
            if (Toast is BindableObject toastBindable)
                this.NotifyKeyboardAwareTargets(toastBindable);
            this.toastLayer.IsVisible = true;
            Toast.Opacity = 1;
            Toast.TranslationY = 0;
            if (Transition == ToastTransition.Fade)
            {
                Toast.Opacity = 0;
                await Toast.FadeTo(1, 150, Easing.CubicOut);
            }
            else if (Transition == ToastTransition.SlideFromBottom || (Transition == ToastTransition.SlideFromTop && Placement == ToastPlacement.Bottom))
            {
                double Offset = this.Height > 0 ? this.Height : 400;
                Toast.TranslationY = Offset * 0.1;
                Toast.Opacity = 0;
                Task<bool> FadeTask = Toast.FadeTo(1, 150, Easing.CubicOut);
                Task<bool> TranslateTask = Toast.TranslateTo(0, 0, 150, Easing.CubicOut);
                await Task.WhenAll(FadeTask, TranslateTask);
            }
            else if (Transition == ToastTransition.SlideFromTop)
            {
                double Offset = this.Height > 0 ? this.Height : 400;
                Toast.TranslationY = -Offset * 0.1;
                Toast.Opacity = 0;
                Task<bool> FadeTask = Toast.FadeTo(1, 150, Easing.CubicOut);
                Task<bool> TranslateTask = Toast.TranslateTo(0, 0, 150, Easing.CubicOut);
                await Task.WhenAll(FadeTask, TranslateTask);
            }
        }

        public async Task HideToast(ToastTransition Transition = ToastTransition.SlideFromTop)
        {
            if (this.activeToast is null)
                return;
            View Toast = this.activeToast;
            if (Transition == ToastTransition.Fade)
            {
                await Toast.FadeTo(0, 120, Easing.CubicIn);
            }
            else if (Transition == ToastTransition.SlideFromBottom || (Transition == ToastTransition.SlideFromTop && this.activeToastPlacement == ToastPlacement.Bottom))
            {
                Task<bool> FadeTask = Toast.FadeTo(0, 120, Easing.CubicIn);
                Task<bool> TranslateTask = Toast.TranslateTo(0, 40, 120, Easing.CubicIn);
                await Task.WhenAll(FadeTask, TranslateTask);
            }
            else if (Transition == ToastTransition.SlideFromTop)
            {
                Task<bool> FadeTask = Toast.FadeTo(0, 120, Easing.CubicIn);
                Task<bool> TranslateTask = Toast.TranslateTo(0, -40, 120, Easing.CubicIn);
                await Task.WhenAll(FadeTask, TranslateTask);
            }
            this.toastLayer.Children.Remove(Toast);
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
                Task<bool> FadeTask = popup.FadeTo(1, 200, Easing.CubicOut);
                Task<bool> ScaleTask = popup.ScaleTo(1, 200, Easing.CubicOut);
                await Task.WhenAll(FadeTask, ScaleTask);
                return;
            }
            if (transition == PopupTransition.SlideUp)
            {
                double FromY = this.Height > 0 ? this.Height : 400;
                popup.TranslationY = FromY * 0.5;
                popup.Opacity = 0;
                Task<bool> FadeTask = popup.FadeTo(1, 200, Easing.CubicOut);
                Task<bool> TranslateTask = popup.TranslateTo(0, 0, 200, Easing.CubicOut);
                await Task.WhenAll(FadeTask, TranslateTask);
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
                Task<bool> FadeTask = popup.FadeTo(0, 150, Easing.CubicIn);
                Task<bool> ScaleTask = popup.ScaleTo(0.9, 150, Easing.CubicIn);
                await Task.WhenAll(FadeTask, ScaleTask);
                return;
            }
            if (transition == PopupTransition.SlideUp)
            {
                Task<bool> FadeTask = popup.FadeTo(0, 150, Easing.CubicIn);
                Task<bool> TranslateTask = popup.TranslateTo(0, 50, 150, Easing.CubicIn);
                await Task.WhenAll(FadeTask, TranslateTask);
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

            ContentView ActiveSlot = this.isSlotAActive ? this.contentHostA : this.contentHostB;
            Thickness SafeArea = this.ApplyInsetsToHost(ActiveSlot, this.currentScreen, false, true);
            this.UpdateOverlayInsets(SafeArea, this.currentKeyboardInset, false);
        }

        private Thickness ApplyInsetsToHost(ContentView host, BindableObject screen, bool animate, bool notify)
        {
            ArgumentNullException.ThrowIfNull(screen);
            ArgumentNullException.ThrowIfNull(host);

            Thickness SafeAreaThickness = SafeArea.ResolveInsetsFor(screen);
            KeyboardInsetMode Mode = KeyboardInsets.GetMode(screen);
            double KeyboardInsetForPadding = Mode == KeyboardInsetMode.Automatic ? this.currentKeyboardInset : 0;
            Thickness TargetPadding = new Thickness(
                SafeAreaThickness.Left,
                SafeAreaThickness.Top,
                SafeAreaThickness.Right,
                SafeAreaThickness.Bottom + KeyboardInsetForPadding);

            this.SetViewPadding(host, TargetPadding, animate);

            if (notify)
                this.NotifyKeyboardAwareTargets(screen);

            return SafeAreaThickness;
        }

        private void UpdateOverlayInsets(Thickness safeArea, double keyboardInset, bool animate)
        {
            Thickness OverlayPadding = new Thickness(safeArea.Left, safeArea.Top, safeArea.Right, safeArea.Bottom + keyboardInset);
            this.SetViewPadding(this.popupOverlay, OverlayPadding, animate);

            Thickness ToastPadding = new Thickness(
                this.toastBasePadding.Left + safeArea.Left,
                this.toastBasePadding.Top + safeArea.Top,
                this.toastBasePadding.Right + safeArea.Right,
                this.toastBasePadding.Bottom + safeArea.Bottom + keyboardInset);
            this.SetViewPadding(this.toastLayer, ToastPadding, animate);

            Thickness TopBarPadding = new Thickness(safeArea.Left, safeArea.Top, safeArea.Right, 0);
            this.SetViewPadding(this.topBar, TopBarPadding, animate);

            Thickness NavBarPadding = new Thickness(safeArea.Left, 0, safeArea.Right, safeArea.Bottom);
            this.SetViewPadding(this.navBar, NavBarPadding, animate);
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

            const uint Duration = 180;
            Microsoft.Maui.Controls.ViewExtensions.CancelAnimations(view);
            view.Animate(
                "KeyboardPadding",
                progress => this.AssignPadding(view, this.InterpolateThickness(currentPadding, targetPadding, progress)),
                rate: 16,
                length: Duration,
                easing: Easing.CubicInOut,
                finished: (v, cancelled) => this.AssignPadding(view, targetPadding));
        }

        private bool TryGetPadding(ControlsVisualElement Element, out Thickness Padding)
        {
            switch (Element)
            {
                case Layout LayoutElement:
                    Padding = LayoutElement.Padding;
                    return true;
                case ContentView ContentViewElement:
                    Padding = ContentViewElement.Padding;
                    return true;
                case TemplatedView TemplatedViewElement:
                    Padding = TemplatedViewElement.Padding;
                    return true;
                case Border BorderElement:
                    Padding = BorderElement.Padding;
                    return true;
                default:
                    Padding = new Thickness(0);
                    return false;
            }
        }

        private void AssignPadding(ControlsVisualElement Element, Thickness Padding)
        {
            switch (Element)
            {
                case Layout LayoutElement:
                    LayoutElement.Padding = Padding;
                    break;
                case ContentView ContentViewElement:
                    ContentViewElement.Padding = Padding;
                    break;
                case TemplatedView TemplatedViewElement:
                    TemplatedViewElement.Padding = Padding;
                    break;
                case Border BorderElement:
                    BorderElement.Padding = Padding;
                    break;
            }
        }

        private Thickness InterpolateThickness(Thickness From, Thickness To, double Progress)
        {
            return new Thickness(
                this.InterpolateDouble(From.Left, To.Left, Progress),
                this.InterpolateDouble(From.Top, To.Top, Progress),
                this.InterpolateDouble(From.Right, To.Right, Progress),
                this.InterpolateDouble(From.Bottom, To.Bottom, Progress));
        }

        private double InterpolateDouble(double From, double To, double Progress)
        {
            return From + ((To - From) * Progress);
        }

        private bool AreThicknessClose(Thickness a, Thickness b)
        {
            return Math.Abs(a.Left - b.Left) < 0.5 &&
                   Math.Abs(a.Top - b.Top) < 0.5 &&
                   Math.Abs(a.Right - b.Right) < 0.5 &&
                   Math.Abs(a.Bottom - b.Bottom) < 0.5;
        }

        private void NotifyKeyboardAwareTargets(BindableObject Screen)
        {
            KeyboardInsetChangedEventArgs Args = new KeyboardInsetChangedEventArgs(this.currentKeyboardInset, this.isKeyboardVisible);

            if (Screen is IKeyboardInsetAware awareView)
                awareView.OnKeyboardInsetChanged(Args);

            if (Screen.BindingContext is IKeyboardInsetAware awareContext)
                awareContext.OnKeyboardInsetChanged(Args);
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

            foreach (Microsoft.Maui.IView Child in this.popupHost.Children)
            {
                if (Child is BindableObject bindableChild)
                    this.NotifyKeyboardAwareTargets(bindableChild);
            }

            if (this.activeToast is BindableObject toastBindable)
                this.NotifyKeyboardAwareTargets(toastBindable);
        }

        public void UpdateBars(BindableObject Screen)
        {
            bool TopVisible = NavigationBars.GetTopBarVisible(Screen);
            bool NavVisible = NavigationBars.GetNavBarVisible(Screen);
            this.topBar.IsVisible = TopVisible;
            this.navBar.IsVisible = NavVisible;
            if (Screen is IBarContentProvider Provider)
            {
                this.topBar.Content = Provider.TopBarContent;
                this.navBar.Content = Provider.NavBarContent;
            }
            else
            {
                this.topBar.Content = null;
                this.navBar.Content = null;
            }

            if (ReferenceEquals(Screen, this.currentScreen))
            {
                ContentView ActiveSlot = this.isSlotAActive ? this.contentHostA : this.contentHostB;
                Thickness SafeAreaThickness = this.ApplyInsetsToHost(ActiveSlot, Screen, false, true);
                this.UpdateOverlayInsets(SafeAreaThickness, this.currentKeyboardInset, false);
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
    /// Helper class for attached properties To show/hide bars.
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
