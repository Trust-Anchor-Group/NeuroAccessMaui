using System;
using System.Threading.Tasks;
using Microsoft.Maui.Controls;
using Microsoft.Maui.Graphics;
using NeuroAccessMaui.UI.Pages;

namespace NeuroAccessMaui.UI.Popups
{
	/// <summary>
	/// Flexible popup surface capable of positioning its content in different regions of the screen.
	/// </summary>
	public class BasePopupView : ContentView, ILifeCycleView
	{
		public static readonly BindableProperty PopupContentProperty = BindableProperty.Create(
			nameof(PopupContent),
			typeof(View),
			typeof(BasePopupView),
			null,
			propertyChanged: OnPopupContentChanged);

		public static readonly BindableProperty PlacementProperty = BindableProperty.Create(
			nameof(Placement),
			typeof(PopupPlacement),
			typeof(BasePopupView),
			PopupPlacement.Center,
			propertyChanged: OnLayoutPropertyChanged);

		public static readonly BindableProperty AnchorPointProperty = BindableProperty.Create(
			nameof(AnchorPoint),
			typeof(Point?),
			typeof(BasePopupView),
			null,
			propertyChanged: OnLayoutPropertyChanged);

		public static readonly BindableProperty PopupMarginProperty = BindableProperty.Create(
			nameof(PopupMargin),
			typeof(Thickness),
			typeof(BasePopupView),
			new Thickness(0),
			propertyChanged: OnLayoutPropertyChanged);

		public static readonly BindableProperty PopupPaddingProperty = BindableProperty.Create(
			nameof(PopupPadding),
			typeof(Thickness),
			typeof(BasePopupView),
			new Thickness(0),
			propertyChanged: OnPopupPaddingChanged);

		public static readonly BindableProperty CloseOnBackgroundTapProperty = BindableProperty.Create(
			nameof(CloseOnBackgroundTap),
			typeof(bool),
			typeof(BasePopupView),
			true);

		public static readonly BindableProperty StretchContentWidthProperty = BindableProperty.Create(
			nameof(StretchContentWidth),
			typeof(bool),
			typeof(BasePopupView),
			false,
			propertyChanged: OnLayoutPropertyChanged);

		public static readonly BindableProperty StretchContentHeightProperty = BindableProperty.Create(
			nameof(StretchContentHeight),
			typeof(bool),
			typeof(BasePopupView),
			false,
			propertyChanged: OnLayoutPropertyChanged);

		private readonly AbsoluteLayout root;
		private readonly ContentView popupContainer;

		public event EventHandler? BackgroundTapped;

		public BasePopupView()
		{
			this.HorizontalOptions = LayoutOptions.Fill;
			this.VerticalOptions = LayoutOptions.Fill;

			this.popupContainer = new ContentView
			{
				Padding = this.PopupPadding
			};

			this.root = new AbsoluteLayout
			{
				HorizontalOptions = LayoutOptions.Fill,
				VerticalOptions = LayoutOptions.Fill
			};
			AbsoluteLayout.SetLayoutFlags(this.popupContainer, Microsoft.Maui.Layouts.AbsoluteLayoutFlags.None);
			this.root.Children.Add(this.popupContainer);

			TapGestureRecognizer backgroundTap = new();
			backgroundTap.Tapped += this.OnPopupBackgroundTapped;
			this.root.GestureRecognizers.Add(backgroundTap);

			base.Content = this.root;
		}

		/// <summary>
		/// Gets or sets the view rendered inside the popup container.
		/// </summary>
		public View? PopupContent
		{
			get => (View?)this.GetValue(PopupContentProperty);
			set => this.SetValue(PopupContentProperty, value);
		}

		/// <summary>
		/// Gets or sets the general placement strategy for the popup.
		/// </summary>
		public PopupPlacement Placement
		{
			get => (PopupPlacement)this.GetValue(PlacementProperty);
			set => this.SetValue(PlacementProperty, value);
		}

