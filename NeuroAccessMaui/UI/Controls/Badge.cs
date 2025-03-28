using System;
using System.ComponentModel;
using System.Threading.Tasks;
using System.Windows.Input;
using Microsoft.Maui;
using Microsoft.Maui.Controls;
using Microsoft.Maui.Controls.Shapes;
using Microsoft.Maui.Graphics;

namespace NeuroAccessMaui.UI.Controls
{
	public class Badge : Border
	{
		private readonly Label badgeLabel;

		// Bindable properties
		public static readonly BindableProperty TextProperty =
			  BindableProperty.Create(
					 nameof(Text),
					 typeof(string),
					 typeof(Badge),
					 default(string),
					 propertyChanged: OnTextChanged);

		public static readonly BindableProperty FontSizeProperty =
			  BindableProperty.Create(
					 nameof(FontSize),
					 typeof(double),
					 typeof(Badge),
					 12d,
					 propertyChanged: OnFontSizeChanged);

		public static readonly BindableProperty TextColorProperty =
			  BindableProperty.Create(
					 nameof(TextColor),
					 typeof(Color),
					 typeof(Badge),
					 Colors.White,
					 propertyChanged: OnTextColorChanged);

		public static readonly BindableProperty FontFamilyProperty =
			  BindableProperty.Create(
					 nameof(FontFamily),
					 typeof(string),
					 typeof(Badge),
					 default(string),
					 propertyChanged: OnFontFamilyChanged);

		// New CornerRadius bindable property.
		public static readonly BindableProperty CornerRadiusProperty =
			  BindableProperty.Create(
					 nameof(CornerRadius),
					 typeof(double),
					 typeof(Badge),
					 10d,
					 propertyChanged: OnCornerRadiusChanged);

		public static readonly BindableProperty IsIndicatorProperty =
			  BindableProperty.Create(
					 nameof(IsIndicator),
					 typeof(bool),
					 typeof(Badge),
					 false,
					 propertyChanged: OnIsIndicatorChanged);

		public static readonly BindableProperty CommandProperty =
			  BindableProperty.Create(
					 nameof(Command),
					 typeof(ICommand),
					 typeof(Badge),
					 null);



		/// <summary>
		/// If true, the badge will automatically translate itself.
		/// </summary>
		public static readonly BindableProperty AutoTranslateProperty =
			  BindableProperty.Create(
					 nameof(AutoTranslate),
					 typeof(bool),
					 typeof(Badge),
					 false,
					 propertyChanged: OnAutoTranslateChanged);




		/// <summary>
		/// The factor (0.0 to 1.0) of half the badge’s size to use for translation.
		/// A value of 0.5 (the default) translates by half the badge’s width/height.
		/// Lower values will shift it less.
		/// </summary>
		public static readonly BindableProperty BadgeTranslationFactorProperty =
			  BindableProperty.Create(
					 nameof(BadgeTranslationFactor),
					 typeof(double),
					 typeof(Badge),
					 0.5,
					 propertyChanged: OnBadgeTranslationFactorChanged);


		// New TextMargin bindable property.
		public static readonly BindableProperty TextMarginProperty =
			 BindableProperty.Create(
				  nameof(TextMargin),
				  typeof(Thickness),
				  typeof(Badge),
				  new Thickness(0),
				  propertyChanged: OnTextMarginChanged);

		// CLR property for TextMargin.
		public Thickness TextMargin
		{
			get => (Thickness)GetValue(TextMarginProperty);
			set => SetValue(TextMarginProperty, value);
		}

		// CLR properties for binding
		public string Text
		{
			get => (string)GetValue(TextProperty);
			set => SetValue(TextProperty, value);
		}

		public double FontSize
		{
			get => (double)GetValue(FontSizeProperty);
			set => SetValue(FontSizeProperty, value);
		}

		public Color TextColor
		{
			get => (Color)GetValue(TextColorProperty);
			set => SetValue(TextColorProperty, value);
		}

		public string FontFamily
		{
			get => (string)GetValue(FontFamilyProperty);
			set => SetValue(FontFamilyProperty, value);
		}

