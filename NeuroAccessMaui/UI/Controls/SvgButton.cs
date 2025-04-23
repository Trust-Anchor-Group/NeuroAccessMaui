using System.Runtime.CompilerServices;
using NeuroAccessMaui.UI.Core;
using NeuroAccessMaui.UI.Popups.Xmpp.RemoveSubscription;
using Waher.Script.Functions.Runtime;

namespace NeuroAccessMaui.UI.Controls
{
	class SvgButton : TemplatedButton, IDisposable
	{
		private readonly SvgView innerSvg;
		private readonly Label innerLabel;
		private readonly Border innerBorder;

		/// <summary>Bindable property for <see cref="SvgSource"/>.</summary>
		public static readonly BindableProperty SvgSourceProperty = BindableProperty.Create(
			nameof(SvgSource),
			typeof(string),
			typeof(SvgButton),
			propertyChanged: OnSvgSourceChanged);

		/// <summary>Bindable property for <see cref="SvgSource"/>.</summary>
		public static readonly BindableProperty SvgAspectProperty = BindableProperty.Create(
			nameof(SvgAspect),
			typeof(Aspect),
			typeof(SvgButton),
			propertyChanged: OnSvgAspectChanged);

		/// <summary>Bindable property for <see cref="SvgSource"/>.</summary>
		public static readonly BindableProperty SvgTintColorProperty = BindableProperty.Create(
			nameof(SvgTintColor),
			typeof(Color),
			typeof(SvgButton),
			propertyChanged: OnSvgTintColorChanged);

		public static readonly BindableProperty LabelTextProperty = BindableProperty.Create(
			nameof(LabelText),
			typeof(string),
			typeof(SvgButton),
			defaultValue: string.Empty,
			propertyChanged: OnLabelTextChanged);

		public static readonly BindableProperty LabelStyleProperty = BindableProperty.Create(
			nameof(LabelStyle),
			typeof(Style),
			typeof(SvgButton),
			propertyChanged: OnLabelTextChanged);

		/// <summary>Bindable property for <see cref="LabelPosition"/>.</summary>
		public static readonly BindableProperty LabelPositionProperty = BindableProperty.Create(
			nameof(LabelPosition),
			typeof(LabelPosition),
			typeof(SvgButton),
			defaultValue: LabelPosition.Left,
			propertyChanged: OnLabelPositionChanged);

		/// <summary>Bindable property for <see cref="BorderStyle"/>.</summary>
		public static readonly BindableProperty BorderStyleProperty = BindableProperty.Create(
				nameof(BorderStyle),
				typeof(Style),
				typeof(SvgButton),
				propertyChanged: OnBorderStyleChanged);

		public static void OnSvgSourceChanged(BindableObject Bindable, object OldValue, object NewValue)
		{
			((SvgButton)Bindable).innerSvg.Source = (string)NewValue;
		}

		public static void OnSvgAspectChanged(BindableObject Bindable, object OldValue, object NewValue)
		{
			((SvgButton)Bindable).innerSvg.Aspect = (Aspect)NewValue;
		}

		public static void OnSvgTintColorChanged(BindableObject Bindable, object OldValue, object NewValue)
		{
			((SvgButton)Bindable).innerSvg.TintColor = (Color)NewValue;
		}

		public static void OnLabelTextChanged(BindableObject Bindable, object OldValue, object NewValue)
		{
			((SvgButton)Bindable).innerLabel.Text = (string)NewValue;
		}

		public static void OnLabelStyleChanged(BindableObject Bindable, object OldValue, object NewValue)
		{
			((SvgButton)Bindable).innerLabel.Style = (Style)NewValue;
		}

