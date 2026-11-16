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

		/// <summary>
		/// What tapping the info mark does: shows the whole description. The mark is shown while this is set.
		/// </summary>
		public static readonly BindableProperty InfoCommandProperty =
			BindableProperty.Create(nameof(InfoCommand), typeof(System.Windows.Input.ICommand), typeof(ParameterHeader), null,
				propertyChanged: (Bindable, OldValue, NewValue) => ((ParameterHeader)Bindable).UpdateInfo());

		/// <summary>
		/// If the info mark is shown, because the label is shorter than the description.
		/// </summary>
		public static readonly BindableProperty ShowsInfoProperty =
			BindableProperty.Create(nameof(ShowsInfo), typeof(bool), typeof(ParameterHeader), false,
				propertyChanged: (Bindable, OldValue, NewValue) => ((ParameterHeader)Bindable).UpdateInfo());

		private readonly Label textLabel;
		private readonly Grid info;
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

			// The info mark is small, but the area that can be tapped is 48 by 48.
			Path InfoIcon = new()
			{
				Data = Geometries.InfoCirclePath,
				Aspect = Stretch.Uniform,
				WidthRequest = 16,
				HeightRequest = 16,
				HorizontalOptions = LayoutOptions.Center,
				VerticalOptions = LayoutOptions.Center
			};
			InfoIcon.SetDynamicResource(Shape.FillProperty, "ContentSecondaryWL");

			this.info = new Grid
			{
				WidthRequest = 48,
				HeightRequest = 48,
				Margin = new Thickness(-14, -14, -14, -14),
				IsVisible = false,
				Children = { InfoIcon }
			};
			TapGestureRecognizer InfoTap = new();
			InfoTap.Tapped += (Sender, Args) =>
			{
				if (this.InfoCommand?.CanExecute(null) == true)
					this.InfoCommand.Execute(null);
			};
			this.info.GestureRecognizers.Add(InfoTap);
			SemanticProperties.SetDescription(this.info, ServiceRef.Localizer[nameof(AppResources.ParameterMoreAbout)]);

			Grid Layout = new()
			{
				ColumnDefinitions = [new ColumnDefinition(GridLength.Star), new ColumnDefinition(GridLength.Auto), new ColumnDefinition(GridLength.Auto)],
				ColumnSpacing = 8
			};
			Layout.Add(this.textLabel, 0, 0);
			Layout.Add(this.info, 1, 0);
			Layout.Add(this.tag, 2, 0);

			this.Content = Layout;
		}

		/// <summary>
		/// What tapping the info mark does: shows the whole description. The mark is shown while this is set.
		/// </summary>
		public System.Windows.Input.ICommand? InfoCommand
		{
			get => (System.Windows.Input.ICommand?)this.GetValue(InfoCommandProperty);
			set => this.SetValue(InfoCommandProperty, value);
		}

		/// <summary>
		/// If the info mark is shown, because the label is shorter than the description.
		/// </summary>
		public bool ShowsInfo
		{
			get => (bool)this.GetValue(ShowsInfoProperty);
			set => this.SetValue(ShowsInfoProperty, value);
		}

		/// <summary>
		/// Shows the info mark when there is more to read and a way to show it.
		/// </summary>
		private void UpdateInfo()
		{
			this.info.IsVisible = this.ShowsInfo && this.InfoCommand is not null;
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
