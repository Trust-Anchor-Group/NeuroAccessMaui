using Microsoft.Maui.Controls;

namespace NeuroAccessMaui.UI.Popups
{
	/// <summary>
	/// Provides a centered card layout with configurable sizing constraints.
	/// </summary>
	[ContentProperty(nameof(CardContent))]
	public class BasicPopup : BasePopupView
	{
		public static readonly BindableProperty CardContentProperty = BindableProperty.Create(
			nameof(CardContent),
			typeof(View),
			typeof(BasicPopup),
			null,
			propertyChanged: OnCardContentChanged);

		public static readonly BindableProperty CardStyleProperty = BindableProperty.Create(
			nameof(CardStyle),
			typeof(Style),
			typeof(BasicPopup),
			null,
			propertyChanged: OnCardStyleChanged);

		public static readonly BindableProperty CardPaddingProperty = BindableProperty.Create(
			nameof(CardPadding),
			typeof(Thickness),
			typeof(BasicPopup),
			new Thickness(16),
			propertyChanged: OnCardPaddingChanged);

		public static readonly BindableProperty CardMarginProperty = BindableProperty.Create(
			nameof(CardMargin),
			typeof(Thickness),
			typeof(BasicPopup),
			new Thickness(0),
			propertyChanged: OnCardMarginChanged);

		public static readonly BindableProperty CardWidthFractionProperty = BindableProperty.Create(
			nameof(CardWidthFraction),
			typeof(double),
			typeof(BasicPopup),
			0.875,
			propertyChanged: OnSizingPropertyChanged);

		public static readonly BindableProperty CardMaxHeightFractionProperty = BindableProperty.Create(
			nameof(CardMaxHeightFraction),
			typeof(double),
			typeof(BasicPopup),
			0.8,
			propertyChanged: OnSizingPropertyChanged);

		private readonly Border cardFrame;
		private readonly ContentView contentHost;

		public BasicPopup()
		{
			this.contentHost = new ContentView();

			this.cardFrame = new Border
			{
				HorizontalOptions = LayoutOptions.Fill,
				VerticalOptions = LayoutOptions.Center,
				Padding = this.CardPadding,
				Margin = this.CardMargin
			};
			this.cardFrame.Content = this.contentHost;

			Grid root = new Grid
			{
				HorizontalOptions = LayoutOptions.Fill,
				VerticalOptions = LayoutOptions.Fill,
				Padding = new Thickness(16)
			};
			root.InputTransparent = true;
			root.CascadeInputTransparent = false;
			root.Add(this.cardFrame);

			this.PopupContent = root;

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

		private static void OnCardContentChanged(BindableObject bindable, object? oldValue, object? newValue)
		{
			if (bindable is BasicPopup popup)
				popup.contentHost.Content = newValue as View;
		}

		private static void OnCardStyleChanged(BindableObject bindable, object? oldValue, object? newValue)
		{
			if (bindable is BasicPopup popup)
				popup.cardFrame.Style = newValue as Style;
		}

		private static void OnCardPaddingChanged(BindableObject bindable, object? oldValue, object? newValue)
		{
			if (bindable is BasicPopup popup && newValue is Thickness padding)
				popup.cardFrame.Padding = padding;
		}

		private static void OnCardMarginChanged(BindableObject bindable, object? oldValue, object? newValue)
		{
			if (bindable is BasicPopup popup && newValue is Thickness margin)
				popup.cardFrame.Margin = margin;
		}

		private static void OnSizingPropertyChanged(BindableObject bindable, object? oldValue, object? newValue)
		{
			if (bindable is BasicPopup popup)
				popup.UpdateCardSizing();
		}

		private void OnSizeChanged(object? sender, System.EventArgs e) => this.UpdateCardSizing();

		private void UpdateCardSizing()
		{
			if (this.Width <= 0 || this.Height <= 0)
				return;

			double maxWidth = this.Width * this.CardWidthFraction;
			double maxHeight = this.Height * this.CardMaxHeightFraction;

			this.cardFrame.MaximumWidthRequest = maxWidth;
			this.cardFrame.MaximumHeightRequest = maxHeight;
		}
	}
}
