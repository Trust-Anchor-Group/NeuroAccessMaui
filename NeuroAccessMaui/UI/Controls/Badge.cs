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
	/// <summary>
	/// Represents a badge control that displays a short text (e.g., a notification count) or a simple indicator dot.
	/// </summary>
	/// <remarks>
	/// <para>
	/// The <c>Badge</c> control is designed to be used as a visual indicator for notifications, statuses, or updates.
	/// It can display a textual value (such as a number) or function as a simple indicator when <see cref="IsIndicator"/> is set to <c>true</c>.
	/// </para>
	/// <para>
	/// The control automatically adapts its layout and appearance based on its content:
	/// <list type="bullet">
	/// <item>
	/// <description>
	/// For single-character text, the badge enforces a circular shape by using an <see cref="Ellipse"/> shape.
	/// </description>
	/// </item>
	/// <item>
	/// <description>
	/// For multi-character text, it auto-sizes to accommodate the content and uses a <see cref="RoundRectangle"/> with a dynamically adjusted corner radius.
	/// </description>
	/// </item>
	/// <item>
	/// <description>
	/// In indicator mode, a fixed-size circular shape is applied based on the constant <c>IndicatorSize</c>.
	/// </description>
	/// </item>
	/// </list>
	/// </para>
	/// <para>
	/// The control exposes several bindable properties including <see cref="Text"/>, <see cref="FontSize"/>, <see cref="TextColor"/>, <see cref="FontFamily"/>,
	/// <see cref="CornerRadius"/>, and <see cref="TextMargin"/>, allowing full customization of its appearance and behavior.
	/// </para>
	/// <para>
	/// Additionally, the badge supports interactivity via the <see cref="Command"/> property, allowing a tap gesture to trigger an associated command.
	/// </para>
	/// <para>
	/// <b>Example usage in XAML:</b>
	/// <code language="xml">
	/// <![CDATA[
	/// <controls:Badge Text="99"
	///                 FontSize="14"
	///                 TextColor="White"
	///                 BackgroundColor="Red"
	///                 CornerRadius="12"
	///                 TextMargin="4"
	///                 Command="{Binding BadgeTappedCommand}" />
	/// ]]>
	/// </code>
	/// </para>
	/// </remarks>
	public class Badge : Border
	{
		#region Fields

		/// <summary>
		/// The label that displays the badge text.
		/// </summary>
		private readonly Label badgeLabel;

		/// <summary>
		/// The fixed size for the indicator badge.
		/// </summary>
		private const double indicatorSize = 10;

		#endregion

		#region Bindable Properties

		/// <summary>
		/// Backing store for the <see cref="Text"/> property.
		/// </summary>
		public static readonly BindableProperty TextProperty =
			 BindableProperty.Create(
				  nameof(Text),
				  typeof(string),
				  typeof(Badge),
				  default(string),
				  propertyChanged: OnTextChanged);

		/// <summary>
		/// Backing store for the <see cref="FontSize"/> property.
		/// </summary>
		public static readonly BindableProperty FontSizeProperty =
			 BindableProperty.Create(
				  nameof(FontSize),
				  typeof(double),
				  typeof(Badge),
				  12d,
				  propertyChanged: OnFontSizeChanged);

		/// <summary>
		/// Backing store for the <see cref="TextColor"/> property.
		/// </summary>
		public static readonly BindableProperty TextColorProperty =
			 BindableProperty.Create(
				  nameof(TextColor),
				  typeof(Color),
				  typeof(Badge),
				  Colors.White,
				  propertyChanged: OnTextColorChanged);

		/// <summary>
		/// Backing store for the <see cref="FontFamily"/> property.
		/// </summary>
		public static readonly BindableProperty FontFamilyProperty =
			 BindableProperty.Create(
				  nameof(FontFamily),
				  typeof(string),
				  typeof(Badge),
				  default(string),
				  propertyChanged: OnFontFamilyChanged);

		/// <summary>
		/// Backing store for the <see cref="CornerRadius"/> property.
		/// </summary>
		public static readonly BindableProperty CornerRadiusProperty =
			 BindableProperty.Create(
				  nameof(CornerRadius),
				  typeof(double),
				  typeof(Badge),
				  10d,
				  propertyChanged: OnCornerRadiusChanged);

		/// <summary>
		/// Backing store for the <see cref="IsIndicator"/> property.
		/// </summary>
		public static readonly BindableProperty IsIndicatorProperty =
			 BindableProperty.Create(
				  nameof(IsIndicator),
				  typeof(bool),
				  typeof(Badge),
				  false,
				  propertyChanged: OnIsIndicatorChanged);

		/// <summary>
		/// Backing store for the <see cref="Command"/> property.
		/// </summary>
		public static readonly BindableProperty CommandProperty =
			 BindableProperty.Create(
				  nameof(Command),
				  typeof(ICommand),
				  typeof(Badge),
				  null);

		/// <summary>
		/// Backing store for the <see cref="TextMargin"/> property.
		/// </summary>
		public static readonly BindableProperty TextMarginProperty =
			 BindableProperty.Create(
				  nameof(TextMargin),
				  typeof(Thickness),
				  typeof(Badge),
				  new Thickness(4),
				  propertyChanged: OnTextMarginChanged);

		#endregion

		#region Properties

		/// <summary>
		/// Gets or sets the text displayed on the badge.
		/// </summary>
		public string Text
		{
			get => (string)this.GetValue(TextProperty);
			set => this.SetValue(TextProperty, value);
		}

		/// <summary>
		/// Gets or sets the font size of the badge text.
		/// </summary>
		public double FontSize
		{
			get => (double)this.GetValue(FontSizeProperty);
			set => this.SetValue(FontSizeProperty, value);
		}

		/// <summary>
		/// Gets or sets the color of the badge text.
		/// </summary>
		public Color TextColor
		{
			get => (Color)this.GetValue(TextColorProperty);
			set => this.SetValue(TextColorProperty, value);
		}

		/// <summary>
		/// Gets or sets the font family of the badge text.
		/// </summary>
		public string FontFamily
		{
			get => (string)this.GetValue(FontFamilyProperty);
			set => this.SetValue(FontFamilyProperty, value);
		}

		/// <summary>
		/// Gets or sets the corner radius of the badge.
		/// </summary>
		public double CornerRadius
		{
			get => (double)this.GetValue(CornerRadiusProperty);
			set => this.SetValue(CornerRadiusProperty, value);
		}

		/// <summary>
		/// Gets or sets a value indicating whether the badge is displayed as an indicator.
		/// </summary>
		public bool IsIndicator
		{
			get => (bool)this.GetValue(IsIndicatorProperty);
			set => this.SetValue(IsIndicatorProperty, value);
		}

		/// <summary>
		/// Gets or sets the command to be executed when the badge is tapped.
		/// </summary>
		public ICommand Command
		{
			get => (ICommand)this.GetValue(CommandProperty);
			set => this.SetValue(CommandProperty, value);
		}

		/// <summary>
		/// Gets or sets the margin for the badge text.
		/// </summary>
		public Thickness TextMargin
		{
			get => (Thickness)this.GetValue(TextMarginProperty);
			set => this.SetValue(TextMarginProperty, value);
		}

		#endregion

		#region Constructors

		/// <summary>
		/// Initializes a new instance of the <see cref="Badge"/> class.
		/// </summary>
		public Badge()
		{
			// Set default styling.
			this.BackgroundColor = Colors.Red;
			// Initially use the defined CornerRadius property.
			this.StrokeShape = new RoundRectangle { CornerRadius = new CornerRadius(this.CornerRadius) };

			this.badgeLabel = new Label
			{
				HorizontalOptions = LayoutOptions.Center,
				VerticalOptions = LayoutOptions.Center,
				Text = this.Text,
				FontSize = this.FontSize,
				TextColor = this.TextColor,
				FontFamily = this.FontFamily
			};

			this.Content = this.badgeLabel;

			TapGestureRecognizer TapGesture = new TapGestureRecognizer();
			TapGesture.Tapped += this.OnBadgeTapped;
			this.GestureRecognizers.Add(TapGesture);
		}

		#endregion

		#region Event Handlers

		/// <summary>
		/// Handles the badge tap gesture.
		/// </summary>
		/// <param name="sender">The source of the event.</param>
		/// <param name="e">Event arguments.</param>
		private void OnBadgeTapped(object? sender, EventArgs e)
		{
			if (this.Command?.CanExecute(null) == true)
			{
				this.Command.Execute(null);
			}
		}

		#endregion

		#region Bindable Property Changed Callbacks

		/// <summary>
		/// Called when the <see cref="TextMargin"/> property changes.
		/// </summary>
		/// <param name="bindable">The bindable object.</param>
		/// <param name="oldValue">The old value.</param>
		/// <param name="newValue">The new value.</param>
		private static void OnTextMarginChanged(BindableObject bindable, object oldValue, object newValue)
		{
			if (bindable is Badge badge && newValue is Thickness newMargin)
			{
				badge.badgeLabel.Margin = newMargin;
			}
		}

		/// <summary>
		/// Called when the <see cref="Text"/> property changes.
		/// </summary>
		/// <param name="bindable">The bindable object.</param>
		/// <param name="oldValue">The old value.</param>
		/// <param name="newValue">The new value.</param>
		private static async void OnTextChanged(BindableObject bindable, object oldValue, object newValue)
		{
			if (bindable is Badge badge)
			{
				string TextValue = newValue?.ToString() ?? string.Empty;
				// Update visibility based on the new text value.
				badge.UpdateVisibility();

				if (!badge.IsIndicator)
				{
					badge.badgeLabel.Text = TextValue;
					await badge.AnimateBounceAsync();
				}

				// Enforce a circular shape if the text is a single character.
				if (TextValue.Length == 1)
				{
					double Diameter = Math.Max(badge.Width, badge.Height);
					badge.WidthRequest = Diameter;
					badge.HeightRequest = Diameter;
					badge.StrokeShape = new Ellipse();
				}
				else
				{
					// For multi-character text, allow the badge to auto-size.
					badge.WidthRequest = -1;
					badge.HeightRequest = -1;
					// For multi-character text, update the corner radius dynamically.
					badge.UpdateCornerRadius();
				}
			}
		}

		/// <summary>
		/// Called when the <see cref="FontSize"/> property changes.
		/// </summary>
		/// <param name="bindable">The bindable object.</param>
		/// <param name="oldValue">The old value.</param>
		/// <param name="newValue">The new value.</param>
		private static void OnFontSizeChanged(BindableObject bindable, object oldValue, object newValue)
		{
			if (bindable is Badge badge)
			{
				badge.badgeLabel.FontSize = (double)newValue;
			}
		}

		/// <summary>
		/// Called when the <see cref="TextColor"/> property changes.
		/// </summary>
		private static void OnTextColorChanged(BindableObject bindable, object oldValue, object newValue)
		{
			if (bindable is Badge badge)
			{
				badge.badgeLabel.TextColor = (Color)newValue;
			}
		}

		/// <summary>
		/// Called when the <see cref="FontFamily"/> property changes.
		/// </summary>
		private static void OnFontFamilyChanged(BindableObject bindable, object oldValue, object newValue)
		{
			if (bindable is Badge badge)
			{
				badge.badgeLabel.FontFamily = newValue?.ToString();
			}
		}

		/// <summary>
		/// Called when the <see cref="CornerRadius"/> property changes.
		/// </summary>
		private static void OnCornerRadiusChanged(BindableObject bindable, object oldValue, object newValue)
		{
			if (bindable is Badge badge && newValue is double)
			{
				// When the user sets CornerRadius explicitly, update the shape.
				badge.UpdateCornerRadius();
			}
		}

		/// <summary>
		/// Called when the <see cref="IsIndicator"/> property changes.
		/// </summary>
		private static void OnIsIndicatorChanged(BindableObject bindable, object oldValue, object newValue)
		{
			if (bindable is Badge badge)
			{
				bool IsIndicatorValue = (bool)newValue;
				if (IsIndicatorValue)
				{
					badge.badgeLabel.Text = string.Empty;
					badge.HeightRequest = indicatorSize;
					badge.WidthRequest = indicatorSize;
					badge.StrokeShape = new RoundRectangle { CornerRadius = new CornerRadius(indicatorSize / 2) };
				}
				else
				{
					badge.HeightRequest = -1;
					badge.WidthRequest = -1;
					badge.badgeLabel.Text = badge.Text;
					badge.UpdateCornerRadius();
				}
				badge.UpdateVisibility();
			}
		}

		#endregion

		#region Private Methods

		/// <summary>
		/// Performs a simple bounce animation to draw attention when the text changes.
		/// </summary>
		/// <returns>A task representing the asynchronous animation operation.</returns>
		private async Task AnimateBounceAsync()
		{
			await this.ScaleTo(1.2, 50);
			await this.ScaleTo(1.0, 50);
		}

		/// <summary>
		/// Updates the visibility of the badge based on its content and mode.
		/// </summary>
		private void UpdateVisibility()
		{
			if (this.IsIndicator)
			{
				this.IsVisible = true;
			}
			else
			{
				if (string.IsNullOrEmpty(this.Text))
				{
					this.IsVisible = false;
				}
				else if (int.TryParse(this.Text, out int Count))
				{
					this.IsVisible = Count > 0;
				}
				else
				{
					this.IsVisible = true;
				}
			}
		}

		/// <summary>
		/// Dynamically updates the <see cref="Shape"/> based on the control's current height.
		/// For multi-character text, it uses a <see cref="RoundRectangle"/> with a corner radius that is the smaller of the defined <see cref="CornerRadius"/> or half the control height.
		/// For single-character text, an <see cref="Ellipse"/> is used.
		/// For indicator mode, a fixed circular shape is applied.
		/// </summary>
		private void UpdateCornerRadius()
		{
			if (this.IsIndicator)
			{
				this.StrokeShape = new RoundRectangle { CornerRadius = new CornerRadius(indicatorSize / 2) };
			}
			else if (this.Height > 0)
			{
				if (!string.IsNullOrEmpty(this.Text) && this.Text.Length == 1)
				{
					this.StrokeShape = new Ellipse();
				}
				else
				{
					// Use the smaller value between the user-set CornerRadius and half the control's height.
					double EffectiveCornerRadius = Math.Min(this.CornerRadius, this.Height / 2);
					this.StrokeShape = new RoundRectangle { CornerRadius = new CornerRadius(EffectiveCornerRadius) };
				}
			}
		}

		#endregion

		#region Overrides
		protected override void OnBindingContextChanged()
		{
			base.OnBindingContextChanged();

			// If the badge is in indicator mode, force the correct sizing and shape.
			if (IsIndicator)
			{
				HeightRequest = indicatorSize;
				WidthRequest = indicatorSize;
				StrokeShape = new RoundRectangle { CornerRadius = new CornerRadius(indicatorSize / 2) };
			}

			// Reapply visibility and corner radius adjustments.
			UpdateVisibility();
			UpdateCornerRadius();

		}

		protected override void OnSizeAllocated(double width, double height)
		{
			base.OnSizeAllocated(width, height);

			// Update the corner radius based on the new size.
			this.UpdateCornerRadius();

			// For multi-character text, ensure the width is at least the measured text width plus padding,
			// and also at least equal to the height (to maintain a pill shape).
			if (!this.IsIndicator && !string.IsNullOrEmpty(this.Text) && this.Text.Length > 1)
			{
				// Measure the label's requested size.
				Size Request = this.badgeLabel.Measure(width, height).Request;
				double DesiredWidth = Math.Max(Request.Width + this.TextMargin.HorizontalThickness, height);

				// Only override if the current width is smaller than desired.
				if (width < DesiredWidth)
				{
					this.WidthRequest = DesiredWidth;
				}
			}
		}


		#endregion
	}
}
