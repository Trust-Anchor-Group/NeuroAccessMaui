using CommunityToolkit.Maui.Markup;
using NeuroAccessMaui.UI.Core;
using Waher.Script.Functions.Strings;

namespace NeuroAccessMaui.UI.Controls
{
	class SvgButton : TemplatedButton, IDisposable
	{
		private readonly SvgView innerSvg;
		private readonly Label innerLabel;
		private readonly Border innerBorder;
		private readonly Grid innerGrid;

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

		/// <summary>Bindable property for <see cref="SvgSource"/>.</summary>
		public static readonly BindableProperty SvgStyleProperty = BindableProperty.Create(
			nameof(SvgStyle),
			typeof(Style),
			typeof(SvgButton),
			propertyChanged: OnSvgStyleChanged);

		public static readonly BindableProperty LabelTextProperty = BindableProperty.Create(
			nameof(LabelText),
			typeof(string),
			typeof(SvgButton),
			propertyChanged: OnLabelTextChanged);

		public static readonly BindableProperty LabelStyleProperty = BindableProperty.Create(
			nameof(LabelStyle),
			typeof(Style),
			typeof(SvgButton),
			propertyChanged: OnLabelStyleChanged);

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

		public static void OnSvgStyleChanged(BindableObject Bindable, object OldValue, object NewValue)
		{
			((SvgButton)Bindable).innerSvg.Style = (Style)NewValue;
		}

		public static void OnLabelTextChanged(BindableObject Bindable, object OldValue, object NewValue)
		{
			SvgButton Button = (SvgButton)Bindable;
			Button.innerLabel.Text = (string)NewValue;

			if (string.IsNullOrEmpty(Button.innerLabel.Text))
			{
				Button.innerGrid.RowSpacing = 0;
				Button.innerGrid.ColumnSpacing = 0;
				Button.innerLabel.IsVisible = false;
			}
			else
			{
				Button.innerLabel.IsVisible = true;
				switch (Button.LabelPosition)
				{
					case LabelPosition.Top:
						Button.innerLabel.Row(0);
						Button.innerLabel.Column(0);
						Button.innerSvg.Row(1);
						Button.innerSvg.Column(0);
						Button.innerGrid.ColumnSpacing = 0;
						Button.innerGrid.RowSpacing = 8;
						break;
					case LabelPosition.Bottom:
						Button.innerLabel.Row(1);
						Button.innerLabel.Column(0);
						Button.innerSvg.Row(0);
						Button.innerSvg.Column(0);
						Button.innerGrid.ColumnSpacing = 0;
						Button.innerGrid.RowSpacing = 8;
						break;
					case LabelPosition.Left:
						Button.innerLabel.Column(0);
						Button.innerLabel.Row(0);
						Button.innerSvg.Column(1);
						Button.innerSvg.Row(0);
						Button.innerGrid.ColumnSpacing = 8;
						Button.innerGrid.RowSpacing = 0;
						break;
					case LabelPosition.Right:
						Button.innerLabel.Column(1);
						Button.innerLabel.Row(0);
						Button.innerSvg.Column(0);
						Button.innerSvg.Row(0);
						Button.innerGrid.ColumnSpacing = 8;
						Button.innerGrid.RowSpacing = 0;
						break;
				}
			}
		}

		public static void OnLabelStyleChanged(BindableObject Bindable, object OldValue, object NewValue)
		{
			((SvgButton)Bindable).innerLabel.Style = (Style)NewValue;
		}

