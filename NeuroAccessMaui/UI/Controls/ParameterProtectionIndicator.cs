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
        /// Initializes a new instance of the <see cref="ParameterProtectionIndicator"/> class.
        /// </summary>
        public ParameterProtectionIndicator()
        {
            // Define local styles for use within this control
            Style transientBorderStyle = new Style(typeof(Border))
            {
				BasedOn = AppStyles.RoundedBorder,
                Setters =
                {
                    new Setter { Property = Border.BackgroundColorProperty, Value = AppColors.Purple15 },
                    new Setter { Property = Border.PaddingProperty, Value = new Thickness(AppStyles.SmallSpacing, AppStyles.SmallSpacing/2) }
                }
            };

            Style encryptedBorderStyle = new Style(typeof(Border))
            {
				BasedOn = AppStyles.RoundedBorder,
                Setters =
                {
                    new Setter { Property = Border.BackgroundColorProperty, Value = AppColors.Blue20Affirm },
                    new Setter { Property = Border.PaddingProperty, Value = new Thickness(AppStyles.SmallSpacing, AppStyles.SmallSpacing/2) }
                }
            };

            Style transientPathStyle = new Style(typeof(Path))
            {
                Setters =
                {
                    new Setter { Property = Path.FillProperty, Value = AppColors.Purple },
                    new Setter { Property = Path.VerticalOptionsProperty, Value = LayoutOptions.Fill },
                    new Setter { Property = Path.HorizontalOptionsProperty, Value = LayoutOptions.Fill },
					new Setter { Property = Path.AspectProperty, Value = Stretch.Uniform },
					new Setter { Property = Path.WidthRequestProperty, Value = 18 },
					new Setter { Property = Path.HeightRequestProperty, Value = 18 }
                }
            };

            Style encryptedPathStyle = new Style(typeof(Path))
            {
                Setters =
                {
                    new Setter { Property = Path.FillProperty, Value = AppColors.Blue }, // Example color
                    new Setter { Property = Path.VerticalOptionsProperty, Value = LayoutOptions.Fill },
                    new Setter { Property = Path.HorizontalOptionsProperty, Value = LayoutOptions.Fill },
					new Setter { Property = Path.AspectProperty, Value = Stretch.Uniform },
					new Setter { Property = Path.WidthRequestProperty, Value = 18 },
					new Setter { Property = Path.HeightRequestProperty, Value = 18 }
                }
            };

            Style transientLabelStyle = new Style(typeof(Label))
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

            Style encryptedLabelStyle = new Style(typeof(Label))
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
            Grid statusGrid = new Grid
            {
                ColumnDefinitions =
                {
                    new ColumnDefinition { Width = GridLength.Auto },
                    new ColumnDefinition { Width = GridLength.Auto }
                },
                ColumnSpacing = AppStyles.SmallSpacing
            };

            // Transient Border
            Border transientBorder = new Border
            {
                Style = transientBorderStyle
            };
            transientBorder.SetBinding(IsVisibleProperty, new Binding(nameof(this.IsTransient), source: this));

            Grid transientInnerGrid = new Grid
            {
                ColumnDefinitions =
                {
                    new ColumnDefinition { Width = GridLength.Auto },
                    new ColumnDefinition { Width = GridLength.Auto }
                },
                ColumnSpacing = AppStyles.SmallSpacing
            };

            Path transientPath = new Path
            {
                Style = transientPathStyle
            };
            // Assuming geometry data is defined here
            transientPath.Data = Geometries.VisibilityOffPath;

            Label transientLabel = new Label
            {
                Style = transientLabelStyle,
                Text = ServiceRef.Localizer[AppResources.NotAvailable]
            };

            Grid.SetColumn(transientPath, 0);
            Grid.SetColumn(transientLabel, 1);

            transientInnerGrid.Children.Add(transientPath);
            transientInnerGrid.Children.Add(transientLabel);
            transientBorder.Content = transientInnerGrid;

            // Encrypted Border
            Border encryptedBorder = new Border
            {
                Style = encryptedBorderStyle
            };
            encryptedBorder.SetBinding(IsVisibleProperty, new Binding(nameof(this.IsEncrypted), source: this));

            Grid encryptedInnerGrid = new Grid
            {
                ColumnDefinitions =
                {
                    new ColumnDefinition { Width = GridLength.Auto },
                    new ColumnDefinition { Width = GridLength.Auto }
                },
                ColumnSpacing = AppStyles.SmallSpacing 
            };

            Path encryptedPath = new Path
            {
                Style = encryptedPathStyle
            };
            encryptedPath.Data = Geometries.LockPath;

            Label encryptedLabel = new Label
            {
                Style = encryptedLabelStyle,
                Text = ServiceRef.Localizer[AppResources.Encrypted]
            };

            Grid.SetColumn(encryptedPath, 0);
            Grid.SetColumn(encryptedLabel, 1);

            encryptedInnerGrid.Children.Add(encryptedPath);
            encryptedInnerGrid.Children.Add(encryptedLabel);
            encryptedBorder.Content = encryptedInnerGrid;

            // Add borders to the status grid
            statusGrid.Children.Add(transientBorder);
            statusGrid.Children.Add(encryptedBorder);

            // Set the Content of the ContentView to the constructed grid
            this.Content = statusGrid;
        }
    }
}
