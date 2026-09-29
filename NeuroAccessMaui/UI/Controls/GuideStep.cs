namespace NeuroAccessMaui.UI.Controls
{
	/// <summary>
	/// Describes one step shown by <see cref="GuideStepper"/>.
	/// </summary>
	/// <remarks>
	/// Steps are bindable objects rather than views, so their text can use localized bindings with an explicit source.
	/// </remarks>
	public class GuideStep : BindableObject
	{
		/// <summary>
		/// Identifies the <see cref="Text"/> bindable property.
		/// </summary>
		public static readonly BindableProperty TextProperty = BindableProperty.Create(
			nameof(Text),
			typeof(string),
			typeof(GuideStep),
			string.Empty);

		/// <summary>
		/// Gets or sets the short caption displayed under the step indicator.
		/// </summary>
		public string Text
		{
			get => (string)this.GetValue(TextProperty);
			set => this.SetValue(TextProperty, value);
		}
	}
}
