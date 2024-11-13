using System.ComponentModel;
using CommunityToolkit.Maui.Converters;
using Microsoft.Maui.Controls.Shapes;
using Path = Microsoft.Maui.Controls.Shapes.Path;
namespace NeuroAccessMaui.UI.Controls
{

	public partial class CompositeInputView : ContentView
	{

		#region Fields
		// UI Elements
		protected Label label;
		protected ContentView leftContentView;
		protected ContentView centerContentView;
		protected ContentView rightContentView;
		protected Grid validationGrid;
		protected Border border;

		public bool CanShowValidation => !this.IsValid && !string.IsNullOrEmpty(this.ValidationText);
		public bool CanShowLabel => !string.IsNullOrEmpty(this.LabelText);
		#endregion

		public CompositeInputView()
		{
			// Main Grid with 3 rows
			Grid mainGrid = new Grid
			{
				RowDefinitions =
				{
					new RowDefinition { Height = GridLength.Auto }, // Label
					new RowDefinition { Height = GridLength.Auto }, // Left, Center, Right Views
					new RowDefinition { Height = GridLength.Auto }  // Validation Icon and Text
				}
			};

			// Row 1: Label
			this.label = new Label();
			this.label.SetBinding(Label.TextProperty, new Binding(nameof(this.LabelText), source: this));
			this.label.SetBinding(Label.StyleProperty, new Binding(nameof(this.LabelStyle), source: this));
			this.label.SetBinding(Label.IsVisibleProperty, new Binding(nameof(this.CanShowLabel), source: this));
			mainGrid.Add(this.label, 0, 0);

			// Row 2: LeftView, CenterView, RightView
			Grid contentGrid = new Grid
			{
				ColumnDefinitions =
				{
					new ColumnDefinition { Width = GridLength.Auto }, // LeftView
					new ColumnDefinition { Width = GridLength.Star }, // CenterView
					new ColumnDefinition { Width = GridLength.Auto }  // RightView
				},
				ColumnSpacing = AppStyles.SmallSpacing
			};

			// LeftView
			this.leftContentView = new ContentView();
			this.leftContentView.SetBinding(ContentView.ContentProperty, new Binding(nameof(this.LeftView), source: this));
			contentGrid.Add(this.leftContentView, 0, 0);

			// CenterView
			this.centerContentView = new ContentView();
			this.centerContentView.SetBinding(ContentView.ContentProperty, new Binding(nameof(this.CenterView), source: this));
			contentGrid.Add(this.centerContentView, 1, 0);

			// RightView
			this.rightContentView = new ContentView();
			this.rightContentView.SetBinding(ContentView.ContentProperty, new Binding(nameof(this.RightView), source: this));
			contentGrid.Add(this.rightContentView, 2, 0);

			this.border = new Border()
			{
				Content = contentGrid
			};
			this.border.PropertyChanged += this.OnBorderPropertyChanged;
			this.border.SetBinding(Border.StrokeProperty, new Binding(nameof(this.BorderStroke), source: this));
			this.border.SetBinding(Border.StrokeShapeProperty, new Binding(nameof(this.BorderStrokeShape), source: this));
			this.border.SetBinding(Border.ShadowProperty, new Binding(nameof(this.BorderShadow), source: this));
			this.border.SetBinding(Border.BackgroundColorProperty, new Binding(nameof(this.BorderBackground), source: this));
			this.border.SetBinding(Border.PaddingProperty, new Binding(nameof(this.BorderPadding), source: this));
			mainGrid.Add(this.border, 0, 1);

			// Row 3: ValidationIcon and ValidationText
			this.validationGrid = new Grid
			{
				ColumnDefinitions =
				{
					new ColumnDefinition { Width = GridLength.Auto }, // ValidationIcon
					new ColumnDefinition { Width = GridLength.Star }  // ValidationText
				}
			};

			// ValidationIcon
			Path validationIcon = new();
			validationIcon.SetBinding(Path.DataProperty, new Binding(nameof(this.ValidationIcon), source: this));
			validationIcon.SetBinding(Path.FillProperty, new Binding(nameof(this.ValidationColor), source: this));
			validationIcon.VerticalOptions = LayoutOptions.Center;
			this.validationGrid.Add(validationIcon, 0, 0);

			// ValidationText
			Label validationLabel = new Label();
			validationLabel.SetBinding(Label.TextProperty, new Binding(nameof(this.ValidationText), source: this));
			validationLabel.SetBinding(Label.TextColorProperty, new Binding(nameof(this.ValidationColor), source: this));
			this.validationGrid.Add(validationLabel, 1, 0);

			this.validationGrid.SetBinding(Grid.IsVisibleProperty, new Binding(nameof(this.CanShowValidation), source: this));

			this.validationGrid.HorizontalOptions = LayoutOptions.Center;
			this.validationGrid.Margin = AppStyles.SmallTopMargins;
			this.validationGrid.ColumnSpacing = AppStyles.SmallSpacing;

			mainGrid.Add(this.validationGrid, 0, 2);
			// Set the Content of the ContentView to the mainGrid
			this.Content = mainGrid;
		}

