using Microsoft.Maui.Controls;
using Microsoft.Maui.Graphics;

namespace NeuroAccessMaui.UI.Controls
{
	public partial class InfoBubble : ContentView
	{
		public InfoBubble()
		{
			this.InitializeComponent();
		}

		// Label Text
		public static readonly BindableProperty LabelTextProperty =
			BindableProperty.Create(
				nameof(LabelText),
				typeof(string),
				typeof(InfoBubble),
				string.Empty);

		public string LabelText
		{
			get => (string)this.GetValue(LabelTextProperty);
			set => this.SetValue(LabelTextProperty, value);
		}

		// SVG Source (as string)
		public static readonly BindableProperty SvgSourceProperty =
			BindableProperty.Create(
				nameof(SvgSource),
				typeof(string),
				typeof(InfoBubble),
				default(string));

		public string SvgSource
		{
			get => (string)this.GetValue(SvgSourceProperty);
			set => this.SetValue(SvgSourceProperty, value);
		}

		// Content Color (used for both icon tint and label text)
		public static readonly BindableProperty ContentColorProperty =
			BindableProperty.Create(
				nameof(ContentColor),
				typeof(Color),
				typeof(InfoBubble),
				Colors.Black); // Default value

		public Color ContentColor
		{
			get => (Color)this.GetValue(ContentColorProperty);
			set => this.SetValue(ContentColorProperty, value);
		}

		// Background Color (of the bubble)
		public static new readonly BindableProperty BackgroundColorProperty =
			BindableProperty.Create(
				nameof(BackgroundColor),
				typeof(Color),
				typeof(InfoBubble),
				Colors.Transparent); // Override base BackgroundColor

		public new Color BackgroundColor
		{
			get => (Color)this.GetValue(BackgroundColorProperty);
			set => this.SetValue(BackgroundColorProperty, value);
		}
	}
}
