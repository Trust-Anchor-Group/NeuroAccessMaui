using System.ComponentModel;
using CommunityToolkit.Maui.Converters;
using Microsoft.Maui.Controls.Shapes;
using Path = Microsoft.Maui.Controls.Shapes.Path;
using FormatedString = Microsoft.Maui.Controls.FormattedString;
using Microsoft.Maui.Graphics.Text;
using Waher.Networking.XMPP.Contracts.HumanReadable.InlineElements;
using NeuroAccessMaui.Resources.Languages;
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
			Grid MainGrid = new Grid
			{
				RowDefinitions =
				{
					new RowDefinition { Height = GridLength.Auto }, // Label
					new RowDefinition { Height = GridLength.Auto }, // Left, Center, Right Views
					new RowDefinition { Height = GridLength.Auto }  // Validation Icon and Text
				}
			};

			MainGrid.RowSpacing = 2;

			// Row 1: Single Label with FormattedText
			this.label = new Label();
			FormattedString Formatted = new FormattedString();

			// Span for the main label text
			Span LabelSpan = new Span();
			LabelSpan.SetBinding(Span.TextProperty, new Binding(nameof(this.LabelText), source: this));

			// Inherit styling from the owner label
			LabelSpan.SetBinding(Span.TextColorProperty, new Binding(nameof(this.label.TextColor), source: this.label));
			LabelSpan.SetBinding(Span.FontSizeProperty, new Binding(nameof(this.label.FontSize), source: this.label));
			LabelSpan.SetBinding(Span.FontFamilyProperty, new Binding(nameof(this.label.FontFamily), source: this.label));
			LabelSpan.SetBinding(Span.FontAttributesProperty, new Binding(nameof(this.label.FontAttributes), source: this.label));
			LabelSpan.SetBinding(Span.TextDecorationsProperty, new Binding(nameof(this.label.TextDecorations), source: this.label));
			LabelSpan.SetBinding(Span.CharacterSpacingProperty, new Binding(nameof(this.label.CharacterSpacing), source: this.label));
			LabelSpan.SetBinding(Span.LineHeightProperty, new Binding(nameof(this.label.LineHeight), source: this.label));
			LabelSpan.SetBinding(Span.TextTransformProperty, new Binding(nameof(this.label.TextTransform), source: this.label));
			LabelSpan.SetBinding(Span.BackgroundColorProperty, new Binding(nameof(this.label.BackgroundColor), source: this.label));
			LabelSpan.SetBinding(Span.FontAutoScalingEnabledProperty, new Binding(nameof(this.label.FontAutoScalingEnabled), source: this.label));

			Formatted.Spans.Add(LabelSpan);

			// Span for the required marker
			Span RequiredMarkerSpan = new Span();
			RequiredMarkerSpan.SetBinding(Span.TextProperty, new Binding(nameof(this.RequiredMarker), source: this));
			RequiredMarkerSpan.Style = AppStyles.RequiredFieldMarkerSpan;
			Formatted.Spans.Add(RequiredMarkerSpan);

			this.label.FormattedText = Formatted;
			this.label.SetBinding(Label.StyleProperty, new Binding(nameof(this.LabelStyle), source: this));
			this.label.SetBinding(Label.IsVisibleProperty, new Binding(nameof(this.CanShowLabel), source: this));

			MainGrid.Add(this.label, 0, 0);

			// Row 2: LeftView, CenterView, RightView
			Grid ContentGrid = new Grid
			{
				ColumnDefinitions =
				{
					new ColumnDefinition { Width = GridLength.Auto }, // LeftView
					new ColumnDefinition { Width = GridLength.Star }, // CenterView
					new ColumnDefinition { Width = GridLength.Auto }  // RightView
				},
				ColumnSpacing = 0,
				Margin = 0
			};

			// LeftView
			this.leftContentView = new ContentView();
			this.leftContentView.SetBinding(ContentView.ContentProperty, new Binding(nameof(this.LeftView), source: this));
			ContentGrid.Add(this.leftContentView, 0, 0);

			// CenterView
			this.centerContentView = new ContentView();
			this.centerContentView.SetBinding(ContentView.ContentProperty, new Binding(nameof(this.CenterView), source: this));
			ContentGrid.Add(this.centerContentView, 1, 0);

			// RightView
			this.rightContentView = new ContentView();
			this.rightContentView.SetBinding(ContentView.ContentProperty, new Binding(nameof(this.RightView), source: this));
			ContentGrid.Add(this.rightContentView, 2, 0);

			this.border = new Border()
			{
				Content = ContentGrid,
				StrokeThickness = 0.5f,
				HorizontalOptions = LayoutOptions.Fill,
				Margin = 0
			};
			this.border.PropertyChanged += this.OnBorderPropertyChanged;
			this.border.SetBinding(Border.StrokeProperty, new Binding(nameof(this.BorderStroke), source: this));
			this.border.SetBinding(Border.StrokeShapeProperty, new Binding(nameof(this.BorderStrokeShape), source: this));
			this.border.SetBinding(Border.BackgroundColorProperty, new Binding(nameof(this.BorderBackground), source: this));
			this.border.SetBinding(Border.PaddingProperty, new Binding(nameof(this.BorderPadding), source: this));
			MainGrid.Add(this.border, 0, 1);

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
			Path ValidationIcon = new();
			ValidationIcon.SetBinding(Path.DataProperty, new Binding(nameof(this.ValidationIcon), source: this));
			ValidationIcon.SetBinding(Path.FillProperty, new Binding(nameof(this.ValidationColor), source: this));
			ValidationIcon.VerticalOptions = LayoutOptions.Center;
			this.validationGrid.Add(ValidationIcon, 0, 0);

			// ValidationText
			Label ValidationLabel = new Label();
			ValidationLabel.SetBinding(Label.TextProperty, new Binding(nameof(this.ValidationText), source: this));
			ValidationLabel.SetBinding(Label.TextColorProperty, new Binding(nameof(this.ValidationColor), source: this));
			this.validationGrid.Add(ValidationLabel, 1, 0);

			this.validationGrid.SetBinding(Grid.IsVisibleProperty, new Binding(nameof(this.CanShowValidation), source: this));

			this.validationGrid.HorizontalOptions = LayoutOptions.Center;
			this.validationGrid.Margin = AppStyles.SmallTopMargins;
			this.validationGrid.ColumnSpacing = AppStyles.SmallSpacing;

			MainGrid.Add(this.validationGrid, 0, 2);
			// Set the Content of the ContentView to the MainGrid
			this.Content = MainGrid;
		}

		#region Bindable Properties

		public static readonly BindableProperty RequiredProperty =
			  BindableProperty.Create(
					 nameof(Required),
					 typeof(bool),
					 typeof(CompositeInputView),
					 false,
					 propertyChanged: OnRequiredChanged);

		public bool Required
		{
			get => (bool)this.GetValue(RequiredProperty);
			set => this.SetValue(RequiredProperty, value);
		}

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
			 BindableProperty.Create(nameof(LeftView), typeof(View), typeof(CompositeInputView), null, propertyChanged: OnLeftViewChanged);

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
			 BindableProperty.Create(nameof(RightView), typeof(View), typeof(CompositeInputView), null, propertyChanged: OnRightViewChanged);

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
		public Style ValidationLabelStyle
		{
			get => (Style)this.GetValue(ValidationLabelStyleProperty);
			set => this.SetValue(ValidationLabelStyleProperty, value);
		}

		/// <summary>
		/// Bindable property for the style applied to the validation icon.
		/// </summary>
		public static readonly BindableProperty ValidationIconStyleProperty =
			 BindableProperty.Create(nameof(ValidationIconStyle), typeof(Style), typeof(CompositeInputView));

		/// <summary>
		/// Gets or sets the style applied to the validation icon.
		/// </summary>
		public Style ValidationIconStyle
		{
			get => (Style)this.GetValue(ValidationIconStyleProperty);
			set => this.SetValue(ValidationIconStyleProperty, value);
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
		public string RequiredMarker => this.Required ? "*" : string.Empty;


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
				  typeof(Shape),
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

		private static void OnRequiredChanged(BindableObject Bindable, object OldValue, object NewValue)
		{
			CompositeInputView Control = (CompositeInputView)Bindable;
			Control.OnPropertyChanged(nameof(RequiredMarker));
		}

		private void OnBorderPropertyChanged(Object? Sender, PropertyChangedEventArgs E)
		{

		}

		private static void OnValidationChanged(BindableObject Bindable, object OldValue, object NewValue)
		{
			CompositeInputView Control = (CompositeInputView)Bindable;

			// Update CanShowValidation
			Control.OnPropertyChanged(nameof(CanShowValidation));
			// Nudge layout to ensure style triggers reflect immediately even if control isn't focused
			Control.InvalidateMeasure();
		}

		private static void OnLabelChanged(BindableObject Bindable, object OldValue, object NewValue)
		{
			CompositeInputView Control = (CompositeInputView)Bindable;
			Control.OnPropertyChanged(nameof(CanShowLabel));
		}

		private static void OnLeftViewChanged(BindableObject Bindable, object OldValue, object NewValue)
		{
			CompositeInputView Control = (CompositeInputView)Bindable;
			bool HasLeft = NewValue is View;
			// Apply spacing only when left view exists
			Control.leftContentView.Margin = HasLeft ? new Thickness(0, 0, 8, 0) : new Thickness(0);
		}

		private static void OnRightViewChanged(BindableObject Bindable, object OldValue, object NewValue)
		{
			CompositeInputView Control = (CompositeInputView)Bindable;
			bool HasRight = NewValue is View;
			// Apply spacing only when right view exists
			Control.rightContentView.Margin = HasRight ? new Thickness(8, 0, 0, 0) : new Thickness(0);
		}
	}
}