		#region Bindable Properties
		// LabelText Property
		public static readonly BindableProperty LabelTextProperty =
			 BindableProperty.Create(nameof(LabelText), typeof(string), typeof(CompositeInputView), string.Empty, propertyChanged: OnLabelChanged);

		public string LabelText
		{
			get => (string)this.GetValue(LabelTextProperty);
			set => this.SetValue(LabelTextProperty, value);
		}

		// LeftView Property
		public static readonly BindableProperty LeftViewProperty =
			 BindableProperty.Create(nameof(LeftView), typeof(View), typeof(CompositeInputView), null);

		public View LeftView
		{
			get => (View)this.GetValue(LeftViewProperty);
			set => this.SetValue(LeftViewProperty, value);
		}

		// CenterView Property
		public static readonly BindableProperty CenterViewProperty =
			 BindableProperty.Create(nameof(CenterView), typeof(View), typeof(CompositeInputView), null);

		public View CenterView
		{
			get => (View)this.GetValue(CenterViewProperty);
			set => this.SetValue(CenterViewProperty, value);
		}

		// RightView Property
		public static readonly BindableProperty RightViewProperty =
			 BindableProperty.Create(nameof(RightView), typeof(View), typeof(CompositeInputView), null);

		public View RightView
		{
			get => (View)this.GetValue(RightViewProperty);
			set => this.SetValue(RightViewProperty, value);
		}

		// ValidationIcon Property
		public static readonly BindableProperty ValidationIconProperty =
			 BindableProperty.Create(nameof(ValidationIcon), typeof(Geometry), typeof(CompositeInputView), Geometries.ErrorPath);

		public Geometry ValidationIcon
		{
			get => (Geometry)this.GetValue(ValidationIconProperty);
			set => this.SetValue(ValidationIconProperty, value);
		}

		// ValidationText Property
		public static readonly BindableProperty ValidationTextProperty =
			 BindableProperty.Create(nameof(ValidationText), typeof(string), typeof(CompositeInputView), string.Empty, propertyChanged: OnValidationChanged);

		public string ValidationText
		{
			get => (string)this.GetValue(ValidationTextProperty);
			set => this.SetValue(ValidationTextProperty, value);
		}

		/// <summary>
		/// Bindable property for the style applied to the validation label.
		/// </summary>
		public static readonly BindableProperty ValidationLabelStyleProperty =
			BindableProperty.Create(nameof(ValidationLabelStyle), typeof(Style), typeof(CompositeInputView));

		/// <summary>
		/// Gets or sets the style applied to the validation label.
		/// </summary>
		public Style ValidationIconStyle
		{
			get => (Style)this.GetValue(ValidationIconStyleProperty);
			set => this.SetValue(ValidationIconStyleProperty, value);
		}

		/// <summary>
		/// Bindable property for the style applied to the label above the entry.
		/// </summary>
		public static readonly BindableProperty ValidationIconStyleProperty =
			BindableProperty.Create(nameof(ValidationIconStyle), typeof(Style), typeof(CompositeInputView));

		/// <summary>
		/// Gets or sets the style applied to the label above the entry.
		/// </summary>
		public Style ValidationLabelStyle
		{
			get => (Style)this.GetValue(ValidationLabelStyleProperty);
			set => this.SetValue(ValidationLabelStyleProperty, value);
		}

		// IsValid Property
		public static readonly BindableProperty IsValidProperty =
			 BindableProperty.Create(nameof(IsValid), typeof(bool), typeof(CompositeInputView), true, propertyChanged: OnValidationChanged);

		public bool IsValid
		{
			get => (bool)this.GetValue(IsValidProperty);
			set => this.SetValue(IsValidProperty, value);
		}

		/// <summary>
		/// Bindable property for the style applied to the label above the entry.
		/// </summary>
		public static readonly BindableProperty LabelStyleProperty =
			BindableProperty.Create(nameof(LabelStyle), typeof(Style), typeof(CompositeInputView));

		/// <summary>
		/// Gets or sets the style applied to the label above the entry.
		/// </summary>
		public Style LabelStyle
		{
			get => (Style)this.GetValue(LabelStyleProperty);
			set => this.SetValue(LabelStyleProperty, value);
		}

