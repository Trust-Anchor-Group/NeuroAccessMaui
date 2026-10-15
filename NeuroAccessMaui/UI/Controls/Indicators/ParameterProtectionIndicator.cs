using Microsoft.Maui.Controls;
using Microsoft.Maui.Controls.Shapes;
using NeuroAccessMaui.Resources.Languages;
using NeuroAccessMaui.Services;
using Path = Microsoft.Maui.Controls.Shapes.Path;

namespace NeuroAccessMaui.UI.Controls
{
	/// <summary>
	/// A custom control that indicates parameter protection status using transient and encrypted indicators.
	/// </summary>
	public class ParameterProtectionIndicator : ContentView
	{
		/// <summary>
		/// Bindable property for controlling the visibility of the transient status indicator.
		/// </summary>
		public static readonly BindableProperty IsTransientProperty =
			 BindableProperty.Create(
				  nameof(IsTransient),
				  typeof(bool),
				  typeof(ParameterProtectionIndicator),
				  false);

		/// <summary>
		/// Bindable property for controlling the visibility of the encrypted status indicator.
		/// </summary>
		public static readonly BindableProperty IsEncryptedProperty =
			 BindableProperty.Create(
				  nameof(IsEncrypted),
				  typeof(bool),
				  typeof(ParameterProtectionIndicator),
				  false);

		/// <summary>
		/// Bindable property for the transient text.
		/// </summary>
		public static readonly BindableProperty TransientTextProperty =
			BindableProperty.Create(
				nameof(TransientText),
				typeof(string),
				typeof(ParameterProtectionIndicator),
				string.Empty,
				BindingMode.TwoWay,
				defaultValueCreator: bindable => ServiceRef.Localizer[nameof(AppResources.Transient)].ToString());

		/// <summary>
		/// Bindable property for the encrypted text.
		/// </summary>
		public static readonly BindableProperty EncryptedTextProperty =
			BindableProperty.Create(
				nameof(EncryptedText),
				typeof(string),
				typeof(ParameterProtectionIndicator),
				string.Empty,
				BindingMode.TwoWay,
				defaultValueCreator: bindable => ServiceRef.Localizer[nameof(AppResources.Encrypted)].ToString());

		/// <summary>
		/// Gets or sets a value indicating whether the transient status is visible.
		/// </summary>
		public bool IsTransient
		{
			get => (bool)this.GetValue(IsTransientProperty);
			set => this.SetValue(IsTransientProperty, value);
		}

		/// <summary>
		/// Gets or sets a value indicating whether the encrypted status is visible.
		/// </summary>
		public bool IsEncrypted
		{
			get => (bool)this.GetValue(IsEncryptedProperty);
			set => this.SetValue(IsEncryptedProperty, value);
		}

		/// <summary>
		/// Gets or sets the transient text.
		/// </summary>
		public string? TransientText
		{
			get => (string)this.GetValue(TransientTextProperty);
			set => this.SetValue(TransientTextProperty, value);
		}

		/// <summary>
		/// Gets or sets the encrypted text.
		/// </summary>
		public string? EncryptedText
		{
			get => (string)this.GetValue(EncryptedTextProperty);
			set => this.SetValue(EncryptedTextProperty, value);
		}

