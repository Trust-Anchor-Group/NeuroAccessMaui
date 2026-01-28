using System;
using System.Threading.Tasks;
using Microsoft.Maui.Controls;
using Microsoft.Maui.Graphics;
using NeuroAccessMaui.UI.Pages;

namespace NeuroAccessMaui.UI.Popups
{
	/// <summary>
	/// Base popup surface that fills the screen and hosts popup content.
	/// </summary>
	[ContentProperty(nameof(PopupContent))]
	public class BasePopup : ContentView, ILifeCycleView, IPopupLayoutTarget
	{
		/// <summary>
		/// Identifies the <see cref="PopupContent"/> bindable property.
		/// </summary>
		public static readonly BindableProperty PopupContentProperty = BindableProperty.Create(
			nameof(PopupContent),
			typeof(View),
			typeof(BasePopup),
			null,
			propertyChanged: OnPopupContentChanged);

		/// <summary>
		/// Identifies the <see cref="Placement"/> bindable property.
		/// </summary>
		public static readonly BindableProperty PlacementProperty = BindableProperty.Create(
			nameof(Placement),
			typeof(PopupPlacement),
			typeof(BasePopup),
			PopupPlacement.Fill,
			propertyChanged: OnLayoutPropertyChanged);

		/// <summary>
		/// Identifies the <see cref="AnchorPoint"/> bindable property.
		/// </summary>
		public static readonly BindableProperty AnchorPointProperty = BindableProperty.Create(
			nameof(AnchorPoint),
			typeof(Point?),
			typeof(BasePopup),
			null,
			propertyChanged: OnLayoutPropertyChanged);

		/// <summary>
		/// Identifies the <see cref="PopupMargin"/> bindable property.
		/// </summary>
		public static readonly BindableProperty PopupMarginProperty = BindableProperty.Create(
			nameof(PopupMargin),
			typeof(Thickness),
			typeof(BasePopup),
			new Thickness(0),
			propertyChanged: OnLayoutPropertyChanged);

		/// <summary>
		/// Identifies the <see cref="PopupPadding"/> bindable property.
		/// </summary>
		public static readonly BindableProperty PopupPaddingProperty = BindableProperty.Create(
			nameof(PopupPadding),
			typeof(Thickness),
			typeof(BasePopup),
			new Thickness(0),
			propertyChanged: OnPopupPaddingChanged);

		/// <summary>
		/// Identifies the <see cref="CloseOnBackgroundTap"/> bindable property.
		/// </summary>
		public static readonly BindableProperty CloseOnBackgroundTapProperty = BindableProperty.Create(
			nameof(CloseOnBackgroundTap),
			typeof(bool),
			typeof(BasePopup),
			true);

		/// <summary>
		/// Identifies the <see cref="StretchContentWidth"/> bindable property.
		/// </summary>
		public static readonly BindableProperty StretchContentWidthProperty = BindableProperty.Create(
			nameof(StretchContentWidth),
			typeof(bool),
			typeof(BasePopup),
			false,
			propertyChanged: OnLayoutPropertyChanged);

		/// <summary>
		/// Identifies the <see cref="StretchContentHeight"/> bindable property.
		/// </summary>
		public static readonly BindableProperty StretchContentHeightProperty = BindableProperty.Create(
			nameof(StretchContentHeight),
			typeof(bool),
			typeof(BasePopup),
			false,
			propertyChanged: OnLayoutPropertyChanged);

		private readonly AbsoluteLayout root;
		private readonly ContentView popupContainer;
		private bool allowLayoutOverrides;

		/// <summary>
		/// Raised when a tap occurs outside the popup content.
		/// </summary>
		public event EventHandler? BackgroundTapped;

		/// <summary>
		/// Initializes a new instance of the <see cref="BasePopup"/> class.
		/// </summary>
		public BasePopup()
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

