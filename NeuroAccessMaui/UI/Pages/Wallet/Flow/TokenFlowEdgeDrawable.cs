namespace NeuroAccessMaui.UI.Pages.Wallet.Flow
{
	/// <summary>
	/// Draws the arrows of the flow map, with a short label on each.
	/// </summary>
	public class TokenFlowEdgeDrawable : IDrawable
	{
		private const int maxLabelLength = 22;
		private const float labelFontSize = 10;
		private const float arrowLength = 8;
		private const float arrowHalfWidth = 4;

		/// <summary>
		/// The arrows to draw.
		/// </summary>
		public IReadOnlyList<TokenFlowEdge> Edges { get; set; } = [];

		/// <summary>
		/// Draws the arrows.
		/// </summary>
		/// <param name="Canvas">The canvas to draw on.</param>
		/// <param name="DirtyRect">The area to draw.</param>
		public void Draw(ICanvas Canvas, RectF DirtyRect)
		{
			Color EdgeColor = GetColor("ContentSecondaryWL", Colors.Gray);
			Color AccentColor = GetColor("ContentAccessWL", Colors.Teal);
			Color LabelBackground = GetColor("SurfaceBackgroundWL", Colors.White);

			foreach (TokenFlowEdge Edge in this.Edges)
			{
				(PointF Start, PointF Control1, PointF Control2, PointF End) = GetCurve(Edge);
				Color Color = Edge.IsFromCurrent ? AccentColor : EdgeColor;

				PathF Path = new();
				Path.MoveTo(Start);
				Path.CurveTo(Control1, Control2, End);

				Canvas.StrokeColor = Color;
				Canvas.StrokeSize = Edge.IsFromCurrent ? 2 : 1.5f;
				Canvas.StrokeDashPattern = Edge.IsFailure ? [5, 4] : null;
				Canvas.DrawPath(Path);

				DrawArrowHead(Canvas, Color, Control2, End);
				DrawLabel(Canvas, Edge, Start, Control1, Control2, End, EdgeColor, LabelBackground);
			}
		}

		/// <summary>
		/// Works out the curve of an arrow. An arrow down the map runs from the bottom of one card to the top of the next. A
		/// loop and an arrow back up run out to the right of the cards.
		/// </summary>
		private static (PointF Start, PointF Control1, PointF Control2, PointF End) GetCurve(TokenFlowEdge Edge)
		{
			Rect From = Edge.From;
			Rect To = Edge.To;

			if (Edge.IsLoop)
			{
				PointF Start = new((float)From.Right, (float)(From.Top + From.Height * 0.3));
				PointF End = new((float)From.Right, (float)(From.Top + From.Height * 0.7));

				return (Start, new PointF(Start.X + 44, Start.Y - 22), new PointF(End.X + 44, End.Y + 22), End);
			}

			if (Edge.IsForward)
			{
				PointF Start = new((float)From.Center.X, (float)From.Bottom);
				PointF End = new((float)To.Center.X, (float)To.Top - 1);
				float MiddleY = (Start.Y + End.Y) / 2;

				return (Start, new PointF(Start.X, MiddleY), new PointF(End.X, MiddleY), End);
			}

			PointF BackStart = new((float)From.Right, (float)From.Center.Y);
			PointF BackEnd = new((float)To.Right + 1, (float)To.Center.Y);
			float OutX = (float)Math.Max(From.Right, To.Right) + 36;

			return (BackStart, new PointF(OutX, BackStart.Y), new PointF(OutX, BackEnd.Y), BackEnd);
		}

		/// <summary>
		/// Draws the head of an arrow, pointing the way the curve ends.
		/// </summary>
		private static void DrawArrowHead(ICanvas Canvas, Color Color, PointF Control, PointF End)
		{
			float Dx = End.X - Control.X;
			float Dy = End.Y - Control.Y;
			float Length = MathF.Sqrt(Dx * Dx + Dy * Dy);

			if (Length < 0.001f)
				return;

			Dx /= Length;
			Dy /= Length;

			PathF Head = new();
			Head.MoveTo(End);
			Head.LineTo(End.X - Dx * arrowLength - Dy * arrowHalfWidth, End.Y - Dy * arrowLength + Dx * arrowHalfWidth);
			Head.LineTo(End.X - Dx * arrowLength + Dy * arrowHalfWidth, End.Y - Dy * arrowLength - Dx * arrowHalfWidth);
			Head.Close();

			Canvas.StrokeDashPattern = null;
			Canvas.FillColor = Color;
			Canvas.FillPath(Head);
		}

		/// <summary>
		/// Draws the label of an arrow at the middle of its curve, on a small background so it can be read over lines.
		/// </summary>
		private static void DrawLabel(ICanvas Canvas, TokenFlowEdge Edge, PointF Start, PointF Control1, PointF Control2, PointF End,
			Color TextColor, Color Background)
		{
			if (string.IsNullOrEmpty(Edge.Label))
				return;

			string Text = Edge.Label.Length > maxLabelLength ? Edge.Label[..(maxLabelLength - 1)].TrimEnd() + "…" : Edge.Label;

			// The middle of a cubic curve.
			float X = (Start.X + 3 * Control1.X + 3 * Control2.X + End.X) / 8;
			float Y = (Start.Y + 3 * Control1.Y + 3 * Control2.Y + End.Y) / 8;

			Canvas.FontSize = labelFontSize;
			Canvas.Font = Microsoft.Maui.Graphics.Font.DefaultBold;

			SizeF Size = Canvas.GetStringSize(Text, Microsoft.Maui.Graphics.Font.DefaultBold, labelFontSize);
			RectF Box = new(X - Size.Width / 2 - 5, Y - Size.Height / 2 - 2, Size.Width + 10, Size.Height + 4);

			Canvas.FillColor = Background;
			Canvas.FillRoundedRectangle(Box, 4);

			Canvas.FontColor = TextColor;
			Canvas.DrawString(Text, Box, HorizontalAlignment.Center, VerticalAlignment.Center);
		}

		/// <summary>
		/// Gets a colour from the app's resources, which follow the light or dark theme.
		/// </summary>
		/// <param name="Key">The resource key.</param>
		/// <param name="Fallback">The colour to use when the resource is missing.</param>
		/// <returns>The colour.</returns>
		private static Color GetColor(string Key, Color Fallback)
		{
			if (Application.Current?.Resources.TryGetValue(Key, out object? Value) == true && Value is Color Color)
				return Color;

			return Fallback;
		}
	}
}
