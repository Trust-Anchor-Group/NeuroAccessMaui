namespace NeuroAccessMaui.UI.Controls
{
	/// <summary>
	/// Describes one frame of a <see cref="StatusVisual"/> scene.
	/// </summary>
	/// <param name="Kind">The scene to draw.</param>
	/// <param name="Entrance">Entrance progress from 0 to 1. At 1 the scene is fully settled.</param>
	/// <param name="LoopPhase">Phase of the pending loop from 0 to 1.</param>
	/// <param name="Celebration">Celebration progress from 0 to 1, or a negative value when no celebration is playing.</param>
	/// <param name="Accent">Color for the disk, glyphs, and celebration.</param>
	/// <param name="OnAccent">Color for glyphs drawn on a filled accent disk.</param>
	/// <param name="Warning">Color for the attention scene.</param>
	public readonly record struct StatusVisualFrame(
		StatusVisualKind Kind,
		double Entrance,
		double LoopPhase,
		double Celebration,
		Color Accent,
		Color OnAccent,
		Color Warning);

	/// <summary>
	/// Draws <see cref="StatusVisual"/> scenes into a square design box of 200 units centered in the bounds.
	/// </summary>
	/// <remarks>
	/// Drawing is a pure function of <see cref="StatusVisualFrame"/>, so frames can be rendered without a live view.
	/// </remarks>
	public static class StatusVisualPainter
	{
		private const float designSize = 200f;
		private const float haloRadius = 84f;
		private const float diskRadius = 60f;
		private const float orbitRadius = 76f;
		private const double orbitSweepDegrees = 64;
		private const double flipStartPhase = 0.76;

		private const string hourglassPathData =
			"M2 0C0.9 0 0 0.9 0 2V5.17C0 5.7 0.21 6.21 0.59 6.59L4 10L0.58 13.42C0.21 13.8 0 14.31 0 14.84V18C0 19.1 0.9 20 2 20H10C11.1 20 12 19.1 12 18V14.84C12 14.31 11.79 13.8 11.42 13.43L8 10L11.41 6.6C11.79 6.22 12 5.71 12 5.18V2C12 0.9 11.1 0 10 0H2ZM10 14.5V17C10 17.55 9.55 18 9 18H3C2.45 18 2 17.55 2 17V14.5L6 10.5L10 14.5ZM6 9.5L2 5.5V3C2 2.45 2.45 2 3 2H9C9.55 2 10 2.45 10 3V5.5L6 9.5Z";

		private const string pencilPathData =
			"M3 17.25V21H6.75L17.81 9.94L14.06 6.19L3 17.25ZM20.71 7.04C21.1 6.65 21.1 6.02 20.71 5.63L18.37 3.29C17.98 2.9 17.35 2.9 16.96 3.29L15.13 5.12L18.88 8.87L20.71 7.04Z";

		private static readonly Lazy<PathF> hourglassPath = new(() => PathBuilder.Build(hourglassPathData));
		private static readonly Lazy<PathF> pencilPath = new(() => PathBuilder.Build(pencilPathData));

		/// <summary>
		/// Draws a frame centered in the given bounds.
		/// </summary>
		/// <param name="Canvas">The canvas to draw on.</param>
		/// <param name="Bounds">The drawing bounds.</param>
		/// <param name="Frame">The frame to draw.</param>
		public static void Draw(ICanvas Canvas, RectF Bounds, StatusVisualFrame Frame)
		{
			float Size = Math.Min(Bounds.Width, Bounds.Height);
			if (Size <= 0)
				return;

			Canvas.SaveState();
			Canvas.Translate(Bounds.Center.X, Bounds.Center.Y);
			Canvas.Scale(Size / designSize, Size / designSize);
			Canvas.StrokeLineCap = LineCap.Round;
			Canvas.StrokeLineJoin = LineJoin.Round;

			Color Tone = Frame.Kind == StatusVisualKind.Attention ? Frame.Warning : Frame.Accent;
			double Entrance = Math.Clamp(Frame.Entrance, 0d, 1d);
			double DiskProgress = Math.Clamp(Entrance / 0.6d, 0d, 1d);
			float DiskScale = (float)BackOut(DiskProgress);
			double GlyphProgress = CubicOut(Math.Clamp((Entrance - 0.3d) / 0.7d, 0d, 1d));

			if (Frame.Kind == StatusVisualKind.Attention && Entrance < 1d)
			{
				float Shake = (float)(Math.Sin(Entrance * Math.PI * 6d) * (1d - Entrance) * 7d);
				Canvas.Translate(Shake, 0);
			}

			Canvas.FillColor = Tone.WithAlpha(0.08f * (float)DiskProgress);
			Canvas.FillCircle(0, 0, haloRadius * DiskScale);

			if (Frame.Kind == StatusVisualKind.Success)
			{
				Canvas.FillColor = Tone;
				Canvas.FillCircle(0, 0, diskRadius * DiskScale);
			}
			else
			{
				Canvas.FillColor = Tone.WithAlpha(0.16f);
				Canvas.FillCircle(0, 0, diskRadius * DiskScale);
			}

			switch (Frame.Kind)
			{
				case StatusVisualKind.Draft:
					DrawGlyph(Canvas, pencilPath.Value, 24f, 12f, 12f, 58f, 0f, Tone, GlyphProgress);
					break;

				case StatusVisualKind.Pending:
					DrawOrbit(Canvas, Tone, Frame.LoopPhase, DiskProgress);
					DrawGlyph(Canvas, hourglassPath.Value, 20f, 6f, 10f, 58f, FlipAngle(Frame.LoopPhase), Tone, GlyphProgress);
					break;

				case StatusVisualKind.Success:
					DrawCheck(Canvas, Frame.OnAccent, GlyphProgress);
					break;

				case StatusVisualKind.Attention:
					DrawExclamation(Canvas, Tone, GlyphProgress);
					break;
			}

			if (Frame.Celebration >= 0d && Frame.Celebration < 1d)
				DrawCelebration(Canvas, Tone, Frame.Celebration);

			Canvas.RestoreState();
		}

		private static void DrawGlyph(ICanvas Canvas, PathF Path, float PathHeight, float PathCenterX, float PathCenterY, float Height, double Rotation, Color Color, double Progress)
		{
			if (Progress <= 0d)
				return;

			float Scale = Height / PathHeight * (float)(0.6d + 0.4d * Progress);

			Canvas.SaveState();
			Canvas.Alpha = (float)Progress;
			Canvas.Rotate((float)Rotation);
			Canvas.Scale(Scale, Scale);
			Canvas.Translate(-PathCenterX, -PathCenterY);
			Canvas.FillColor = Color;
			Canvas.FillPath(Path, WindingMode.EvenOdd);
			Canvas.RestoreState();
		}

		private static void DrawOrbit(ICanvas Canvas, Color Tone, double LoopPhase, double Appear)
		{
			if (Appear <= 0d)
				return;

			Canvas.StrokeColor = Tone.WithAlpha(0.14f * (float)Appear);
			Canvas.StrokeSize = 3f;
			Canvas.DrawCircle(0, 0, orbitRadius);

			double StartDegrees = -90d + LoopPhase * 360d;
			Canvas.StrokeColor = Tone.WithAlpha((float)Appear);
			Canvas.StrokeSize = 5f;
			Canvas.DrawPath(CreateArc(orbitRadius, StartDegrees, orbitSweepDegrees));
		}

		private static void DrawCheck(ICanvas Canvas, Color Color, double Progress)
		{
			if (Progress <= 0d)
				return;

			PointF Start = new PointF(-25f, 2f);
			PointF Corner = new PointF(-8f, 19f);
			PointF End = new PointF(26f, -17f);
			double FirstLength = Distance(Start, Corner);
			double SecondLength = Distance(Corner, End);
			double Drawn = Progress * (FirstLength + SecondLength);

			PathF Check = new PathF();
			Check.MoveTo(Start);
			if (Drawn <= FirstLength)
			{
				Check.LineTo(Interpolate(Start, Corner, Drawn / FirstLength));
			}
			else
			{
				Check.LineTo(Corner);
				Check.LineTo(Interpolate(Corner, End, (Drawn - FirstLength) / SecondLength));
			}

			Canvas.StrokeColor = Color;
			Canvas.StrokeSize = 11f;
			Canvas.DrawPath(Check);
		}

		private static void DrawExclamation(ICanvas Canvas, Color Color, double Progress)
		{
			if (Progress <= 0d)
				return;

			double LineProgress = Math.Clamp(Progress / 0.75d, 0d, 1d);
			PointF Top = new PointF(0f, -28f);
			PointF Bottom = new PointF(0f, 6f);
			PointF Tip = Interpolate(Top, Bottom, LineProgress);

			Canvas.StrokeColor = Color;
			Canvas.StrokeSize = 11f;
			Canvas.DrawLine(Top, Tip);

			double DotProgress = Math.Clamp((Progress - 0.7d) / 0.3d, 0d, 1d);
			if (DotProgress > 0d)
			{
				Canvas.FillColor = Color;
				Canvas.FillCircle(0f, 26f, 6.5f * (float)BackOut(DotProgress));
			}
		}

		private static void DrawCelebration(ICanvas Canvas, Color Tone, double Progress)
		{
			double Eased = CubicOut(Progress);
			float Fade = (float)(1d - Progress);

			Canvas.StrokeColor = Tone.WithAlpha(0.45f * Fade);
			Canvas.StrokeSize = 5f * Fade + 0.5f;
			Canvas.DrawCircle(0, 0, (float)(diskRadius + (96d - diskRadius) * Eased));

			const int ParticleCount = 12;
			for (int i = 0; i < ParticleCount; i++)
			{
				double Angle = (i * 360d / ParticleCount + 15d) * Math.PI / 180d;
				double Reach = 70d + (i % 2 == 0 ? 26d : 18d) * Eased;
				float Radius = (float)((i % 2 == 0 ? 5d : 3.5d) * (1d - Progress * 0.7d));
				Canvas.FillColor = Tone.WithAlpha((i % 2 == 0 ? 0.9f : 0.55f) * Fade);
				Canvas.FillCircle((float)(Math.Cos(Angle) * Reach), (float)(Math.Sin(Angle) * Reach), Radius);
			}
		}

		private static PathF CreateArc(float Radius, double StartDegrees, double SweepDegrees)
		{
			const int Steps = 24;
			PathF Arc = new PathF();
			for (int i = 0; i <= Steps; i++)
			{
				double Radians = (StartDegrees + SweepDegrees * i / Steps) * Math.PI / 180d;
				PointF Point = new PointF((float)(Math.Cos(Radians) * Radius), (float)(Math.Sin(Radians) * Radius));
				if (i == 0)
					Arc.MoveTo(Point);
				else
					Arc.LineTo(Point);
			}

			return Arc;
		}

		/// <summary>
		/// Turns the hourglass over near the end of each loop. The glyph is symmetric, so the loop restarts seamlessly.
		/// </summary>
		private static double FlipAngle(double LoopPhase)
		{
			if (LoopPhase <= flipStartPhase)
				return 0d;

			double Flip = (LoopPhase - flipStartPhase) / (1d - flipStartPhase);
			return 180d * CubicInOut(Math.Clamp(Flip, 0d, 1d));
		}

		private static PointF Interpolate(PointF From, PointF To, double Amount)
		{
			return new PointF(
				(float)(From.X + (To.X - From.X) * Amount),
				(float)(From.Y + (To.Y - From.Y) * Amount));
		}

		private static double Distance(PointF From, PointF To)
		{
			double Dx = To.X - From.X;
			double Dy = To.Y - From.Y;
			return Math.Sqrt(Dx * Dx + Dy * Dy);
		}

		private static double CubicOut(double T)
		{
			double Inverse = 1d - T;
			return 1d - Inverse * Inverse * Inverse;
		}

		private static double CubicInOut(double T)
		{
			return T < 0.5d
				? 4d * T * T * T
				: 1d - Math.Pow(-2d * T + 2d, 3d) / 2d;
		}

		private static double BackOut(double T)
		{
			const double Overshoot = 1.4d;
			double Shifted = T - 1d;
			return 1d + (Overshoot + 1d) * Shifted * Shifted * Shifted + Overshoot * Shifted * Shifted;
		}
	}
}
