using NeuroAccessMaui.Services.Kyc.Models;

namespace NeuroAccessMaui.UI.Pages.Utility.Images
{
	/// <summary>
	/// Draws a dimmed surround with a cut-out that shows where to place the face or the document for a capture profile.
	/// </summary>
	/// <remarks>
	/// Selfies get an oval. Documents get a rounded frame with corner brackets in the document's aspect ratio.
	/// </remarks>
	public sealed class CaptureGuideDrawable : IDrawable
	{
		private const float maximumOvalWidth = 300f;
		private const float ovalAspect = 1.32f;
		private const float maximumFrameWidth = 560f;

		private readonly KycCaptureProfile profile;
		private readonly double frameAspectRatio;

		/// <summary>
		/// Initializes a new instance of the <see cref="CaptureGuideDrawable"/> class.
		/// </summary>
		/// <param name="Profile">The capture profile that decides the guide shape.</param>
		/// <param name="FrameAspectRatio">The document frame aspect ratio, as width divided by height.</param>
		public CaptureGuideDrawable(KycCaptureProfile Profile, double FrameAspectRatio)
		{
			this.profile = Profile;
			this.frameAspectRatio = FrameAspectRatio > 0 ? FrameAspectRatio : 1.41;
		}

		/// <inheritdoc/>
		public void Draw(ICanvas Canvas, RectF DirtyRect)
		{
			if (DirtyRect.Width <= 0 || DirtyRect.Height <= 0)
				return;

			Canvas.SaveState();
			Canvas.StrokeLineCap = LineCap.Round;
			Canvas.StrokeLineJoin = LineJoin.Round;

			if (this.profile == KycCaptureProfile.Selfie)
				DrawOval(Canvas, DirtyRect);
			else
				this.DrawFrame(Canvas, DirtyRect);

			Canvas.RestoreState();
		}

		/// <summary>
		/// Places the face oval in the upper part of the screen, above the guidance card and shutter button.
		/// </summary>
		/// <param name="Bounds">The overlay bounds.</param>
		/// <returns>The oval bounds.</returns>
		public static RectF GetOval(RectF Bounds)
		{
			float Width = Math.Min(Bounds.Width * 0.7f, maximumOvalWidth);
			float Height = Width * ovalAspect;
			float MaximumHeight = Bounds.Height * 0.48f;
			if (Height > MaximumHeight)
			{
				Height = MaximumHeight;
				Width = Height / ovalAspect;
			}

			return new RectF(Bounds.Center.X - Width / 2f, Bounds.Top + Bounds.Height * 0.4f - Height / 2f, Width, Height);
		}

		/// <summary>
		/// Places the document frame above the guidance card, as large as the screen allows.
		/// </summary>
		/// <param name="Bounds">The overlay bounds.</param>
		/// <param name="AspectRatio">The frame aspect ratio, as width divided by height.</param>
		/// <returns>The frame bounds.</returns>
		public static RectF GetFrame(RectF Bounds, double AspectRatio)
		{
			float Width = Math.Min(Bounds.Width - 48f, maximumFrameWidth);
			float Height = (float)(Width / AspectRatio);
			float MaximumHeight = Bounds.Height * (AspectRatio < 1 ? 0.54f : 0.44f);
			if (Height > MaximumHeight)
			{
				Height = MaximumHeight;
				Width = (float)(Height * AspectRatio);
			}

			float CenterY = Bounds.Top + Bounds.Height * (AspectRatio < 1 ? 0.43f : 0.42f);
			return new RectF(Bounds.Center.X - Width / 2f, CenterY - Height / 2f, Width, Height);
		}

		private static void DrawOval(ICanvas Canvas, RectF Bounds)
		{
			RectF Oval = GetOval(Bounds);

			PathF Scrim = new PathF();
			Scrim.AppendRectangle(Bounds);
			Scrim.AppendEllipse(Oval);
			Canvas.FillColor = ScannerColors.Scrim;
			Canvas.FillPath(Scrim, WindingMode.EvenOdd);

			Canvas.StrokeColor = ScannerColors.Neutral.WithAlpha(0.9f);
			Canvas.StrokeSize = 3f;
			Canvas.DrawEllipse(Oval);
		}

		private void DrawFrame(ICanvas Canvas, RectF Bounds)
		{
			RectF Frame = GetFrame(Bounds, this.frameAspectRatio);
			float Corner = Math.Min(Frame.Width, Frame.Height) * 0.06f;

			PathF Scrim = new PathF();
			Scrim.AppendRectangle(Bounds);
			Scrim.AppendRoundedRectangle(Frame, Corner);
			Canvas.FillColor = ScannerColors.Scrim;
			Canvas.FillPath(Scrim, WindingMode.EvenOdd);

			Canvas.StrokeColor = ScannerColors.Neutral.WithAlpha(0.45f);
			Canvas.StrokeSize = 1.5f;
			Canvas.DrawRoundedRectangle(Frame, Corner);

			float Length = Math.Min(28f, Math.Min(Frame.Width, Frame.Height) * 0.18f);
			Canvas.StrokeColor = ScannerColors.Neutral;
			Canvas.StrokeSize = 5f;
			DrawBracket(Canvas, Frame.Left, Frame.Top, Length, Length, Corner);
			DrawBracket(Canvas, Frame.Right, Frame.Top, -Length, Length, Corner);
			DrawBracket(Canvas, Frame.Right, Frame.Bottom, -Length, -Length, Corner);
			DrawBracket(Canvas, Frame.Left, Frame.Bottom, Length, -Length, Corner);
		}

		private static void DrawBracket(ICanvas Canvas, float X, float Y, float Dx, float Dy, float Corner)
		{
			// Each bracket follows the frame's rounded corner, so it reads as part of the frame.
			float Rx = Math.Sign(Dx) * Corner;
			float Ry = Math.Sign(Dy) * Corner;
			PathF Path = new PathF();
			Path.MoveTo(X + Dx, Y);
			Path.LineTo(X + Rx, Y);
			Path.QuadTo(X, Y, X, Y + Ry);
			Path.LineTo(X, Y + Dy);
			Canvas.DrawPath(Path);
		}
	}
}
