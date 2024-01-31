namespace NeuroAccessMaui.UI.Pages.Things.ReadSensor.Model
{
	/// <summary>
	/// Represents a header
	/// </summary>
	public class HeaderModel
	{
		/// <summary>
		/// Represents a header
		/// </summary>
		/// <param name="Label">Header label.</param>
		public HeaderModel(string Label)
		{
			this.Label = Label;
		}

		/// <summary>
		/// Header label.
		/// </summary>
		public string Label { get; set; }
	}
}
