using System;
using System.Threading.Tasks;
using Microsoft.Maui.Controls;
using Microsoft.Maui.Devices;
using Microsoft.Maui.Graphics;
using NeuroAccessMaui.Services;
using NeuroAccessMaui.UI.Pages;

namespace NeuroAccessMaui.UI.Popups
{
	/// <summary>
	/// Common popup container providing centered layout and lifecycle forwarding for popup content.
	/// </summary>
	public class BasePopupView : ContentView, ILifeCycleView
	{
		public static readonly BindableProperty PopupContentProperty = BindableProperty.Create(
			nameof(PopupContent),
			typeof(View),
			typeof(BasePopupView),
			null,
			propertyChanged: OnPopupContentChanged);

		public static readonly BindableProperty AllowBackgroundDismissProperty = BindableProperty.Create(
			nameof(AllowBackgroundDismiss),
			typeof(bool),
			typeof(BasePopupView),
			true);

		public static readonly BindableProperty CardWidthFractionProperty = BindableProperty.Create(
			nameof(CardWidthFraction),
			typeof(double),
			typeof(BasePopupView),
			0.875,
			propertyChanged: OnLayoutPropertyChanged);

		public static readonly BindableProperty CardMaxHeightFractionProperty = BindableProperty.Create(
			nameof(CardMaxHeightFraction),
			typeof(double),
			typeof(BasePopupView),
			0.75,
			propertyChanged: OnLayoutPropertyChanged);

		private readonly Grid root;
		private readonly BoxView overlay;
		private readonly ContentView cardHost;

		public BasePopupView()
		{
			this.overlay = new BoxView
			{
				Color = new Color(0, 0, 0, 178),
				HorizontalOptions = LayoutOptions.Fill,
				VerticalOptions = LayoutOptions.Fill
			};

			TapGestureRecognizer backgroundTap = new();
			backgroundTap.Tapped += this.OnOverlayTapped;
			this.overlay.GestureRecognizers.Add(backgroundTap);

			this.cardHost = new ContentView
			{
				HorizontalOptions = LayoutOptions.Center,
				VerticalOptions = LayoutOptions.Center
			};

			this.root = new Grid();
			this.root.Add(this.overlay);
			this.root.Add(this.cardHost);

			base.Content = this.root;
		}

		/// <summary>
		/// View rendered within the popup card.
		/// </summary>
		public View? PopupContent
		{
			get => (View?)this.GetValue(PopupContentProperty);
			set => this.SetValue(PopupContentProperty, value);
		}

		/// <summary>
		/// Determines if tapping the dimmed background should dismiss the popup.
		/// </summary>
		public bool AllowBackgroundDismiss
		{
			get => (bool)this.GetValue(AllowBackgroundDismissProperty);
			set => this.SetValue(AllowBackgroundDismissProperty, value);
		}

		/// <summary>
		/// Fraction of viewport width used for the popup card.
		/// </summary>
		public double CardWidthFraction
		{
			get => (double)this.GetValue(CardWidthFractionProperty);
			set => this.SetValue(CardWidthFractionProperty, value);
		}

		/// <summary>
		/// Fraction of viewport height used as maximum for the popup card.
		/// </summary>
		public double CardMaxHeightFraction
		{
			get => (double)this.GetValue(CardMaxHeightFractionProperty);
			set => this.SetValue(CardMaxHeightFractionProperty, value);
		}

		public virtual Task OnInitializeAsync() => this.ForwardLifecycleAsync(static lifecycle => lifecycle.OnInitializeAsync());

		public virtual Task OnDisposeAsync() => this.ForwardLifecycleAsync(static lifecycle => lifecycle.OnDisposeAsync());

		public virtual Task OnAppearingAsync() => this.ForwardLifecycleAsync(static lifecycle => lifecycle.OnAppearingAsync());

		public virtual Task OnDisappearingAsync() => this.ForwardLifecycleAsync(static lifecycle => lifecycle.OnDisappearingAsync());

		protected virtual Task OnBackgroundDismissRequestedAsync() => ServiceRef.PopupService.PopAsync();

		protected virtual void ApplyCardSizing(View card)
		{
			DisplayInfo display = DeviceDisplay.MainDisplayInfo;
			double width = display.Width / display.Density * this.CardWidthFraction;
			double maxHeight = display.Height / display.Density * this.CardMaxHeightFraction;
			card.WidthRequest = width;
			card.MaximumHeightRequest = maxHeight;
		}

		private static void OnPopupContentChanged(BindableObject bindable, object? oldValue, object? newValue)
		{
			if (bindable is BasePopupView popupView)
			{
				popupView.cardHost.Content = newValue as View;
				if (newValue is View view)
					popupView.ApplyCardSizing(view);
			}
		}

		private Task ForwardLifecycleAsync(Func<ILifeCycleView, Task> invoker)
		{
			if (this.BindingContext is ILifeCycleView lifecycle)
				return invoker(lifecycle);
			return Task.CompletedTask;
		}

		protected override void OnPropertyChanged(string? propertyName = null)
		{
			base.OnPropertyChanged(propertyName);
			if (propertyName == nameof(Content) && base.Content is View content && !ReferenceEquals(content, this.root))
			{
				base.Content = this.root;
				this.PopupContent = content;
			}
		}

		private async void OnOverlayTapped(object? sender, EventArgs e)
		{
			if (!this.AllowBackgroundDismiss)
				return;
			await this.OnBackgroundDismissRequestedAsync();
		}

		private static void OnLayoutPropertyChanged(BindableObject bindable, object? oldValue, object? newValue)
		{
			if (bindable is BasePopupView popupView && popupView.cardHost.Content is View card)
			{
				popupView.ApplyCardSizing(card);
			}
		}
	}
}
