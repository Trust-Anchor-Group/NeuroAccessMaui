namespace NeuroAccessMaui.UI.Pages.Wallet.Flow
{
	/// <summary>
	/// Draws the arrows of the flow map. Their labels are views placed on the map by <see cref="TokenFlowMap"/>, at
	/// <see cref="GetLabelCenter"/>, so they can wrap and be tapped.
	/// </summary>
	public class TokenFlowEdgeDrawable : IDrawable
	{
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
			}
		}

		/// <summary>
		/// Gets where an arrow's label goes: the middle of its curve.
		/// </summary>
		/// <param name="Edge">The arrow.</param>
		/// <returns>The middle of the curve, in map coordinates.</returns>
		public static Point GetLabelCenter(TokenFlowEdge Edge)
		{
			(PointF Start, PointF Control1, PointF Control2, PointF End) = GetCurve(Edge);

			return new Point((Start.X + 3 * Control1.X + 3 * Control2.X + End.X) / 8, (Start.Y + 3 * Control1.Y + 3 * Control2.Y + End.Y) / 8);
		}

		/// <summary>
		/// Works out the curve of an arrow, by how it runs. See <see cref="TokenFlowRoute"/>.
		/// </summary>
		private static (PointF Start, PointF Control1, PointF Control2, PointF End) GetCurve(TokenFlowEdge Edge)
		{
			Rect From = Edge.From;
			Rect To = Edge.To;

			switch (Edge.Route)
			{
				case TokenFlowRoute.Loop:
					float Side = Edge.OnLeft ? (float)From.Left : (float)From.Right;
					float Out = Edge.OnLeft ? -44 : 44;
					PointF LoopStart = new(Side, (float)(From.Top + From.Height * 0.3));
					PointF LoopEnd = new(Side, (float)(From.Top + From.Height * 0.7));

					return (LoopStart, new PointF(LoopStart.X + Out, LoopStart.Y - 22), new PointF(LoopEnd.X + Out, LoopEnd.Y + 22), LoopEnd);

				case TokenFlowRoute.Lane:
					PointF LaneStart = new(Edge.OnLeft ? (float)From.Left : (float)From.Right, (float)From.Center.Y);
					PointF LaneEnd = new(Edge.OnLeft ? (float)To.Left - 1 : (float)To.Right + 1, (float)To.Center.Y);
					float LaneX = (float)Edge.LaneX;

					return (LaneStart, new PointF(LaneX, LaneStart.Y), new PointF(LaneX, LaneEnd.Y), LaneEnd);

				case TokenFlowRoute.Beside:
					bool ToRight = To.Center.X > From.Center.X;
					float Y = (float)(From.Center.Y + Edge.Shift);
					PointF BesideStart = new(ToRight ? (float)From.Right : (float)From.Left, Y);
					PointF BesideEnd = new(ToRight ? (float)To.Left - 1 : (float)To.Right + 1, Y);
					float Third = (BesideEnd.X - BesideStart.X) / 3;

					return (BesideStart, new PointF(BesideStart.X + Third, Y), new PointF(BesideEnd.X - Third, Y), BesideEnd);

				case TokenFlowRoute.UnderRow:
					// Arrows to the right run in a shallow curve, and arrows back to the left in a wider, deeper one around it,
					// so two arrows between the same cards don't lie on top of each other.
					bool Rightward = To.Center.X > From.Center.X;
					float Direction = Rightward ? 1 : -1;
					float Reach = Rightward ? 0.3f : 0.15f;
					float Dip = Rightward ? 30 : 52;
					PointF UnderStart = new((float)(From.Center.X + Direction * From.Width * Reach), (float)From.Bottom);
					PointF UnderEnd = new((float)(To.Center.X - Direction * To.Width * Reach), (float)To.Bottom + 1);

					return (UnderStart, new PointF(UnderStart.X, UnderStart.Y + Dip), new PointF(UnderEnd.X, UnderEnd.Y + Dip), UnderEnd);

				case TokenFlowRoute.Up:
					// Off the centre of the cards, so it doesn't lie on an arrow down between the same cards.
					PointF UpStart = new((float)(From.Center.X + From.Width * 0.3), (float)From.Top);
					PointF UpEnd = new((float)(To.Center.X + To.Width * 0.3), (float)To.Bottom + 1);
					float UpMiddle = (UpStart.Y + UpEnd.Y) / 2;

					return (UpStart, new PointF(UpStart.X, UpMiddle), new PointF(UpEnd.X, UpMiddle), UpEnd);

				default:
					PointF DownStart = new((float)From.Center.X, (float)From.Bottom);
					PointF DownEnd = new((float)To.Center.X, (float)To.Top - 1);
					float DownMiddle = (DownStart.Y + DownEnd.Y) / 2;

					return (DownStart, new PointF(DownStart.X, DownMiddle), new PointF(DownEnd.X, DownMiddle), DownEnd);
			}
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