		/// <summary>
		/// Initializes a new instance of the <see cref="ParameterProtectionIndicator"/> class.
		/// </summary>
		public ParameterProtectionIndicator()
		{
			// Define local styles for use within this control
			Style TransientBorderStyle = new Style(typeof(Border))
			{
				BasedOn = AppStyles.RoundedBorder,
				Setters =
					 {
						  new Setter { Property = BackgroundColorProperty, Value = AppColors.Purple15 },
						  new Setter { Property = Border.PaddingProperty, Value = new Thickness(AppStyles.SmallSpacing, AppStyles.SmallSpacing/2) }
					 }
			};

			Style EncryptedBorderStyle = new Style(typeof(Border))
			{
				BasedOn = AppStyles.RoundedBorder,
				Setters =
					 {
						  new Setter { Property = BackgroundColorProperty, Value = AppColors.Blue20Affirm },
						  new Setter { Property = Border.PaddingProperty, Value = new Thickness(AppStyles.SmallSpacing, AppStyles.SmallSpacing/2) }
					 }
			};

			Style TransientPathStyle = new Style(typeof(Path))
			{
				Setters =
					 {
						  new Setter { Property = Shape.FillProperty, Value = AppColors.Purple },
						  new Setter { Property = VerticalOptionsProperty, Value = LayoutOptions.Fill },
						  new Setter { Property = HorizontalOptionsProperty, Value = LayoutOptions.Fill },
					new Setter { Property = Shape.AspectProperty, Value = Stretch.Uniform },
					new Setter { Property = WidthRequestProperty, Value = 18 },
					new Setter { Property = HeightRequestProperty, Value = 18 }
					 }
			};

			Style EncryptedPathStyle = new Style(typeof(Path))
			{
				Setters =
					 {
						  new Setter { Property = Shape.FillProperty, Value = AppColors.Blue }, // Example color
                    new Setter { Property = VerticalOptionsProperty, Value = LayoutOptions.Fill },
						  new Setter { Property = HorizontalOptionsProperty, Value = LayoutOptions.Fill },
					new Setter { Property = Shape.AspectProperty, Value = Stretch.Uniform },
					new Setter { Property = WidthRequestProperty, Value = 18 },
					new Setter { Property = HeightRequestProperty, Value = 18 }
					 }
			};

			Style TransientLabelStyle = new Style(typeof(Label))
			{
				Setters =
					 {
						  new Setter { Property = Label.TextColorProperty, Value = AppColors.Purple },
						  new Setter { Property = Label.FontSizeProperty, Value = 15 },
						  new Setter { Property = Label.LineBreakModeProperty, Value = LineBreakMode.WordWrap },
						  new Setter { Property = Label.VerticalTextAlignmentProperty, Value = TextAlignment.Center },
						  new Setter { Property = Label.HorizontalTextAlignmentProperty, Value = TextAlignment.Center }
					 }
			};

			Style EncryptedLabelStyle = new Style(typeof(Label))
			{
				Setters =
					 {
						  new Setter { Property = Label.TextColorProperty, Value = AppColors.Blue },
						  new Setter { Property = Label.FontSizeProperty, Value = 15 },
						  new Setter { Property = Label.LineBreakModeProperty, Value = LineBreakMode.WordWrap },
						  new Setter { Property = Label.VerticalTextAlignmentProperty, Value = TextAlignment.Center },
						  new Setter { Property = Label.HorizontalTextAlignmentProperty, Value = TextAlignment.Center }
					 }
			};

			// Create a grid to hold the status indicators
			Grid StatusGrid = new Grid
			{
				ColumnDefinitions =
					 {
						  new ColumnDefinition { Width = GridLength.Auto },
						  new ColumnDefinition { Width = GridLength.Auto }
					 },
				ColumnSpacing = AppStyles.SmallSpacing
			};

			// Transient Border
			Border TransientBorder = new Border
			{
				Style = TransientBorderStyle
			};
			TransientBorder.SetBinding(IsVisibleProperty, new Binding(nameof(this.IsTransient), source: this));

			Grid TransientInnerGrid = new Grid
			{
				ColumnDefinitions =
					 {
						  new ColumnDefinition { Width = GridLength.Auto },
						  new ColumnDefinition { Width = GridLength.Auto }
					 },
				ColumnSpacing = AppStyles.SmallSpacing
			};

			Path TransientPath = new Path
			{
				Style = TransientPathStyle,
				// Assuming geometry data is defined here
				Data = Geometries.VisibilityOffPath
			};

			Label TransientLabel = new Label
			{
				Style = TransientLabelStyle,
			};
			TransientLabel.SetBinding(Label.TextProperty, new Binding(nameof(this.TransientText), source: this));

			Grid.SetColumn(TransientPath, 0);
			Grid.SetColumn(TransientLabel, 1);

			TransientInnerGrid.Children.Add(TransientPath);
			TransientInnerGrid.Children.Add(TransientLabel);
			TransientBorder.Content = TransientInnerGrid;

			// Encrypted Border
			Border EncryptedBorder = new Border
			{
				Style = EncryptedBorderStyle
			};
			EncryptedBorder.SetBinding(IsVisibleProperty, new Binding(nameof(this.IsEncrypted), source: this));

			Grid EncryptedInnerGrid = new Grid
			{
				ColumnDefinitions =
					 {
						  new ColumnDefinition { Width = GridLength.Auto },
						  new ColumnDefinition { Width = GridLength.Auto }
					 },
				ColumnSpacing = AppStyles.SmallSpacing
			};

			Path EncryptedPath = new Path
			{
				Style = EncryptedPathStyle,
				Data = Geometries.LockPath
			};

			Label EncryptedLabel = new Label
			{
				Style = EncryptedLabelStyle,
			};
			EncryptedLabel.SetBinding(Label.TextProperty, new Binding(nameof(this.EncryptedText), source: this));

			Grid.SetColumn(EncryptedPath, 0);
			Grid.SetColumn(EncryptedLabel, 1);

			EncryptedInnerGrid.Children.Add(EncryptedPath);
			EncryptedInnerGrid.Children.Add(EncryptedLabel);
			EncryptedBorder.Content = EncryptedInnerGrid;

			// Add borders to the status grid
			StatusGrid.Children.Add(TransientBorder);
			StatusGrid.Children.Add(EncryptedBorder);

			// Set the Content of the ContentView to the constructed grid
			this.Content = StatusGrid;
		}
	}
}
