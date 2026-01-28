using Microsoft.Maui.Controls;

namespace NeuroAccessMaui.UI.Popups
{
	/// <summary>
	/// Popup with a centered card layout and size constraints.
	/// </summary>
	[ContentProperty(nameof(CardContent))]
	public class SimplePopup : BasePopup
	{
		/// <summary>
		/// Identifies the <see cref="CardContent"/> bindable property.
		/// </summary>
		public static readonly BindableProperty CardContentProperty = BindableProperty.Create(
			nameof(CardContent),
			typeof(View),
			typeof(SimplePopup),
			null,
			propertyChanged: OnCardContentChanged);

		/// <summary>
		/// Identifies the <see cref="CardStyle"/> bindable property.
		/// </summary>
		public static readonly BindableProperty CardStyleProperty = BindableProperty.Create(
			nameof(CardStyle),
			typeof(Style),
			typeof(SimplePopup),
			null,
			propertyChanged: OnCardStyleChanged);

		/// <summary>
		/// Identifies the <see cref="CardPadding"/> bindable property.
		/// </summary>
		public static readonly BindableProperty CardPaddingProperty = BindableProperty.Create(
			nameof(CardPadding),
			typeof(Thickness),
			typeof(SimplePopup),
			new Thickness(16),
			propertyChanged: OnCardPaddingChanged);

		/// <summary>
		/// Identifies the <see cref="CardMargin"/> bindable property.
		/// </summary>
		public static readonly BindableProperty CardMarginProperty = BindableProperty.Create(
			nameof(CardMargin),
			typeof(Thickness),
			typeof(SimplePopup),
			new Thickness(0),
			propertyChanged: OnCardMarginChanged);

		/// <summary>
		/// Identifies the <see cref="CardWidthFraction"/> bindable property.
		/// </summary>
		public static readonly BindableProperty CardWidthFractionProperty = BindableProperty.Create(
			nameof(CardWidthFraction),
			typeof(double),
			typeof(SimplePopup),
			0.875,
			propertyChanged: OnSizingPropertyChanged);

		/// <summary>
		/// Identifies the <see cref="CardMaxHeightFraction"/> bindable property.
		/// </summary>
		public static readonly BindableProperty CardMaxHeightFractionProperty = BindableProperty.Create(
			nameof(CardMaxHeightFraction),
			typeof(double),
			typeof(SimplePopup),
			0.8,
			propertyChanged: OnSizingPropertyChanged);

		private readonly Border cardFrame;
		private readonly ContentView contentHost;

		/// <summary>
		/// Initializes a new instance of the <see cref="SimplePopup"/> class.
		/// </summary>
		public SimplePopup()
		{
			this.contentHost = new ContentView { HorizontalOptions = LayoutOptions.Fill };

			this.cardFrame = new Border
			{
				HorizontalOptions = LayoutOptions.Fill,
				VerticalOptions = LayoutOptions.Center,
				Padding = this.CardPadding,
				Margin = this.CardMargin
			};
			this.cardFrame.Content = this.contentHost;

			this.AllowLayoutOverrides = true;
			this.Placement = PopupPlacement.Center;
			this.PopupMargin = new Thickness(16);
			this.StretchContentWidth = true;
			this.StretchContentHeight = false;
			this.PopupContent = this.cardFrame;

			this.SetDynamicResource(CardStyleProperty, "PopupBorder");

			this.SizeChanged += this.OnSizeChanged;
		}

		/// <summary>
		/// Content presented inside the centered card.
		/// </summary>
		public View? CardContent
		{
			get => (View?)this.GetValue(CardContentProperty);
			set => this.SetValue(CardContentProperty, value);
		}

		/// <summary>
		/// Optional style applied to the card <see cref="Border"/>.
		/// </summary>
		public Style? CardStyle
		{
			get => (Style?)this.GetValue(CardStyleProperty);
			set => this.SetValue(CardStyleProperty, value);
		}

		/// <summary>
		/// Padding inside the card frame.
		/// </summary>
		public Thickness CardPadding
		{
			get => (Thickness)this.GetValue(CardPaddingProperty);
			set => this.SetValue(CardPaddingProperty, value);
		}

		/// <summary>
		/// Margin applied around the card frame.
		/// </summary>
		public Thickness CardMargin
		{
			get => (Thickness)this.GetValue(CardMarginProperty);
			set => this.SetValue(CardMarginProperty, value);
		}

		/// <summary>
		/// Fraction of available width used as the maximum card width.
		/// </summary>
		public double CardWidthFraction
		{
			get => (double)this.GetValue(CardWidthFractionProperty);
			set => this.SetValue(CardWidthFractionProperty, value);
		}

		/// <summary>
		/// Fraction of available height used as the maximum card height.
		/// </summary>
		public double CardMaxHeightFraction
		{
			get => (double)this.GetValue(CardMaxHeightFractionProperty);
			set => this.SetValue(CardMaxHeightFractionProperty, value);
		}

		private static void OnCardContentChanged(BindableObject Bindable, object? OldValue, object? NewValue)
		{
			if (Bindable is SimplePopup Popup)
				Popup.contentHost.Content = NewValue as View;
		}

		private static void OnCardStyleChanged(BindableObject Bindable, object? OldValue, object? NewValue)
		{
			if (Bindable is SimplePopup Popup)
				Popup.cardFrame.Style = NewValue as Style;
		}

		private static void OnCardPaddingChanged(BindableObject Bindable, object? OldValue, object? NewValue)
		{
			if (Bindable is SimplePopup Popup && NewValue is Thickness Padding)
				Popup.cardFrame.Padding = Padding;
		}

		private static void OnCardMarginChanged(BindableObject Bindable, object? OldValue, object? NewValue)
		{
			if (Bindable is SimplePopup Popup && NewValue is Thickness Margin)
				Popup.cardFrame.Margin = Margin;
		}

		private static void OnSizingPropertyChanged(BindableObject Bindable, object? OldValue, object? NewValue)
		{
			if (Bindable is SimplePopup Popup)
				Popup.UpdateCardSizing();
		}

		private void OnSizeChanged(object? Sender, System.EventArgs Args) => this.UpdateCardSizing();

		private void UpdateCardSizing()
		{
			if (this.Width <= 0 || this.Height <= 0)
				return;

			double MaxWidth = this.Width * this.CardWidthFraction;
			double MaxHeight = this.Height * this.CardMaxHeightFraction;
			this.cardFrame.WidthRequest = MaxWidth;
			this.cardFrame.MaximumHeightRequest = MaxHeight;
		}
	}
}
