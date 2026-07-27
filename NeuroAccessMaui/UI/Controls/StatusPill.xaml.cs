using Microsoft.Maui.Controls;

namespace NeuroAccessMaui.UI.Controls
{
	/// <summary>
	/// Defines the semantic visual tones supported by <see cref="StatusPill"/>.
	/// </summary>
	public enum StatusPillTone
	{
		/// <summary>
		/// Represents a neutral or informationally inactive status.
		/// </summary>
		Neutral,

		/// <summary>
		/// Represents an informational status.
		/// </summary>
		Information,

		/// <summary>
		/// Represents a successful or completed status.
		/// </summary>
		Success,

		/// <summary>
		/// Represents a warning or action-needed status.
		/// </summary>
		Warning,

		/// <summary>
		/// Represents a dangerous, failed, or destructive status.
		/// </summary>
		Danger
	}

	/// <summary>
	/// Displays concise status text with an optional icon and a theme-driven semantic tone.
	/// </summary>
	public partial class StatusPill : ContentView
	{
		/// <summary>
		/// Identifies the <see cref="Text"/> bindable property.
		/// </summary>
		public static readonly BindableProperty TextProperty = BindableProperty.Create(
			nameof(Text),
			typeof(string),
			typeof(StatusPill),
			string.Empty);

		/// <summary>
		/// Identifies the <see cref="IconSource"/> bindable property.
		/// </summary>
		public static readonly BindableProperty IconSourceProperty = BindableProperty.Create(
			nameof(IconSource),
			typeof(string),
			typeof(StatusPill),
			default(string));

		/// <summary>
		/// Identifies the <see cref="Tone"/> bindable property.
		/// </summary>
		public static readonly BindableProperty ToneProperty = BindableProperty.Create(
			nameof(Tone),
			typeof(StatusPillTone),
			typeof(StatusPill),
			StatusPillTone.Neutral);

		/// <summary>
		/// Initializes a new instance of the <see cref="StatusPill"/> class.
		/// </summary>
		public StatusPill()
		{
			this.InitializeComponent();
		}

		/// <summary>
		/// Gets or sets the localized status text.
		/// </summary>
		public string Text
		{
			get => (string?)this.GetValue(TextProperty) ?? string.Empty;
			set => this.SetValue(TextProperty, value);
		}

		/// <summary>
		/// Gets or sets the optional SVG asset displayed beside the status.
		/// </summary>
		public string? IconSource
		{
			get => (string?)this.GetValue(IconSourceProperty);
			set => this.SetValue(IconSourceProperty, value);
		}

		/// <summary>
		/// Gets or sets the semantic visual tone.
		/// </summary>
		public StatusPillTone Tone
		{
			get => (StatusPillTone)this.GetValue(ToneProperty);
			set => this.SetValue(ToneProperty, value);
		}
	}
}
