using Microsoft.Maui.Controls.Shapes;
using NeuroAccessMaui.Resources.Languages;
using NeuroAccessMaui.Services;
using Path = Microsoft.Maui.Controls.Shapes.Path;

namespace NeuroAccessMaui.UI.Controls
{
	/// <summary>
	/// The label above a contract value that is being filled in. A private value gets a small tag on the same line:
	/// "Only parties see this" for an encrypted value, or "Not saved" for a value that isn't kept in the contract.
	/// </summary>
	public class ParameterHeader : ContentView
	{
		/// <summary>
		/// The label text.
		/// </summary>
		public static readonly BindableProperty TextProperty =
			BindableProperty.Create(nameof(Text), typeof(string), typeof(ParameterHeader), string.Empty);

		/// <summary>
		/// If the value is encrypted, so only the contract's parties can read it.
		/// </summary>
		public static readonly BindableProperty IsEncryptedProperty =
			BindableProperty.Create(nameof(IsEncrypted), typeof(bool), typeof(ParameterHeader), false,
				propertyChanged: (Bindable, OldValue, NewValue) => ((ParameterHeader)Bindable).UpdateTag());

		/// <summary>
		/// If the value isn't saved in the contract.
		/// </summary>
		public static readonly BindableProperty IsTransientProperty =
			BindableProperty.Create(nameof(IsTransient), typeof(bool), typeof(ParameterHeader), false,
				propertyChanged: (Bindable, OldValue, NewValue) => ((ParameterHeader)Bindable).UpdateTag());

		/// <summary>
		/// The label's colour, when it should differ from the usual dimmed label colour.
		/// </summary>
		public static readonly BindableProperty TextColorProperty =
			BindableProperty.Create(nameof(TextColor), typeof(Color), typeof(ParameterHeader), null,
				propertyChanged: (Bindable, OldValue, NewValue) =>
				{
					if (NewValue is Color Color)
						((ParameterHeader)Bindable).textLabel.TextColor = Color;
				});

		private readonly Label textLabel;
		private readonly Border tag;
		private readonly Path tagIcon;
		private readonly Label tagLabel;

		/// <summary>
		/// Creates the label and its tag.
		/// </summary>
		public ParameterHeader()
		{
			this.textLabel = new Label();
			this.textLabel.SetDynamicResource(StyleProperty, "ParameterItemTitleLabel");
			this.textLabel.SetBinding(Label.TextProperty, new Binding(nameof(this.Text), source: this));

			this.tagIcon = new Path
			{
				Aspect = Stretch.Uniform,
				WidthRequest = 12,
				HeightRequest = 12,
				VerticalOptions = LayoutOptions.Center
			};
			this.tagIcon.SetDynamicResource(Shape.FillProperty, "ContentSecondaryWL");

			this.tagLabel = new Label
			{
				FontFamily = "HaasGroteskMedium",
				FontSize = 11,
				VerticalOptions = LayoutOptions.Center
			};
			this.tagLabel.SetDynamicResource(Label.TextColorProperty, "ContentSecondaryWL");

			this.tag = new Border
			{
				StrokeShape = new RoundRectangle { CornerRadius = 6 },
				StrokeThickness = 0,
				Padding = new Thickness(7, 2),
				VerticalOptions = LayoutOptions.Center,
				IsVisible = false,
				Content = new HorizontalStackLayout
				{
					Spacing = 4,
					Children = { this.tagIcon, this.tagLabel }
				}
			};
			this.tag.SetDynamicResource(BackgroundColorProperty, "SurfaceElevation2WL");

			Grid Layout = new()
			{
				ColumnDefinitions = [new ColumnDefinition(GridLength.Star), new ColumnDefinition(GridLength.Auto)],
				ColumnSpacing = 8
			};
			Layout.Add(this.textLabel, 0, 0);
			Layout.Add(this.tag, 1, 0);

			this.Content = Layout;
		}

		/// <summary>
		/// The label text.
		/// </summary>
		public string Text
		{
			get => (string)this.GetValue(TextProperty);
			set => this.SetValue(TextProperty, value);
		}

		/// <summary>
		/// The label's colour, when it should differ from the usual dimmed label colour.
		/// </summary>
		public Color? TextColor
		{
			get => (Color?)this.GetValue(TextColorProperty);
			set => this.SetValue(TextColorProperty, value);
		}

		/// <summary>
		/// If the value is encrypted, so only the contract's parties can read it.
		/// </summary>
		public bool IsEncrypted
		{
			get => (bool)this.GetValue(IsEncryptedProperty);
			set => this.SetValue(IsEncryptedProperty, value);
		}

		/// <summary>
		/// If the value isn't saved in the contract.
		/// </summary>
		public bool IsTransient
		{
			get => (bool)this.GetValue(IsTransientProperty);
			set => this.SetValue(IsTransientProperty, value);
		}

		/// <summary>
		/// Shows the tag that fits how the value is protected, or hides it.
		/// </summary>
		private void UpdateTag()
		{
			if (this.IsEncrypted)
			{
				this.tagIcon.Data = Geometries.LockPath;
				this.tagLabel.Text = ServiceRef.Localizer[nameof(AppResources.ParameterOnlyPartiesSee)];
				this.tag.IsVisible = true;
			}
			else if (this.IsTransient)
			{
				this.tagIcon.Data = Geometries.VisibilityOffPath;
				this.tagLabel.Text = ServiceRef.Localizer[nameof(AppResources.ParameterNotStored)];
				this.tag.IsVisible = true;
			}
			else
				this.tag.IsVisible = false;
		}
	}
}
