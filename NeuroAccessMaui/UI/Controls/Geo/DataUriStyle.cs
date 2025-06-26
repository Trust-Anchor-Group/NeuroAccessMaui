using Mapsui.Styles;

namespace NeuroAccessMaui.UI.Controls.Geo
{
	/// <summary>
	/// Marks a feature that should be rendered by <see cref="DataUriStyleRenderer"/>.
	/// </summary>
	public class DataUriStyle : IStyle
	{
		public bool Enabled { get; set; } = true;
		public float Opacity { get; set; } = 1.0f;
		public double MinVisible { get; set; } = 0;
		public double MaxVisible { get; set; } = double.MaxValue;
	}
}
