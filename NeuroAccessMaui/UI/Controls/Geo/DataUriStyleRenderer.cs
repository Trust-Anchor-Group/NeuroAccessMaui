using Mapsui;
using Mapsui.Extensions;
using Mapsui.Layers;
// Attempt to use Skia renderer interfaces; if unavailable, fall back to stub.
#if MAPSUI_SKIA
using Mapsui.Rendering.Skia; // Provides ISkiaStyleRenderer & IRenderCache in Mapsui Skia packages
#endif
using Mapsui.Rendering.Skia.SkiaStyles;
using Mapsui.Styles;
using SkiaSharp;

namespace NeuroAccessMaui.UI.Controls.Geo
{
#if MAPSUI_SKIA
	/// <summary>
	/// Draws each feature tagged with <see cref="DataUriStyle"/>, reading its “DataUri” attribute
	/// and rendering a custom icon or marker.
	/// </summary>
	public class DataUriStyleRenderer : ISkiaStyleRenderer
	{
		/// <summary>
		/// Draws features styled with <see cref="DataUriStyle"/> by reading their <c>DataUri</c> attribute.
		/// </summary>
		/// <param name="Canvas">Target Skia canvas.</param>
		/// <param name="Viewport">Current map viewport.</param>
		/// <param name="Layer">Layer being rendered.</param>
		/// <param name="Feature">Feature to draw.</param>
		/// <param name="Style">Resolved style.</param>
		/// <param name="RenderCache">Render cache provided by Mapsui.</param>
		/// <param name="Iteration">Current render iteration.</param>
		/// <returns>True if custom rendering occurred; otherwise false.</returns>
		public bool Draw(
			SKCanvas Canvas,
			Viewport Viewport,
			ILayer Layer,
			IFeature Feature,
			IStyle Style,
			IRenderCache RenderCache,
			long Iteration)
		{
			if (Style is not DataUriStyle CustomStyle)
				return false;

			// Extract a single coordinate (assumes point geometry)
			double WorldX = 0;
			double WorldY = 0;
			bool GotPoint = false;

			Feature.CoordinateVisitor((X, Y, Setter) =>
			{
				WorldX = X;
				WorldY = Y;
				GotPoint = true;
			});

			if (!GotPoint)
				return false;

			MPoint WorldPoint = new MPoint(WorldX, WorldY);
			MPoint Screen = Viewport.WorldToScreen(WorldPoint);

			string DataUri = Feature["DataUri"]?.ToString() ?? string.Empty;

			// Placeholder: Render a circle. Replace with logic parsing DataUri into an icon.
			using SKPaint Paint = new SKPaint
			{
				Color = SKColors.Blue.WithAlpha((byte)(255 * CustomStyle.Opacity)),
				IsAntialias = true
			};
			Canvas.DrawCircle((float)Screen.X, (float)Screen.Y, 12, Paint);

			return true;
		}
	}
#else
	/// <summary>
	/// Stub renderer compiled when MAPSUI_SKIA is not defined; avoids build break.
	/// </summary>
	public class DataUriStyleRenderer
	{
		public bool Draw() => false;
	}
#endif
}