		/// <summary>
		/// Gets or sets the anchor point for <see cref="PopupPlacement.Anchor"/>.
		/// </summary>
		public Point? AnchorPoint
		{
			get => (Point?)this.GetValue(AnchorPointProperty);
			set => this.SetValue(AnchorPointProperty, value);
		}

		/// <summary>
		/// Gets or sets the margin reserved around the popup.
		/// </summary>
		public Thickness PopupMargin
		{
			get => (Thickness)this.GetValue(PopupMarginProperty);
			set => this.SetValue(PopupMarginProperty, value);
		}

		/// <summary>
		/// Gets or sets padding applied inside the popup container.
		/// </summary>
		public Thickness PopupPadding
		{
			get => (Thickness)this.GetValue(PopupPaddingProperty);
			set => this.SetValue(PopupPaddingProperty, value);
		}

		/// <summary>
		/// Gets or sets if taps outside popup content should raise <see cref="BackgroundTapped"/>.
		/// </summary>
		public bool CloseOnBackgroundTap
		{
			get => (bool)this.GetValue(CloseOnBackgroundTapProperty);
			set => this.SetValue(CloseOnBackgroundTapProperty, value);
		}

		/// <summary>
		/// Gets or sets if popup content should stretch to use the available width inside margins.
		/// </summary>
		public bool StretchContentWidth
		{
			get => (bool)this.GetValue(StretchContentWidthProperty);
			set => this.SetValue(StretchContentWidthProperty, value);
		}

		/// <summary>
		/// Gets or sets if popup content should stretch to use the available height inside margins.
		/// </summary>
		public bool StretchContentHeight
		{
			get => (bool)this.GetValue(StretchContentHeightProperty);
			set => this.SetValue(StretchContentHeightProperty, value);
		}

		/// <summary>
		/// Manually triggers the background tapped event.
		/// </summary>
		public void TriggerBackgroundTapped()
		{
			this.BackgroundTapped?.Invoke(this, EventArgs.Empty);
		}

		public virtual Task OnInitializeAsync()
		{
			return Task.CompletedTask;
		}

		public virtual Task OnDisposeAsync()
		{
			return Task.CompletedTask;
		}

		public virtual Task OnAppearingAsync()
		{
			return Task.CompletedTask;
		}

		public virtual Task OnDisappearingAsync()
		{
			return Task.CompletedTask;
		}

		protected override void OnSizeAllocated(double width, double height)
		{
			base.OnSizeAllocated(width, height);
			this.UpdatePopupLayout(width, height);
		}

		private static void OnPopupContentChanged(BindableObject bindable, object? oldValue, object? newValue)
		{
			if (bindable is BasePopupView popupView)
			{
				popupView.popupContainer.Content = newValue as View;
				popupView.InvalidateMeasure();
				popupView.UpdatePopupLayout(popupView.Width, popupView.Height);
			}
		}

		private static void OnLayoutPropertyChanged(BindableObject bindable, object? oldValue, object? newValue)
		{
			if (bindable is BasePopupView popupView)
				popupView.UpdatePopupLayout(popupView.Width, popupView.Height);
		}

		private static void OnPopupPaddingChanged(BindableObject bindable, object? oldValue, object? newValue)
		{
			if (bindable is BasePopupView popupView && newValue is Thickness padding)
			{
				popupView.popupContainer.Padding = padding;
				popupView.InvalidateMeasure();
				popupView.UpdatePopupLayout(popupView.Width, popupView.Height);
			}
		}

		private void OnPopupBackgroundTapped(object? sender, TappedEventArgs e)
		{
			if (!this.CloseOnBackgroundTap)
				return;

			if (this.popupContainer.Width > 0 && this.popupContainer.Height > 0)
			{
				Point? point = e.GetPosition(this.popupContainer);
				if (point is Point hit &&
					hit.X >= 0 && hit.Y >= 0 &&
					hit.X <= this.popupContainer.Width &&
					hit.Y <= this.popupContainer.Height)
				{
					return;
				}
			}

			this.BackgroundTapped?.Invoke(this, EventArgs.Empty);
		}