			TapGestureRecognizer BackgroundTap = new();
			BackgroundTap.Tapped += this.OnPopupBackgroundTapped;
			this.root.GestureRecognizers.Add(BackgroundTap);

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
		/// Gets or sets whether layout-related popup options should be applied.
		/// </summary>
		public bool AllowLayoutOverrides
		{
			get => this.allowLayoutOverrides;
			set => this.allowLayoutOverrides = value;
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

		/// <summary>
		/// Called when the popup is first initialized.
		/// </summary>
		public virtual Task OnInitializeAsync()
		{
			return Task.CompletedTask;
		}

		/// <summary>
		/// Called when the popup is being disposed.
		/// </summary>
		public virtual Task OnDisposeAsync()
		{
			return Task.CompletedTask;
		}

		/// <summary>
		/// Called when the popup is appearing.
		/// </summary>
		public virtual Task OnAppearingAsync()
		{
			return Task.CompletedTask;
		}

		/// <summary>
		/// Called when the popup is disappearing.
		/// </summary>
		public virtual Task OnDisappearingAsync()
		{
			return Task.CompletedTask;
		}

		/// <inheritdoc/>
		protected override void OnSizeAllocated(double Width, double Height)
		{
			base.OnSizeAllocated(Width, Height);
			this.UpdatePopupLayout(Width, Height);
		}

		private static void OnPopupContentChanged(BindableObject Bindable, object? OldValue, object? NewValue)
		{
			if (Bindable is BasePopup PopupView)
			{
				PopupView.popupContainer.Content = NewValue as View;
				PopupView.InvalidateMeasure();
				PopupView.UpdatePopupLayout(PopupView.Width, PopupView.Height);
			}
		}

		private static void OnLayoutPropertyChanged(BindableObject Bindable, object? OldValue, object? NewValue)
		{
			if (Bindable is BasePopup PopupView)
				PopupView.UpdatePopupLayout(PopupView.Width, PopupView.Height);
		}

		private static void OnPopupPaddingChanged(BindableObject Bindable, object? OldValue, object? NewValue)
		{
			if (Bindable is BasePopup PopupView && NewValue is Thickness Padding)
			{
				PopupView.popupContainer.Padding = Padding;
				PopupView.InvalidateMeasure();
				PopupView.UpdatePopupLayout(PopupView.Width, PopupView.Height);
			}
		}

		private void OnPopupBackgroundTapped(object? Sender, TappedEventArgs Args)
		{
			if (!this.CloseOnBackgroundTap)
				return;

			if (this.popupContainer.Width > 0 && this.popupContainer.Height > 0)
			{
				Point? TapPoint = Args.GetPosition(this.popupContainer);
				if (TapPoint is Point Hit &&
					Hit.X >= 0 && Hit.Y >= 0 &&
					Hit.X <= this.popupContainer.Width &&
					Hit.Y <= this.popupContainer.Height)
				{
					return;
				}
			}

			this.BackgroundTapped?.Invoke(this, EventArgs.Empty);
		}

		private void UpdatePopupLayout(double Width, double Height)
		{
			if (Width <= 0 || Height <= 0)
				return;

			Thickness Margin = this.PopupMargin;
			double AvailableWidth = Math.Max(0, Width - Margin.Left - Margin.Right);
			double AvailableHeight = Math.Max(0, Height - Margin.Top - Margin.Bottom);

			if (AvailableWidth <= 0 || AvailableHeight <= 0)
			{
				AbsoluteLayout.SetLayoutBounds(this.popupContainer, new Rect(Margin.Left, Margin.Top, Math.Max(0, AvailableWidth), Math.Max(0, AvailableHeight)));
				this.root.InvalidateMeasure();
				return;
			}

			Size Request = this.popupContainer.Measure(AvailableWidth, AvailableHeight);
			double ContentWidth = double.IsInfinity(Request.Width) || Request.Width <= 0 ? AvailableWidth : Math.Min(Request.Width, AvailableWidth);
			double ContentHeight = double.IsInfinity(Request.Height) || Request.Height <= 0 ? AvailableHeight : Math.Min(Request.Height, AvailableHeight);

			if (this.Placement == PopupPlacement.Fill)
			{
				ContentWidth = AvailableWidth;
				ContentHeight = AvailableHeight;
			}
			else
			{
				if (this.StretchContentWidth)
					ContentWidth = AvailableWidth;
				if (this.StretchContentHeight)
					ContentHeight = AvailableHeight;
			}

			double X = Margin.Left;
			double Y = Margin.Top;

			switch (this.Placement)
			{
				case PopupPlacement.Center:
					X += (AvailableWidth - ContentWidth) / 2;
					Y += (AvailableHeight - ContentHeight) / 2;
					break;
				case PopupPlacement.Top:
					X += (AvailableWidth - ContentWidth) / 2;
					break;
				case PopupPlacement.Bottom:
					X += (AvailableWidth - ContentWidth) / 2;
					Y = Height - Margin.Bottom - ContentHeight;
					break;
				case PopupPlacement.Left:
					Y += (AvailableHeight - ContentHeight) / 2;
					break;
				case PopupPlacement.Right:
					X = Width - Margin.Right - ContentWidth;
					Y += (AvailableHeight - ContentHeight) / 2;
					break;
				case PopupPlacement.Anchor:
					Point Anchor = this.AnchorPoint ?? new Point(Width / 2, Height / 2);
					X = Anchor.X - (ContentWidth / 2);
					Y = Anchor.Y - (ContentHeight / 2);
					double MinX = Margin.Left;
					double MaxX = Width - Margin.Right - ContentWidth;
					double MinY = Margin.Top;
					double MaxY = Height - Margin.Bottom - ContentHeight;
					X = Math.Min(Math.Max(X, MinX), MaxX);
					Y = Math.Min(Math.Max(Y, MinY), MaxY);
					break;
				case PopupPlacement.Fill:
					X = Margin.Left;
					Y = Margin.Top;
					break;
				case PopupPlacement.TopLeft:
					X = Margin.Left;
					Y = Margin.Top;
					break;
				case PopupPlacement.TopRight:
					X = Width - Margin.Right - ContentWidth;
					Y = Margin.Top;
					break;
				case PopupPlacement.BottomLeft:
					X = Margin.Left;
					Y = Height - Margin.Bottom - ContentHeight;
					break;
				case PopupPlacement.BottomRight:
					X = Width - Margin.Right - ContentWidth;
					Y = Height - Margin.Bottom - ContentHeight;
					break;
				default:
					X += (AvailableWidth - ContentWidth) / 2;
					Y += (AvailableHeight - ContentHeight) / 2;
					break;
			}

			AbsoluteLayout.SetLayoutBounds(this.popupContainer, new Rect(X, Y, ContentWidth, ContentHeight));
			this.root.InvalidateMeasure();
		}

		/// <inheritdoc/>
		protected override void OnPropertyChanged(string? PropertyName = null)
		{
			base.OnPropertyChanged(PropertyName);
			if (PropertyName == nameof(this.Content) && base.Content is View Content && !ReferenceEquals(Content, this.root))
			{
				base.Content = this.root;
				this.PopupContent = Content;
			}
		}
	}
}
