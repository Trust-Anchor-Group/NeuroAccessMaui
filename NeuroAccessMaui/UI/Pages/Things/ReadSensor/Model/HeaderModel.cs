namespace NeuroAccessMaui.UI.Pages.Things.ReadSensor.Model
{
	/// <summary>
	/// Represents a header
	/// </summary>
	/// <param name="Label">Header label.</param>
	public class HeaderModel(string Label)
	{
		/// <summary>
		/// Header label.
		/// </summary>
		public string Label { get; set; } = Label;
	}
}