		private void UpdatePopupLayout(double width, double height)
		{
			if (width <= 0 || height <= 0)
				return;

			Thickness margin = this.PopupMargin;
			double availableWidth = Math.Max(0, width - margin.Left - margin.Right);
			double availableHeight = Math.Max(0, height - margin.Top - margin.Bottom);

			if (availableWidth <= 0 || availableHeight <= 0)
			{
				AbsoluteLayout.SetLayoutBounds(this.popupContainer, new Rect(margin.Left, margin.Top, Math.Max(0, availableWidth), Math.Max(0, availableHeight)));
				this.root.InvalidateMeasure();
				return;
			}

			Size request = this.popupContainer.Measure(availableWidth, availableHeight);
			double contentWidth = double.IsInfinity(request.Width) || request.Width <= 0 ? availableWidth : Math.Min(request.Width, availableWidth);
			double contentHeight = double.IsInfinity(request.Height) || request.Height <= 0 ? availableHeight : Math.Min(request.Height, availableHeight);

			if (this.Placement == PopupPlacement.Fill)
			{
				contentWidth = availableWidth;
				contentHeight = availableHeight;
			}
			else
			{
				if (this.StretchContentWidth)
					contentWidth = availableWidth;
				if (this.StretchContentHeight)
					contentHeight = availableHeight;
			}

			double x = margin.Left;
			double y = margin.Top;

			switch (this.Placement)
			{
				case PopupPlacement.Center:
					x += (availableWidth - contentWidth) / 2;
					y += (availableHeight - contentHeight) / 2;
					break;
				case PopupPlacement.Top:
					x += (availableWidth - contentWidth) / 2;
					break;
				case PopupPlacement.Bottom:
					x += (availableWidth - contentWidth) / 2;
					y = height - margin.Bottom - contentHeight;
					break;
				case PopupPlacement.Left:
					y += (availableHeight - contentHeight) / 2;
					break;
				case PopupPlacement.Right:
					x = width - margin.Right - contentWidth;
					y += (availableHeight - contentHeight) / 2;
					break;
				case PopupPlacement.Anchor:
					Point anchor = this.AnchorPoint ?? new Point(width / 2, height / 2);
					x = anchor.X - (contentWidth / 2);
					y = anchor.Y - (contentHeight / 2);
					double minX = margin.Left;
					double maxX = width - margin.Right - contentWidth;
					double minY = margin.Top;
					double maxY = height - margin.Bottom - contentHeight;
					x = Math.Min(Math.Max(x, minX), maxX);
					y = Math.Min(Math.Max(y, minY), maxY);
					break;
				case PopupPlacement.Fill:
					x = margin.Left;
					y = margin.Top;
					break;
				case PopupPlacement.TopLeft:
					x = margin.Left;
					y = margin.Top;
					break;
				case PopupPlacement.TopRight:
					x = width - margin.Right - contentWidth;
					y = margin.Top;
					break;
				case PopupPlacement.BottomLeft:
					x = margin.Left;
					y = height - margin.Bottom - contentHeight;
					break;
				case PopupPlacement.BottomRight:
					x = width - margin.Right - contentWidth;
					y = height - margin.Bottom - contentHeight;
					break;
				default:
					x += (availableWidth - contentWidth) / 2;
					y += (availableHeight - contentHeight) / 2;
					break;
			}

			AbsoluteLayout.SetLayoutBounds(this.popupContainer, new Rect(x, y, contentWidth, contentHeight));
			this.root.InvalidateMeasure();
		}

		protected override void OnPropertyChanged(string? propertyName = null)
		{
			base.OnPropertyChanged(propertyName);
			if (propertyName == nameof(this.Content) && base.Content is View content && !ReferenceEquals(content, this.root))
			{
				base.Content = this.root;
				this.PopupContent = content;
			}
		}
	}
}
