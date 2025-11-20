using System.Windows.Input;

namespace NeuroAccessMaui.UI.Controls
{
	internal class BottomBar : ContentView, IDisposable
	{
		#region Fields

		private readonly SvgView leftIcon;
		private readonly SvgView centerIcon;
		private readonly SvgView rightIcon;
		private readonly Border border;
		private readonly Border leftInnerBorder;
		private readonly Border centerInnerBorder;
		private readonly Border rightInnerBorder;
		private readonly Label leftLabel;
		private readonly Label centerLabel;
	    private readonly Label rightLabel;

		#endregion

		#region Constructor
		public BottomBar()
		{
			// LeftIcon, CenterIcon, RightIcon
			Grid ContentGrid = new Grid
			{
				ColumnDefinitions =
				{
					new ColumnDefinition { Width = GridLength.Star }, // LeftIcon
					new ColumnDefinition { Width = GridLength.Star }, // CenterIcon
					new ColumnDefinition { Width = GridLength.Star }  // RightIcon
				},
				Margin = 0
			};

			// Main Border
			this.border = new();
			this.border.Content = ContentGrid;
			this.border.SetBinding(Border.StyleProperty, new Binding(nameof(this.BorderStyle), source: this));

			// LeftIcon
			this.leftIcon = new SvgView();
			this.leftIcon.VerticalOptions = LayoutOptions.Center;
			this.leftIcon.SetBinding(SvgView.SourceProperty, new Binding(nameof(this.LeftIcon), source: this));
			this.leftInnerBorder = new();
			this.leftInnerBorder.Content = this.leftIcon;

			this.leftLabel = new();
			this.leftLabel.SetBinding(Label.TextProperty, new Binding(nameof(this.LeftLabelText), source: this));

			TemplatedButton LeftButton = new()
			{
				Content = new VerticalStackLayout
				{
					Children =
					{
						this.leftInnerBorder,
						this.leftLabel
					}
				}
			};
			LeftButton.SetBinding(TemplatedButton.CommandProperty, new Binding(nameof(this.LeftCommand), source: this));

			ContentGrid.Add(LeftButton, 0, 0);

			// CenterIcon
			this.centerIcon = new SvgView();
			this.centerIcon.VerticalOptions = LayoutOptions.Center;
			this.centerIcon.SetBinding(SvgView.SourceProperty, new Binding(nameof(this.CenterIcon), source: this));
			this.centerInnerBorder = new();
			this.centerInnerBorder.Content = this.centerIcon;

			this.centerLabel = new();
			this.centerLabel.SetBinding(Label.TextProperty, new Binding(nameof(this.CenterLabelText), source: this));

			TemplatedButton CenterButton = new()
			{
				Content = new VerticalStackLayout
				{
					Children =
					{
						this.centerInnerBorder,
						this.centerLabel
					}
				}
			};
			CenterButton.SetBinding(TemplatedButton.CommandProperty, new Binding(nameof(this.CenterCommand), source: this));

			ContentGrid.Add(CenterButton, 1, 0);

			// RightIcon
			this.rightIcon = new SvgView();
			this.rightIcon.VerticalOptions = LayoutOptions.Center;
			this.rightIcon.SetBinding(SvgView.SourceProperty, new Binding(nameof(this.RightIcon), source: this));
			this.rightInnerBorder = new();
			this.rightInnerBorder.Content = this.rightIcon;

			this.rightLabel = new();
			this.rightLabel.SetBinding(Label.TextProperty, new Binding(nameof(this.RightLabelText), source: this));

			TemplatedButton RightButton = new()
			{
				Content = new VerticalStackLayout
				{
					Children =
					{
						this.rightInnerBorder,
						this.rightLabel
					}
				}
			};
			RightButton.SetBinding(TemplatedButton.CommandProperty, new Binding(nameof(this.RightCommand), source: this));

			ContentGrid.Add(RightButton, 2, 0);

			this.VerticalOptions = LayoutOptions.End;
			this.HorizontalOptions = LayoutOptions.Fill;
			this.Content = this.border;

			this.SetBinding(ActiveIconProperty, new Binding(nameof(this.SelectedIcon), source: this));
		}
		#endregion

		#region Bindable Properties
		// LeftIcon Property
		public static readonly BindableProperty LeftIconProperty =
			BindableProperty.Create(
				nameof(LeftIcon),
				typeof(string),
				typeof(SvgView),
				null
			);

		public string LeftIcon
		{
			get => (string)this.GetValue(LeftIconProperty);
			set => this.SetValue(LeftIconProperty, value);
		}

		// CenterIcon Property
		public static readonly BindableProperty CenterIconProperty =
			BindableProperty.Create(
				nameof(CenterIcon),
				typeof(string),
				typeof(SvgView),
				null
			);