		public static readonly BindableProperty ValidationColorProperty =
			BindableProperty.Create(nameof(ValidationColor), typeof(Color), typeof(CompositeInputView), AppColors.ErrorBackground);

		public Color ValidationColor
		{
			get => (Color)this.GetValue(ValidationColorProperty);
			set => this.SetValue(ValidationColorProperty, value);
		}



		#endregion

		#region Border Bindable Properties

		//Border Background Color
		public static readonly BindableProperty BorderBackgroundProperty =
			 BindableProperty.Create(
				  nameof(BorderBackground),
				  typeof(Color),
				  typeof(CompositeInputView),
				  Colors.Transparent);
		public Color BorderBackground { get => (Color)this.GetValue(BorderBackgroundProperty); set => this.SetValue(BorderBackgroundProperty, value); }

		// Border Stroke Color
		public static readonly BindableProperty BorderStrokeProperty =
			 BindableProperty.Create(
				  nameof(BorderStroke),
				  typeof(Color),
				  typeof(CompositeInputView),
				  Colors.Gray);

		public Color BorderStroke
		{
			get => (Color)this.GetValue(BorderStrokeProperty);
			set => this.SetValue(BorderStrokeProperty, value);
		}

		// Border Stroke Shape
		public static readonly BindableProperty BorderStrokeShapeProperty =
			 BindableProperty.Create(
				  nameof(BorderStrokeShape),
				  typeof(IShape),
				  typeof(CompositeInputView),
				  new Rectangle());
		public Shape BorderStrokeShape
		{
			get => (Shape)this.GetValue(BorderStrokeShapeProperty);
			set => this.SetValue(BorderStrokeShapeProperty, value);
		}

		// Border Shadow
		public static readonly BindableProperty BorderShadowProperty =
			 BindableProperty.Create(
				  nameof(BorderShadow),
				  typeof(Shadow),
				  typeof(CompositeInputView),
				  new Shadow());
		public Shadow BorderShadow
		{
			get => (Shadow)this.GetValue(BorderShadowProperty);
			set => this.SetValue(BorderShadowProperty, value);
		}

		// Border Padding
		public static readonly BindableProperty BorderPaddingProperty =
			 BindableProperty.Create(
				  nameof(BorderPadding),
				  typeof(Thickness),
				  typeof(CompositeInputView),
				  new Thickness(0));

		public Thickness BorderPadding
		{
			get => (Thickness)this.GetValue(BorderPaddingProperty);
			set => this.SetValue(BorderPaddingProperty, value);
		}

		// Border Corner Radius
		public static readonly BindableProperty BorderCornerRadiusProperty =
			 BindableProperty.Create(
				  nameof(BorderCornerRadius),
				  typeof(CornerRadius),
				  typeof(CompositeInputView),
				  new CornerRadius(5));

		public CornerRadius BorderCornerRadius
		{
			get => (CornerRadius)this.GetValue(BorderCornerRadiusProperty);
			set => this.SetValue(BorderCornerRadiusProperty, value);
		}

		#endregion

		private void OnBorderPropertyChanged(Object? sender, PropertyChangedEventArgs e)
		{
			// WORKAROUND FOR ClIPPED SHADOW in certain views
			if (e.PropertyName == nameof(Border.Shadow))
			{
				if (this.BorderShadow is null)
					return;

				double blurRadius = this.BorderShadow.Radius;
				double offsetX = this.BorderShadow?.Offset.X ?? 0;
				double offsetY = this.BorderShadow?.Offset.Y ?? 0;

				// Calculate padding for each side
				double paddingLeft = blurRadius + (offsetX < 0 ? Math.Abs(offsetX) : 0);
				double paddingRight = blurRadius + (offsetX > 0 ? offsetX : 0);
				double paddingTop = blurRadius + (offsetY < 0 ? Math.Abs(offsetY) : 0);
				double paddingBottom = blurRadius + (offsetY > 0 ? offsetY : 0);

				// Apply the calculated padding
				this.border.Margin = new Thickness(paddingLeft, paddingTop, paddingRight, paddingBottom);
			}
		}
		private static void OnValidationChanged(BindableObject bindable, object oldValue, object newValue)
		{
			CompositeInputView control = (CompositeInputView)bindable;

			// Update CanShowValidation
			control.OnPropertyChanged(nameof(CanShowValidation));

		}

		private static void OnLabelChanged(BindableObject bindable, object oldValue, object newValue)
		{
			CompositeInputView control = (CompositeInputView)bindable;
			control.OnPropertyChanged(nameof(CanShowLabel));
		}

	}
}
