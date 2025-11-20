using Microsoft.Maui.Controls.PlatformConfiguration;
using Microsoft.Maui.Controls.PlatformConfiguration.iOSSpecific;
using NeuroAccessMaui.UI.Pages;
using NeuroAccessMaui.UI.Pages.Startup;             // for LoadingPage
using NeuroAccessMaui.UI;
using NeuroAccessMaui.UI.Popups;
using ControlsVisualElement = Microsoft.Maui.Controls.VisualElement;
using CommunityToolkit.Maui.Core;
using CommunityToolkit.Maui.Behaviors;
using NeuroAccessMaui.Animations;

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
        private readonly IAnimationCoordinator animationCoordinator;
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
        public CustomShell(LoadingPage loadingPage, IKeyboardInsetsService keyboardInsetsService, IAnimationCoordinator animationCoordinator)
        {
            ArgumentNullException.ThrowIfNull(keyboardInsetsService);
            ArgumentNullException.ThrowIfNull(animationCoordinator);
            this.keyboardInsetsService = keyboardInsetsService;
            this.animationCoordinator = animationCoordinator;
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

			this.popupBackground = new BoxView { Opacity = 0, Color = Colors.Black };
			this.popupOverlay = new Grid { IsVisible = false, InputTransparent = false };
			this.popupOverlay.Add(this.popupBackground);
			this.popupHost = new Grid { VerticalOptions = LayoutOptions.Fill, HorizontalOptions = LayoutOptions.Fill };
			this.popupOverlay.Add(this.popupHost);
            this.popupOverlay.IgnoreSafeArea = true;

			TapGestureRecognizer popupBackgroundTap = new();
            popupBackgroundTap.Tapped += this.OnPopupBackgroundTapped;
            this.popupBackground.GestureRecognizers.Add(popupBackgroundTap);

            this.toastLayer = new Grid { IsVisible = false, InputTransparent = false, Padding = this.toastBasePadding, VerticalOptions = LayoutOptions.Fill, HorizontalOptions = LayoutOptions.Fill };
            this.toastLayer.RowDefinitions.Add(new RowDefinition { Height = GridLength.Auto });
            this.toastLayer.RowDefinitions.Add(new RowDefinition { Height = new GridLength(1, GridUnitType.Star) });
            this.toastLayer.RowDefinitions.Add(new RowDefinition { Height = GridLength.Auto });
            this.toastLayer.IgnoreSafeArea = true;


            this.layout.Add(this.topBar, 0, 0);
            this.layout.Add(this.contentHostA, 0, 1);
            this.layout.Add(this.contentHostB, 0, 1);
            this.layout.Add(this.navBar, 0, 2);
            this.layout.Add(this.popupOverlay);
            Grid.SetRowSpan(this.popupOverlay, 3);
            this.layout.Add(this.toastLayer);
            Grid.SetRowSpan(this.toastLayer, 3);

            this.layout.IgnoreSafeArea = true;
            this.On<iOS>().SetUseSafeArea(false);
            this.HideSoftInputOnTapped = true;

            this.Content = this.layout;
            this.Padding = 0;
            this.SetDynamicResource(BackgroundColorProperty, "SurfaceBackgroundWL");
            
            LoadingPage loadingPageInstance = ServiceHelper.GetService<LoadingPage>();
            this.contentHostA.Content = loadingPageInstance;
            this.contentHostB.Content = null;
            this.currentScreen = loadingPageInstance;
	            Thickness initialSafeArea = this.ApplyInsetsToHost(this.contentHostA, loadingPageInstance, false, false);
	            this.UpdateOverlayInsets(initialSafeArea, false);

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

			if (Microsoft.Maui.Controls.Application.Current is not null)
			{
				Microsoft.Maui.Controls.Application.Current.RequestedThemeChanged += (s, e) =>
				{
					StatusBarStyle StatusStyle = CurrentTheme == AppTheme.Dark ? StatusBarStyle.LightContent : StatusBarStyle.DarkContent;
					StatusBehaviour.StatusBarStyle = StatusStyle;
				};
			}
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

            AnimationContextOptions ContextOptions = this.CreateAnimationContextOptions();
            try
            {
                if (Transition == TransitionType.Fade)
                {
                    await this.animationCoordinator.PlayTransitionAsync(AnimationKeys.Shell.PageCrossFade, InactiveSlot, ActiveSlot, null, ContextOptions);
                }
                else if (Transition == TransitionType.SwipeLeft)
                {
                    await this.animationCoordinator.PlayTransitionAsync(AnimationKeys.Shell.PageSlideLeft, InactiveSlot, ActiveSlot, null, ContextOptions);
                }
                else if (Transition == TransitionType.SwipeRight)
                {
                    await this.animationCoordinator.PlayTransitionAsync(AnimationKeys.Shell.PageSlideRight, InactiveSlot, ActiveSlot, null, ContextOptions);
                }
                else
                {
                    InactiveSlot.Opacity = 1;
                    ActiveSlot.Opacity = 1;
                }
            }
            finally
            {
                InactiveSlot.TranslationX = 0;
                ActiveSlot.TranslationX = 0;
                InactiveSlot.Opacity = 1;
                ActiveSlot.Opacity = 1;
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
	            this.UpdateOverlayInsets(SafeAreaThickness, false);
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

			if (transition == PopupTransition.None)
			{
				popup.Opacity = 1;
				popup.Scale = 1;
				popup.TranslationX = 0;
				popup.TranslationY = 0;
			}
			else
			{
				AnimationKey? ShowKey = transition switch
				{
					PopupTransition.Fade => AnimationKeys.Shell.PopupShowFade,
					PopupTransition.Scale => AnimationKeys.Shell.PopupShowScale,
					PopupTransition.SlideUp => AnimationKeys.Shell.PopupShowSlideUp,
					_ => null
				};

				if (ShowKey.HasValue)
					await this.animationCoordinator.PlayAsync(ShowKey.Value, popup, null, this.CreateAnimationContextOptions());
				else
					popup.Opacity = 1;
			}

			popup.TranslationX = 0;
			popup.TranslationY = 0;
			popup.Scale = 1;
			popup.Opacity = 1;

			this.currentPopupState = visualState;
		}

		public async Task HidePopup(ContentView popup, PopupTransition transition, PopupVisualState? nextVisualState)
		{
			ArgumentNullException.ThrowIfNull(popup);

			if (transition == PopupTransition.None)
			{
				popup.Opacity = 0;
			}
			else
			{
				AnimationKey? HideKey = transition switch
				{
					PopupTransition.Fade => AnimationKeys.Shell.PopupHideFade,
					PopupTransition.Scale => AnimationKeys.Shell.PopupHideScale,
					PopupTransition.SlideUp => AnimationKeys.Shell.PopupHideSlideUp,
					_ => null
				};

				if (HideKey.HasValue)
					await this.animationCoordinator.PlayAsync(HideKey.Value, popup, null, this.CreateAnimationContextOptions());
				else
					popup.Opacity = 0;
			}

			popup.TranslationX = 0;
			popup.TranslationY = 0;
			popup.Scale = 1;
			popup.Opacity = 0;
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

			AnimationKey? ShowKey = Transition switch
			{
				ToastTransition.Fade => AnimationKeys.Shell.ToastShowFade,
				ToastTransition.SlideFromBottom => AnimationKeys.Shell.ToastShowSlideBottom,
				ToastTransition.SlideFromTop when Placement == ToastPlacement.Bottom => AnimationKeys.Shell.ToastShowSlideBottom,
				ToastTransition.SlideFromTop => AnimationKeys.Shell.ToastShowSlideTop,
				_ => null
			};

			if (ShowKey.HasValue)
			{
				await this.animationCoordinator.PlayAsync(ShowKey.Value, Toast, null, this.CreateAnimationContextOptions());
			}
			else
			{
				Toast.Opacity = 1;
				Toast.TranslationX = 0;
				Toast.TranslationY = 0;
			}

			Toast.TranslationX = 0;
			Toast.TranslationY = 0;
			Toast.Opacity = 1;
		}

		public async Task HideToast(ToastTransition Transition = ToastTransition.SlideFromTop)
		{
			if (this.activeToast is null)
				return;
			View Toast = this.activeToast;

			AnimationKey? HideKey = Transition switch
			{
				ToastTransition.Fade => AnimationKeys.Shell.ToastHideFade,
				ToastTransition.SlideFromBottom => AnimationKeys.Shell.ToastHideSlideBottom,
				ToastTransition.SlideFromTop when this.activeToastPlacement == ToastPlacement.Bottom => AnimationKeys.Shell.ToastHideSlideBottom,
				ToastTransition.SlideFromTop => AnimationKeys.Shell.ToastHideSlideTop,
				_ => null
			};

			if (HideKey.HasValue)
			{
				await this.animationCoordinator.PlayAsync(HideKey.Value, Toast, null, this.CreateAnimationContextOptions());
			}
			else
			{
				Toast.Opacity = 0;
			}

			this.toastLayer.Children.Remove(Toast);
			this.activeToast = null;
			this.activeToastPlacement = ToastPlacement.Top;
			if (this.toastLayer.Children.Count == 0)
				this.toastLayer.IsVisible = false;
		}

		private AnimationContextOptions CreateAnimationContextOptions()
		{
			double? ViewportWidth = this.Width > 0 ? this.Width : null;
			double? ViewportHeight = this.Height > 0 ? this.Height : null;
			return new AnimationContextOptions
			{
				KeyboardInset = this.currentKeyboardInset,
				ViewportWidth = ViewportWidth,
				ViewportHeight = ViewportHeight
			};
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
	            this.UpdateOverlayInsets(SafeArea, false);
        }

        private Thickness ApplyInsetsToHost(ContentView host, BindableObject screen, bool animate, bool notify)
        {
            ArgumentNullException.ThrowIfNull(screen);
            ArgumentNullException.ThrowIfNull(host);

            Thickness SafeAreaThickness = SafeArea.ResolveInsetsFor(screen);
            KeyboardInsetMode Mode = KeyboardInsets.GetMode(screen);
            double KeyboardInsetForPadding = Mode == KeyboardInsetMode.Automatic ? this.ResolveEffectiveKeyboardInset(SafeAreaThickness) : 0;
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

	        private void UpdateOverlayInsets(Thickness safeArea, bool animate)
        {
            double EffectiveKeyboardInset = this.ResolveEffectiveKeyboardInset(safeArea);
            Thickness OverlayPadding = new Thickness(safeArea.Left, safeArea.Top, safeArea.Right, safeArea.Bottom + EffectiveKeyboardInset);
            this.SetViewPadding(this.popupOverlay, OverlayPadding, animate);

            Thickness ToastPadding = new Thickness(
                this.toastBasePadding.Left + safeArea.Left,
                this.toastBasePadding.Top + safeArea.Top,
                this.toastBasePadding.Right + safeArea.Right,
                this.toastBasePadding.Bottom + safeArea.Bottom + EffectiveKeyboardInset);
            this.SetViewPadding(this.toastLayer, ToastPadding, animate);

            Thickness TopBarPadding = new Thickness(safeArea.Left, safeArea.Top, safeArea.Right, 0);
            this.SetViewPadding(this.topBar, TopBarPadding, animate);

            Thickness NavBarPadding = new Thickness(safeArea.Left, 0, safeArea.Right, safeArea.Bottom);
            this.SetViewPadding(this.navBar, NavBarPadding, animate);
        }

        private double ResolveEffectiveKeyboardInset(Thickness safeArea)
        {
            double EffectiveInset = this.currentKeyboardInset - safeArea.Bottom;
            if (EffectiveInset < 0)
                return 0;
            return EffectiveInset;
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
	                this.UpdateOverlayInsets(safeArea, true);
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
	                this.UpdateOverlayInsets(SafeAreaThickness, false);
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