		public string CenterIcon
		{
			get => (string)this.GetValue(CenterIconProperty);
			set => this.SetValue(CenterIconProperty, value);
		}

		// RightIcon Property
		public static readonly BindableProperty RightIconProperty =
			BindableProperty.Create(
				nameof(RightIcon),
				typeof(string),
				typeof(SvgView),
				null
			);

		public string RightIcon
		{
			get => (string)this.GetValue(RightIconProperty);
			set => this.SetValue(RightIconProperty, value);
		}

		// Left label Text Property
		public static readonly BindableProperty LeftLabelTextProperty =
			BindableProperty.Create(
				nameof(LeftLabelText),
				typeof(string),
				typeof(Label),
				null
			);

		public string LeftLabelText
		{
			get => (string)this.GetValue(LeftLabelTextProperty);
			set => this.SetValue(LeftLabelTextProperty, value);
		}

		// Center label Text Property
		public static readonly BindableProperty CenterLabelTextProperty =
			BindableProperty.Create(
				nameof(CenterLabelText),
				typeof(string),
				typeof(Label),
				null
			);

		public string CenterLabelText
		{
			get => (string)this.GetValue(CenterLabelTextProperty);
			set => this.SetValue(CenterLabelTextProperty, value);
		}

		// Right label Text Property
		public static readonly BindableProperty RightLabelTextProperty =
			BindableProperty.Create(
				nameof(RightLabelText),
				typeof(string),
				typeof(Label),
				null
			);

		public string RightLabelText
		{
			get => (string)this.GetValue(RightLabelTextProperty);
			set => this.SetValue(RightLabelTextProperty, value);
		}

		// Border Style Property
		public static readonly BindableProperty BorderStyleProperty =
			BindableProperty.Create(
				nameof(BorderStyle),
				typeof(Style),
				typeof(Border),
				null
			);

		public Style BorderStyle
		{
			get => (Style)this.GetValue(BorderStyleProperty);
			set => this.SetValue(BorderStyleProperty, value);
		}

		// Selected Label Style
		public static readonly BindableProperty SelectedLabelStyleProperty =
			BindableProperty.Create(
				nameof(SelectedLabelStyle),
				typeof(Style),
				typeof(Label),
				null
			);

		public Style SelectedLabelStyle
		{
			get => (Style)this.GetValue(SelectedLabelStyleProperty);
			set => this.SetValue(SelectedLabelStyleProperty, value);
		}

		// Selected Icon Style
		public static readonly BindableProperty SelectedIconStyleProperty =
			BindableProperty.Create(
				nameof(SelectedIconStyle),
				typeof(Style),
				typeof(SvgView),
				null
			);

		public Style SelectedIconStyle
		{
			get => (Style)this.GetValue(SelectedIconStyleProperty);
			set => this.SetValue(SelectedIconStyleProperty, value);
		}

		// Unselected Label Style
		public static readonly BindableProperty UnselectedLabelStyleProperty =
			BindableProperty.Create(
				nameof(UnselectedLabelStyle),
				typeof(Style),
				typeof(Label),
				null
			);

		public Style UnselectedLabelStyle
		{
			get => (Style)this.GetValue(UnselectedLabelStyleProperty);
			set => this.SetValue(UnselectedLabelStyleProperty, value);
		}

		// Unselected Icon Style
		public static readonly BindableProperty UnselectedIconStyleProperty =
			BindableProperty.Create(
				nameof(UnselectedIconStyle),
				typeof(Style),
				typeof(SvgView),
				null
			);

		public Style UnselectedIconStyle
		{
			get => (Style)this.GetValue(UnselectedIconStyleProperty);
			set => this.SetValue(UnselectedIconStyleProperty, value);
		}

		// ActiveIcon Property
		public static readonly BindableProperty ActiveIconProperty =
			BindableProperty.Create(
				nameof(SelectedIcon),
				typeof(ActiveIcon),
				typeof(BottomBar),
				defaultValue: ActiveIcon.Default,
				propertyChanged: OnActiveIconChanged
			);

		public ActiveIcon SelectedIcon
		{
			get => (ActiveIcon)this.GetValue(ActiveIconProperty);
			set => this.SetValue(ActiveIconProperty, value);
		}

		// Selected Icon Border Style Property
		public static readonly BindableProperty SelectedIconBorderStyleProperty =
			BindableProperty.Create(
				nameof(SelectedIconBorderStyle),
				typeof(Style),
				typeof(Border),
				null
			);

		public Style SelectedIconBorderStyle
		{
			get => (Style)this.GetValue(SelectedIconBorderStyleProperty);
			set => this.SetValue(SelectedIconBorderStyleProperty, value);
		}

		// Unselected Icon Border Style Property
		public static readonly BindableProperty UnselectedIconBorderStyleProperty =
			BindableProperty.Create(
				nameof(UnselectedIconBorderStyle),
				typeof(Style),
				typeof(Border),
				null
			);

