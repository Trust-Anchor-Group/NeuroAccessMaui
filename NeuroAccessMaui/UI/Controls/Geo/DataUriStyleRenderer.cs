using Mapsui;
using Mapsui.Extensions;
using Mapsui.Layers;
using Mapsui.Rendering;
using Mapsui.Rendering.Skia.Cache;
using Mapsui.Rendering.Skia.SkiaStyles;
using Mapsui.Styles;
using SkiaSharp;

namespace NeuroAccessMaui.UI.Controls.Geo
{
	/// <summary>
	/// Draws each feature tagged with <see cref="DataUriStyle"/>, reading its “DataUri” attribute
	/// and rendering a custom icon or marker.
	/// </summary>
	public class DataUriStyleRenderer : ISkiaStyleRenderer
	{
		public bool Draw(
			SKCanvas Canvas,
			Viewport Viewport,
			ILayer Layer,
			IFeature Feature,
			IStyle Style,
			RenderService renderService, long iteration)
		{
			if (Style is not DataUriStyle CustomStyle)
				return false;

			// Extract the feature’s single coordinate (world = EPSG:3857)
			double WorldX = 0, WorldY = 0;
			bool GotPoint = false;

			Feature.CoordinateVisitor((x, y, setter) =>
			{
				WorldX = x;
				WorldY = y;
				GotPoint = true;
				// If there were multiple coordinates (e.g. a linestring),
				// you could collect or test them all; here we assume a point.
			});

			if (!GotPoint)
				return false;

			// Convert world (spherical mercator) → screen point
			MPoint WorldPoint = new MPoint(WorldX, WorldY);
			var Screen = Viewport.WorldToScreen(WorldPoint);

			// Retrieve your data URI (for use in icon logic)
			string UriString = Feature["DataUri"]?.ToString() ?? string.Empty;

			// TODO: replace this placeholder circle with your UriToMapIcon(uriString) logic
			using SKPaint Paint = new SKPaint
			{
				Color = SKColors.Blue.WithAlpha((byte)(255 * CustomStyle.Opacity)),
				IsAntialias = true
			};
			Canvas.DrawCircle((float)Screen.X, (float)Screen.Y, 12, Paint);

			return true;
		}
	}
}