		public static void OnLabelPositionChanged(BindableObject Bindable, object OldValue, object NewValue)
		{
			SvgButton Button = (SvgButton)Bindable;

			LabelPosition NewPosition = (LabelPosition)NewValue;
			Button.LabelPosition = NewPosition;

			switch (NewPosition)
			{
				case LabelPosition.Top:
					Button.innerLabel.Row(0);
					Button.innerLabel.Column(0);
					Button.innerSvg.Row(1);
					Button.innerSvg.Column(0);
					Button.innerGrid.ColumnSpacing = 0;
					Button.innerGrid.RowSpacing = 8;
					break;
				case LabelPosition.Bottom:
					Button.innerLabel.Row(1);
					Button.innerLabel.Column(0);
					Button.innerSvg.Row(0);
					Button.innerSvg.Column(0);
					Button.innerGrid.ColumnSpacing = 0;
					Button.innerGrid.RowSpacing = 8;
					break;
				case LabelPosition.Left:
					Button.innerLabel.Column(0);
					Button.innerLabel.Row(0);
					Button.innerSvg.Column(1);
					Button.innerSvg.Row(0);
					Button.innerGrid.ColumnSpacing = 8;
					Button.innerGrid.RowSpacing = 0;
					break;
				case LabelPosition.Right:
					Button.innerLabel.Column(1);
					Button.innerLabel.Row(0);
					Button.innerSvg.Column(0);
					Button.innerSvg.Row(0);
					Button.innerGrid.ColumnSpacing = 8;
					Button.innerGrid.RowSpacing = 0;
					break;
			}

			if (string.IsNullOrEmpty(Button.LabelText))
			{
				Button.innerGrid.ColumnSpacing = 0;
				Button.innerGrid.RowSpacing = 0;
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

		public Style SvgStyle
		{
			get => (Style)this.GetValue(SvgStyleProperty);
			set => this.SetValue(SvgStyleProperty, value);
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
				Style = this.SvgStyle,
				TintColor = this.SvgTintColor,
				Source = this.SvgSource
			};
			this.innerLabel = new()
			{
				Text = this.LabelText,
				Style = this.LabelStyle
			};
			this.innerGrid = new()
			{
				ColumnDefinitions =
				{
					new ColumnDefinition(width: GridLength.Auto),
					new ColumnDefinition(width: GridLength.Auto)
				},
				RowDefinitions =
				{
					new RowDefinition(height: GridLength.Auto),
					new RowDefinition(height: GridLength.Auto)
				},
				HorizontalOptions = LayoutOptions.Center,
				VerticalOptions = LayoutOptions.Center
			};

			this.innerGrid.Add(this.innerLabel);
			this.innerGrid.Add(this.innerSvg);

			if (string.IsNullOrEmpty(this.LabelText))
			{
				this.innerGrid.RowSpacing = 0;
				this.innerGrid.ColumnSpacing = 0;
				this.innerLabel.IsVisible = false;
			}
			else
			{
				switch (this.LabelPosition)
				{
					case LabelPosition.Top:
						this.innerLabel.Row(0);
						this.innerLabel.Column(0);
						this.innerSvg.Row(1);
						this.innerSvg.Column(0);
						this.innerGrid.ColumnSpacing = 0;
						this.innerGrid.RowSpacing = 8;
						break;
					case LabelPosition.Bottom:
						this.innerLabel.Row(1);
						this.innerLabel.Column(0);
						this.innerSvg.Row(0);
						this.innerSvg.Column(0);
						this.innerGrid.ColumnSpacing = 0;
						this.innerGrid.RowSpacing = 8;
						break;
					case LabelPosition.Left:
						this.innerLabel.Column(0);
						this.innerLabel.Row(0);
						this.innerSvg.Column(1);
						this.innerSvg.Row(0);
						this.innerGrid.ColumnSpacing = 8;
						this.innerGrid.RowSpacing = 0;
						break;
					case LabelPosition.Right:
						this.innerLabel.Column(1);
						this.innerLabel.Row(0);
						this.innerSvg.Column(0);
						this.innerSvg.Row(0);
						this.innerGrid.ColumnSpacing = 8;
						this.innerGrid.RowSpacing = 0;
						break;
				}
			}

			this.innerBorder = new()
			{
				Style = this.BorderStyle,
				Content = this.innerGrid
			};

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