		public static void OnLabelPositionChanged(BindableObject Bindable, object OldValue, object NewValue)
		{
			SvgButton Button = (SvgButton)Bindable;
			if (Button.LabelText == string.Empty) return;

			LabelPosition NewPosition = (LabelPosition)NewValue;
			switch (NewPosition)
			{
				case LabelPosition.Top:
					Button.innerBorder.Content = new StackLayout
					{
						Orientation = StackOrientation.Vertical,
						Children =
						{
							Button.innerLabel,
							Button.innerSvg
						},
						Spacing = 8
					};
					break;
				case LabelPosition.Bottom:
					Button.innerBorder.Content = new StackLayout
					{
						Orientation = StackOrientation.Vertical,
						Children =
						{
							Button.innerSvg,
							Button.innerLabel
						},
						Spacing = 8
					};
					break;
				case LabelPosition.Left:
					Button.innerBorder.Content = new StackLayout
					{
						Orientation = StackOrientation.Horizontal,
						Children =
						{
							Button.innerLabel,
							Button.innerSvg
						},
						Spacing = 8
					};
					break;
				case LabelPosition.Right:
					Button.innerBorder.Content = new StackLayout
					{
						Orientation = StackOrientation.Horizontal,
						Children =
						{
							Button.innerSvg,
							Button.innerLabel
						},
						Spacing = 8
					};
					break;
			}
		}

		public static void OnBorderStyleChanged(BindableObject Bindable, object OldValue, object NewValue)
		{
			((SvgButton)Bindable).innerBorder.Style = (Style)NewValue;
		}

		public string SvgSource
		{
			get => (string)this.GetValue(SvgSourceProperty);
			set => this.SetValue(SvgSourceProperty, value);
		}

		public Aspect SvgAspect
		{
			get => (Aspect)this.GetValue(SvgAspectProperty);
			set => this.SetValue(SvgAspectProperty, value);
		}

		public Color SvgTintColor
		{
			get => (Color)this.GetValue(SvgTintColorProperty);
			set => this.SetValue(SvgTintColorProperty, value);
		}

		public string LabelText
		{
			get => (string)this.GetValue(LabelTextProperty);
			set => this.SetValue(LabelTextProperty, value);
		}

		public Style LabelStyle
		{
			get => (Style)this.GetValue(LabelStyleProperty);
			set => this.SetValue(LabelStyleProperty, value);
		}

		public LabelPosition LabelPosition
		{
			get => (LabelPosition)this.GetValue(LabelPositionProperty);
			set => this.SetValue(LabelPositionProperty, value);
		}

		public Style BorderStyle
		{
			get => (Style)this.GetValue(BorderDataElement.BorderStyleProperty);
			set => this.SetValue(BorderDataElement.BorderStyleProperty, value);
		}

		public SvgButton()
			: base()
		{
			this.innerSvg = new()
			{
				Aspect = this.SvgAspect,
				WidthRequest = 24,
				HeightRequest = 24,
				TintColor = this.SvgTintColor,
				Source = this.SvgSource
			};
			this.innerBorder = new()
			{
				Style = this.BorderStyle
			};
			this.innerLabel = new()
			{
				Text = this.LabelText,
				Style = this.LabelStyle
			};

			switch (this.LabelPosition)
			{
				case LabelPosition.Top:
					this.innerBorder.Content = new StackLayout
					{
						Orientation = StackOrientation.Vertical,
						Children =
						{
							this.innerLabel,
							this.innerSvg
						},
						Spacing = 8
					};
					break;
				case LabelPosition.Bottom:
					this.innerBorder.Content = new StackLayout
					{
						Orientation = StackOrientation.Vertical,
						Children =
						{
							this.innerSvg,
							this.innerLabel
						},
						Spacing = 8
					};
					break;
				case LabelPosition.Left:
					this.innerBorder.Content = new StackLayout
					{
						Orientation = StackOrientation.Horizontal,
						Children =
						{
							this.innerLabel,
							this.innerSvg
						},
						Spacing = 8
					};
					break;
				case LabelPosition.Right:
					this.innerBorder.Content = new StackLayout
					{
						Orientation = StackOrientation.Horizontal,
						Children =
						{
							this.innerSvg,
							this.innerLabel
						},
						Spacing = 8
					};
					break;
			}

			this.Content = this.innerBorder;
		}

		/// <summary>
		/// Releases all resources used by the <see cref="SvgView"/> control.
		/// </summary>
		public void Dispose()
		{
			this.innerSvg.Dispose();
			GC.SuppressFinalize(this);
		}
	}

	public enum LabelPosition
	{
		Top,
		Bottom,
		Left,
		Right
	}
}