		// New CornerRadius CLR property.
		public double CornerRadius
		{
			get => (double)GetValue(CornerRadiusProperty);
			set => SetValue(CornerRadiusProperty, value);
		}

		/// <summary>
		/// When true, the badge displays a small dot instead of text.
		/// </summary>
		public bool IsIndicator
		{
			get => (bool)GetValue(IsIndicatorProperty);
			set => SetValue(IsIndicatorProperty, value);
		}

		/// <summary>
		/// Optional command to invoke when the badge is tapped.
		/// </summary>
		public ICommand Command
		{
			get => (ICommand)GetValue(CommandProperty);
			set => SetValue(CommandProperty, value);
		}

		/// <summary>
		/// If true, automatically translates the badge so it sits on the edge of its parent.
		/// </summary>
		public bool AutoTranslate
		{
			get => (bool)GetValue(AutoTranslateProperty);
			set => SetValue(AutoTranslateProperty, value);
		}

		/// <summary>
		/// Determines the fraction (of half the badge’s size) used for translation.
		/// </summary>
		public double BadgeTranslationFactor
		{
			get => (double)GetValue(BadgeTranslationFactorProperty);
			set => SetValue(BadgeTranslationFactorProperty, value);
		}


		// Hide StrokeShape from consumers.
		[EditorBrowsable(EditorBrowsableState.Never)]
		public new IShape StrokeShape
		{
			get => base.StrokeShape;
			private set => base.StrokeShape = value;
		}

		public Badge()
		{
			// Default styling for the badge.
			BackgroundColor = Colors.Red;
			// Use the CornerRadius property value.
			StrokeShape = new RoundRectangle { CornerRadius = new CornerRadius(CornerRadius) };

			// Create and configure the label that shows the text.
			badgeLabel = new Label
			{
				HorizontalOptions = LayoutOptions.Center,
				VerticalOptions = LayoutOptions.Center,
				Text = Text,
				FontSize = FontSize,
				TextColor = TextColor,
				FontFamily = FontFamily
			};

			// Set the label as the content of the Border.
			Content = badgeLabel;

			// Add a tap gesture recognizer so the badge can be interactive.
			var tapGesture = new TapGestureRecognizer();
			tapGesture.Tapped += (s, e) =>
			{
				if (Command?.CanExecute(null) == true)
				{
					Command.Execute(null);
				}
			};
			GestureRecognizers.Add(tapGesture);

			// Listen for size changes so we can update translation if AutoTranslate is true.
			SizeChanged += (s, e) =>
			{
				if (AutoTranslate)
					UpdateTranslation();
			};
		}

		private static void OnTextMarginChanged(BindableObject bindable, object oldValue, object newValue)
		{
			if (bindable is Badge badge && newValue is Thickness newMargin)
			{
				badge.badgeLabel.Margin = newMargin;
			}
		}

		// Called when Text property changes.
		private static async void OnTextChanged(BindableObject bindable, object oldValue, object newValue)
		{
			if (bindable is Badge badge)
			{
				string textValue = newValue?.ToString() ?? string.Empty;
				if (!badge.IsIndicator)
				{
					badge.badgeLabel.Text = textValue;
					// Simple bounce animation to draw attention on change.
					await badge.ScaleTo(1.2, 50);
					await badge.ScaleTo(1.0, 50);
				}

				// If the text is a single character, change to circular shape.
				if (textValue.Length == 1)
				{
					// Optionally enforce a square layout for a perfect circle.
					double diameter = Math.Max(badge.Width, badge.Height);
					badge.WidthRequest = diameter;
					badge.HeightRequest = diameter;

					// Use an Ellipse shape to make the border circular.
					badge.StrokeShape = new Ellipse {};
				}
				else
				{
					// Revert back to the default rounded rectangle shape using the CornerRadius property.
					badge.StrokeShape = new RoundRectangle { CornerRadius = new CornerRadius(badge.CornerRadius)};
				}
			}
		}