		public Style UnselectedIconBorderStyle
		{
			get => (Style)this.GetValue(UnselectedIconBorderStyleProperty);
			set => this.SetValue(UnselectedIconBorderStyleProperty, value);
		}

		// Left Button Command Property
		public static readonly BindableProperty LeftCommandProperty =
			BindableProperty.Create(
				nameof(LeftCommand),
				typeof(ICommand),
				typeof(TemplatedButton),
				null
			);

		public ICommand LeftCommand
		{
			get => (ICommand)this.GetValue(LeftCommandProperty);
			set => this.SetValue(LeftCommandProperty, value);
		}

		// Center Button Command Property
		public static readonly BindableProperty CenterCommandProperty =
			BindableProperty.Create(
				nameof(CenterCommand),
				typeof(ICommand),
				typeof(TemplatedButton),
				null
			);

		public ICommand CenterCommand
		{
			get => (ICommand)this.GetValue(CenterCommandProperty);
			set => this.SetValue(CenterCommandProperty, value);
		}

		// Right Button Command Property
		public static readonly BindableProperty RightCommandProperty =
			BindableProperty.Create(
				nameof(RightCommand),
				typeof(ICommand),
				typeof(TemplatedButton),
				null
			);

		public ICommand RightCommand
		{
			get => (ICommand)this.GetValue(RightCommandProperty);
			set => this.SetValue(RightCommandProperty, value);
		}

		#endregion

		#region OnChanged

		public static void OnActiveIconChanged(BindableObject bindable, object oldValue, object newValue)
		{
			if (bindable is BottomBar BottomBar && newValue is ActiveIcon ActiveIcon)
			{
				switch (ActiveIcon)
				{
					case ActiveIcon.Left:
						BottomBar.leftIcon.Style = BottomBar.SelectedIconStyle;
						BottomBar.centerIcon.Style = BottomBar.UnselectedIconStyle;
						BottomBar.rightIcon.Style = BottomBar.UnselectedIconStyle;

						BottomBar.leftInnerBorder.Style = BottomBar.SelectedIconBorderStyle;
						BottomBar.centerInnerBorder.Style = BottomBar.UnselectedIconBorderStyle;
						BottomBar.rightInnerBorder.Style = BottomBar.UnselectedIconBorderStyle;

						BottomBar.leftLabel.Style = BottomBar.SelectedLabelStyle;
						BottomBar.centerLabel.Style = BottomBar.UnselectedLabelStyle;
						BottomBar.rightLabel.Style = BottomBar.UnselectedLabelStyle;
						break;
					case ActiveIcon.Center:
						BottomBar.leftIcon.Style = BottomBar.UnselectedIconStyle;
						BottomBar.centerIcon.Style = BottomBar.SelectedIconStyle;
						BottomBar.rightIcon.Style = BottomBar.UnselectedIconStyle;

						BottomBar.leftInnerBorder.Style = BottomBar.UnselectedIconBorderStyle;
						BottomBar.centerInnerBorder.Style = BottomBar.SelectedIconBorderStyle;
						BottomBar.rightInnerBorder.Style = BottomBar.UnselectedIconBorderStyle;

						BottomBar.leftLabel.Style = BottomBar.UnselectedLabelStyle;
						BottomBar.centerLabel.Style = BottomBar.SelectedLabelStyle;
						BottomBar.rightLabel.Style = BottomBar.UnselectedLabelStyle;
						break;
					case ActiveIcon.Right:
						BottomBar.leftIcon.Style = BottomBar.UnselectedIconStyle;
						BottomBar.centerIcon.Style = BottomBar.UnselectedIconStyle;
						BottomBar.rightIcon.Style = BottomBar.SelectedIconStyle;

						BottomBar.leftInnerBorder.Style = BottomBar.UnselectedIconBorderStyle;
						BottomBar.centerInnerBorder.Style = BottomBar.UnselectedIconBorderStyle;
						BottomBar.rightInnerBorder.Style = BottomBar.SelectedIconBorderStyle;

						BottomBar.leftLabel.Style = BottomBar.UnselectedLabelStyle;
						BottomBar.centerLabel.Style = BottomBar.UnselectedLabelStyle;
						BottomBar.rightLabel.Style = BottomBar.SelectedLabelStyle;
						break;
				}
			}
		}

		#endregion

		#region IDisposable Implementation
		public void Dispose()
		{
			this.leftIcon.Dispose();
			this.centerIcon.Dispose();
			this.rightIcon.Dispose();
			GC.SuppressFinalize(this);
		}
		#endregion

		#region Enum

		public enum ActiveIcon
		{
			Left,
			Center,
			Right,
			Default // DO NOT USE, THIS ONLY MAKES IT SO ON SELECTED ICON CHANGED WORKS
		}

		#endregion
	}
}