		private static void OnFontSizeChanged(BindableObject bindable, object oldValue, object newValue)
		{
			if (bindable is Badge badge)
			{
				badge.badgeLabel.FontSize = (double)newValue;
			}
		}

		private static void OnTextColorChanged(BindableObject bindable, object oldValue, object newValue)
		{
			if (bindable is Badge badge)
			{
				badge.badgeLabel.TextColor = (Color)newValue;
			}
		}

		private static void OnFontFamilyChanged(BindableObject bindable, object oldValue, object newValue)
		{
			if (bindable is Badge badge)
			{
				badge.badgeLabel.FontFamily = newValue?.ToString();
			}
		}

		// Called when CornerRadius property changes.
		private static void OnCornerRadiusChanged(BindableObject bindable, object oldValue, object newValue)
		{
			if (bindable is Badge badge && newValue is double newCornerRadius)
			{
				// If the current shape is a RoundRectangle, update its CornerRadius.
				if (badge.StrokeShape is RoundRectangle roundRectangle)
				{
					roundRectangle.CornerRadius = new CornerRadius(newCornerRadius);
				}
				// If not in indicator mode and not displaying a single character, reset the shape.
				else if (!badge.IsIndicator && (badge.Text?.Length ?? 0) != 1)
				{
					badge.StrokeShape = new RoundRectangle { CornerRadius = new CornerRadius(newCornerRadius) };
				}
			}
		}

		private static void OnIsIndicatorChanged(BindableObject bindable, object oldValue, object newValue)
		{
			if (bindable is Badge badge)
			{
				bool isIndicator = (bool)newValue;
				if (isIndicator)
				{
					// In indicator mode: hide text and force a small circular dot.
					badge.badgeLabel.Text = string.Empty;
					badge.HeightRequest = 10;
					badge.WidthRequest = 10;
				}
				else
				{
					// Exit indicator mode: restore auto sizing and display text.
					badge.HeightRequest = -1;
					badge.WidthRequest = -1;
					badge.badgeLabel.Text = badge.Text;
				}
			}
		}

		private static void OnAutoTranslateChanged(BindableObject bindable, object oldValue, object newValue)
		{
			if (bindable is Badge badge)
			{
				badge.UpdateTranslation();
			}
		}

		private static void OnBadgeTranslationFactorChanged(BindableObject bindable, object oldValue, object newValue)
		{
			if (bindable is Badge badge)
			{
				badge.UpdateTranslation();
			}
		}

		protected override void OnPropertyChanged(string propertyName = null)
		{
			base.OnPropertyChanged(propertyName);

			// When HorizontalOptions or VerticalOptions change, update translation if needed.
			if (AutoTranslate && (propertyName == nameof(HorizontalOptions) || propertyName == nameof(VerticalOptions)))
			{
				UpdateTranslation();
			}
		}

		/// <summary>
		/// Offsets the badge by a fraction of its half width/height based on its HorizontalOptions and VerticalOptions.
		/// </summary>
		private void UpdateTranslation()
		{
			if (!AutoTranslate || Width <= 0 || Height <= 0)
				return;

			// Calculate a scaled half-width/height using the translation factor.
			double offsetX = (Width / 2) * BadgeTranslationFactor;
			double offsetY = (Height / 2) * BadgeTranslationFactor;
			double translateX = 0;
			double translateY = 0;

			// Adjust horizontal translation based on alignment.
			switch (HorizontalOptions.Alignment)
			{
				case LayoutAlignment.Start:
					translateX = -offsetX;
					break;
				case LayoutAlignment.End:
					translateX = offsetX;
					break;
				default:
					translateX = 0;
					break;
			}

			// Adjust vertical translation based on alignment.
			switch (VerticalOptions.Alignment)
			{
				case LayoutAlignment.Start:
					translateY = -offsetY;
					break;
				case LayoutAlignment.End:
					translateY = offsetY;
					break;
				default:
					translateY = 0;
					break;
			}

			TranslationX = translateX;
			TranslationY = translateY;
		}
	}
}
