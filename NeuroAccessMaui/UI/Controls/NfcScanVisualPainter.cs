using SkiaSharp;

namespace NeuroAccessMaui.UI.Controls
{
	/// <summary>
	/// Draws the scenes of <see cref="NfcScanVisual"/> in a fixed design space centered on the origin.
	/// </summary>
	/// <remarks>
	/// All coordinates are design units inside a <see cref="DesignSize"/> square centered on (0, 0).
	/// The caller scales the canvas to the view and handles crossfades between scenes, so each scene
	/// only depends on its own elapsed time and the values in <see cref="SceneFrame"/>.
	/// </remarks>
	internal static class NfcScanVisualPainter
	{
		/// <summary>
		/// The width and height of the square design space, in design units.
		/// </summary>
		public const float DesignSize = 240f;

		/// <summary>
		/// The time the success sequence takes after its reveal delay, in milliseconds.
		/// </summary>
		public const double SuccessDurationMs = 1500;

		/// <summary>
		/// The time the failure entrance and shake take, in milliseconds.
		/// </summary>
		public const double FailureDurationMs = 700;

		private const int segmentCount = 4;
		private const float segmentSpan = 360f / segmentCount;
		private const float segmentGap = 16f;
		private const float ringRadius = 92f;
		private const float ringStroke = 12f;
		private const float coreRadius = 70f;
		private const float successDiskRadius = 76f;
		private const double successLeadInMs = 250;
		private const double searchingEntranceMs = 950;
		private const float phoneWidth = 76f;
		private const float phoneHeight = 146f;
		private const float phoneCorner = 17f;

		// Path data of the ICAO chip symbol from Resources/Raw/Vectors/icao_chip.svg.
		private const string chipSymbolPathData =
			"M-111.5625 24.75V136.125H23.539306A82.5 82.5 0 0 1 105 66A82.5 82.5 0 0 1 186.5 136.125H321.5625V24.75Z" +
			"M105 90.75A57.75 57.75 0 0 0 47.25 148.5A57.75 57.75 0 0 0 105 206.25A57.75 57.75 0 0 0 162.75 148.5A57.75 57.75 0 0 0 105 90.75Z" +
			"M-111.5625 160.875V272.25H321.5625V160.875H186.46068A82.5 82.5 0 0 1 105 231A82.5 82.5 0 0 1 23.5 160.875Z";
		private const float chipSymbolWidth = 433.125f;
		private const float chipSymbolCenterX = 105f;
		private const float chipSymbolCenterY = 148.5f;

		private static readonly SKPath? chipSymbolPath = SKPath.ParseSvgPathData(chipSymbolPathData);
		private static readonly SKMaskFilter glowBlur = SKMaskFilter.CreateBlur(SKBlurStyle.Normal, 5f);
		private static readonly SKMaskFilter bloomBlur = SKMaskFilter.CreateBlur(SKBlurStyle.Normal, 4f);

		/// <summary>
		/// Colors used to draw a scene, resolved from the control's theme-bound properties.
		/// </summary>
		/// <param name="Accent">Brand accent used for documents, rings, and the success disk.</param>
		/// <param name="OnAccent">Color drawn on top of the accent, such as the check mark.</param>
		/// <param name="Content">Primary content color used for the phone outline.</param>
		/// <param name="Track">Neutral color used for empty ring segments.</param>
		/// <param name="Surface">Elevated surface color used to fill documents and the phone.</param>
		/// <param name="Warning">Warning color used for failures.</param>
		internal readonly record struct ScenePalette(
			SKColor Accent,
			SKColor OnAccent,
			SKColor Content,
			SKColor Track,
			SKColor Surface,
			SKColor Warning);

		/// <summary>
		/// Per-frame inputs for drawing a scene.
		/// </summary>
		/// <param name="Time">Milliseconds since the scene started, including any skipped entrance time.</param>
		/// <param name="Progress">Displayed chip read progress, from 0 to 1.</param>
		/// <param name="IsPassport">Whether to draw a passport rather than an ID card.</param>
		/// <param name="Placement">Where the phone's NFC antenna is located.</param>
		/// <param name="Animate">Whether motion is allowed; false draws settled, static frames.</param>
		/// <param name="PulseAge">Milliseconds since chip data last arrived.</param>
		/// <param name="SuccessDelay">Milliseconds to hold the full ring before revealing success.</param>
		/// <param name="SegmentFlashAges">Milliseconds since each ring segment completed.</param>
		internal readonly record struct SceneFrame(
			double Time,
			float Progress,
			bool IsPassport,
			NfcAntennaPlacement Placement,
			bool Animate,
			double PulseAge,
			double SuccessDelay,
			double[] SegmentFlashAges);

		/// <summary>
		/// Draws the requested scene.
		/// </summary>
		/// <param name="Canvas">Canvas already transformed into design space.</param>
		/// <param name="State">The scene to draw.</param>
		/// <param name="Frame">Per-frame inputs.</param>
		/// <param name="Palette">Colors to draw with.</param>
		public static void Draw(SKCanvas Canvas, NfcScanVisualState State, SceneFrame Frame, ScenePalette Palette)
		{
			switch (State)
			{
				case NfcScanVisualState.Intro:
					DrawIntro(Canvas, Frame, Palette);
					break;

				case NfcScanVisualState.Preparing:
					DrawPreparing(Canvas, Frame, Palette);
					break;

				case NfcScanVisualState.Searching:
					DrawPlacement(Canvas, Frame, Palette, true);
					break;

				case NfcScanVisualState.Paused:
					DrawPlacement(Canvas, Frame, Palette, false);
					break;

				case NfcScanVisualState.Reading:
					DrawReading(Canvas, Frame, Palette);
					break;

				case NfcScanVisualState.Success:
					DrawSuccess(Canvas, Frame, Palette);
					break;

				case NfcScanVisualState.Failure:
					DrawFailure(Canvas, Frame, Palette);
					break;
			}
		}

		private static void DrawIntro(SKCanvas Canvas, SceneFrame Frame, ScenePalette Palette)
		{
			float Entrance = Frame.Animate ? CubicOut(Phase(Frame.Time, 0, 520)) : 1;
			float Bob = Frame.Animate ? 3f * Wave(Frame.Time, 3200) : 0;

			Canvas.Save();
			Canvas.Translate(0, Bob);
			Canvas.Scale(0.94f + 0.06f * Entrance);

			SKPoint Symbol = DrawPassport(Canvas, SKRect.Create(-64, -92, 128, 184), Palette, 0.4f);
			const float HighlightRadius = 36f;

			using (SKPaint Highlight = StrokePaint(Fade(Palette.Accent, 0.9f), 2.5f))
				Canvas.DrawCircle(Symbol, HighlightRadius, Highlight);

			double PulseTime = Frame.Time - 450;
			if (Frame.Animate && PulseTime > 0)
			{
				float Q = (float)(PulseTime % 2200 / 2200);
				using SKPaint Pulse = StrokePaint(Fade(Palette.Accent, 0.55f * (1 - Q)), 0.5f + 3f * (1 - Q));
				Canvas.DrawCircle(Symbol, HighlightRadius + 22 * CubicOut(Q), Pulse);
			}

			Canvas.Restore();
		}

		private static void DrawPreparing(SKCanvas Canvas, SceneFrame Frame, ScenePalette Palette)
		{
			DrawChipCore(Canvas, Palette, 1, 1);

			using (SKPaint Track = StrokePaint(Palette.Track, 6))
				Canvas.DrawCircle(0, 0, ringRadius, Track);

			float Rotation = Frame.Animate ? (float)(Frame.Time * 0.3 % 360) : 0;
			float Sweep = Frame.Animate
				? 40 + 200 * (0.5f - 0.5f * (float)Math.Cos(2 * Math.PI * Frame.Time / 1500))
				: 90;

			using SKPaint Arc = StrokePaint(Palette.Accent, 6);
			Canvas.DrawArc(RingRect(ringRadius), Rotation - 90, Sweep, false, Arc);
		}

		private static void DrawPlacement(SKCanvas Canvas, SceneFrame Frame, ScenePalette Palette, bool IsSearching)
		{
			bool IsMoving = IsSearching && Frame.Animate;
			SKSize DocumentSize = Frame.IsPassport ? new SKSize(124, 172) : new SKSize(170, 108);
			SKRect DocumentRect = SKRect.Create(-DocumentSize.Width / 2, -DocumentSize.Height / 2, DocumentSize.Width, DocumentSize.Height);

			// The phone rests so that its antenna sits over the middle of the document.
			SKPoint Antenna = AntennaOffset(Frame.Placement);
			SKPoint Rest = new SKPoint(-Antenna.X, -Antenna.Y);
			SKRect PhoneRect = SKRect.Create(Rest.X - phoneWidth / 2, Rest.Y - phoneHeight / 2, phoneWidth, phoneHeight);

			SKRect Bounds = SKRect.Union(DocumentRect, PhoneRect);
			Bounds.Inflate(14, 14);
			float Fit = Math.Min(1f, Math.Min((DesignSize - 20) / Bounds.Width, (DesignSize - 20) / Bounds.Height));

			Canvas.Save();
			Canvas.Scale(Fit);
			Canvas.Translate(-Bounds.MidX, -Bounds.MidY);

			if (Frame.IsPassport)
				DrawPassport(Canvas, DocumentRect, Palette, 0.36f);
			else
				DrawIdCard(Canvas, DocumentRect, Palette);

			double T = Frame.Time;
			float Entrance = IsMoving ? CubicOut(Phase(T, 100, 850)) : 1;
			float PhoneOpacity = IsMoving ? Phase(T, 100, 280) : 1;
			float DriftRamp = IsMoving ? Phase(T, searchingEntranceMs, 800) : 0;
			double DriftTime = T - searchingEntranceMs;

			// A slow figure-eight drift hints that the user should move the document gently.
			float DriftX = 5f * Wave(DriftTime, 3400) * DriftRamp;
			float DriftY = 3.5f * Wave(DriftTime, 1700) * DriftRamp;

			// A phone read from its back is lowered onto the document; one read from its top edge is raised up to it.
			SKPoint Approach = Frame.Placement == NfcAntennaPlacement.TopEdge ? new SKPoint(36, 70) : new SKPoint(56, -64);
			float Rotation = (Frame.Placement == NfcAntennaPlacement.TopEdge ? 10 : 14) * (1 - Entrance);
			SKPoint PhoneCenter = new SKPoint(
				Rest.X + Approach.X * (1 - Entrance) + DriftX,
				Rest.Y + Approach.Y * (1 - Entrance) + DriftY);
			SKPoint RotatedAntenna = Rotate(Antenna, Rotation);
			SKPoint AntennaPoint = new SKPoint(PhoneCenter.X + RotatedAntenna.X, PhoneCenter.Y + RotatedAntenna.Y);

			if (IsMoving)
				DrawRipples(Canvas, AntennaPoint, T - 700, Palette);
			else if (IsSearching)
				DrawStaticRipples(Canvas, AntennaPoint, Palette);

			float AntennaGlow = IsMoving ? 0.5f + 0.5f * Wave(T, 1800) : 0;
			DrawPhone(Canvas, PhoneCenter, Rotation, PhoneOpacity, AntennaGlow, Frame.Placement, Palette);

			Canvas.Restore();
		}

		private static void DrawReading(SKCanvas Canvas, SceneFrame Frame, ScenePalette Palette)
		{
			float Entrance = Frame.Animate ? CubicOut(Phase(Frame.Time, 0, 420)) : 1;
			float Breath = Frame.Animate ? 1 + 0.025f * Wave(Frame.Time, 1700) : 1;

			Canvas.Save();
			Canvas.Scale(0.86f + 0.14f * Entrance);

			DrawChipCore(Canvas, Palette, Breath, 1);

			// A faint ring expands from the chip each time data arrives, so stalls are visible.
			if (Frame.Animate && Frame.PulseAge < 650)
			{
				float Q = (float)(Frame.PulseAge / 650);
				using SKPaint Pulse = StrokePaint(Fade(Palette.Accent, 0.4f * (1 - Q)), 2.5f);
				Canvas.DrawCircle(0, 0, coreRadius + 2 + 14 * CubicOut(Q), Pulse);
			}

			DrawSegments(Canvas, Frame.Progress, segmentGap, ringStroke, Palette.Track, Palette.Accent);

			if (Frame.Animate)
			{
				for (int i = 0; i < segmentCount && i < Frame.SegmentFlashAges.Length; i++)
				{
					double Age = Frame.SegmentFlashAges[i];
					if (Age >= 500)
						continue;

					float Flash = 1 - (float)(Age / 500);
					using SKPaint Bloom = StrokePaint(Fade(Palette.Accent, 0.35f * Flash), ringStroke + 10 * Flash);
					Bloom.MaskFilter = bloomBlur;
					Canvas.DrawArc(RingRect(ringRadius), SegmentStart(i, segmentGap), segmentSpan - segmentGap, false, Bloom);
				}
			}

			DrawProgressHead(Canvas, Frame, Palette);
			Canvas.Restore();
		}

		private static void DrawSuccess(SKCanvas Canvas, SceneFrame Frame, ScenePalette Palette)
		{
			double U = Frame.Animate ? Frame.Time - Frame.SuccessDelay - successLeadInMs : SuccessDurationMs;

			// Until the reveal starts, keep the reading ring so the handover from reading is seamless.
			if (U < 0)
			{
				DrawChipCore(Canvas, Palette, 1, 1);
				DrawSegments(Canvas, Frame.Progress, segmentGap, ringStroke, Palette.Track, Palette.Accent);
				return;
			}

			float Close = CubicInOut(Phase(U, 0, 280));
			float Gap = segmentGap * (1 - Close);
			float RingFade = Phase(U, 320, 320);

			using (SKPaint Ring = StrokePaint(Fade(Palette.Accent, 1 - 0.75f * RingFade), ringStroke - 6 * RingFade))
			{
				if (Gap < 0.5f)
					Canvas.DrawCircle(0, 0, ringRadius, Ring);
				else
				{
					for (int i = 0; i < segmentCount; i++)
						Canvas.DrawArc(RingRect(ringRadius), SegmentStart(i, Gap), segmentSpan - Gap, false, Ring);
				}
			}

			float ChipOpacity = 1 - Phase(U, 150, 200);
			if (ChipOpacity > 0)
				DrawChipCore(Canvas, Palette, 1, ChipOpacity);

			float Disk = BackOut(Phase(U, 180, 420));
			if (Disk > 0)
			{
				using SKPaint Fill = FillPaint(Palette.Accent);
				Canvas.DrawCircle(0, 0, successDiskRadius * Disk, Fill);
			}

			float Check = CubicOut(Phase(U, 430, 380));
			if (Check > 0)
				DrawCheck(Canvas, Check, Palette.OnAccent);

			float Burst = Phase(U, 400, 750);
			if (Frame.Animate && Burst > 0 && Burst < 1)
				DrawBurst(Canvas, Burst, Palette);
		}

		private static void DrawFailure(SKCanvas Canvas, SceneFrame Frame, ScenePalette Palette)
		{
			float Shake = Frame.Animate
				? 9f * (float)(Math.Sin(2 * Math.PI * Frame.Time / 90) * Math.Exp(-Frame.Time / 170))
				: 0;
			float Pop = Frame.Animate ? Math.Max(0.01f, BackOut(Phase(Frame.Time, 0, 380))) : 1;

			Canvas.Save();
			Canvas.Translate(Shake, 0);

			DrawSegments(Canvas, Frame.Progress, segmentGap, ringStroke, Fade(Palette.Warning, 0.18f), Fade(Palette.Warning, 0.85f));

			Canvas.Scale(Pop);
			using (SKPaint Disk = FillPaint(Fade(Palette.Warning, 0.14f)))
				Canvas.DrawCircle(0, 0, coreRadius, Disk);

			using (SKPaint Mark = StrokePaint(Palette.Warning, 11))
				Canvas.DrawLine(0, -28, 0, 6, Mark);

			using (SKPaint Dot = FillPaint(Palette.Warning))
				Canvas.DrawCircle(0, 27, 6.5f, Dot);

			Canvas.Restore();
		}

		private static SKPoint DrawPassport(SKCanvas Canvas, SKRect Rect, ScenePalette Palette, float SymbolWidthRatio)
		{
			float W = Rect.Width;
			float H = Rect.Height;
			float Corner = W * 0.1f;

			DrawDocumentBase(Canvas, Rect, Corner, Palette);

			float SpineX = Rect.Left + W * 0.09f;
			using (SKPaint Spine = StrokePaint(Fade(Palette.Accent, 0.25f), 2f))
				Canvas.DrawLine(SpineX, Rect.Top + 10, SpineX, Rect.Bottom - 10, Spine);

			float CenterX = Rect.MidX + W * 0.045f;
			using (SKPaint Text = StrokePaint(Fade(Palette.Accent, 0.35f), 4f))
			{
				float FirstLine = Rect.Top + H * 0.14f;
				Canvas.DrawLine(CenterX - W * 0.22f, FirstLine, CenterX + W * 0.22f, FirstLine, Text);
				Canvas.DrawLine(CenterX - W * 0.13f, FirstLine + 10, CenterX + W * 0.13f, FirstLine + 10, Text);
			}

			float EmblemY = Rect.Top + H * 0.37f;
			using (SKPaint Emblem = StrokePaint(Fade(Palette.Accent, 0.35f), 2.5f))
				Canvas.DrawCircle(CenterX, EmblemY, W * 0.15f, Emblem);

			using (SKPaint EmblemCore = FillPaint(Fade(Palette.Accent, 0.2f)))
				Canvas.DrawCircle(CenterX, EmblemY, W * 0.07f, EmblemCore);

			SKPoint Symbol = new SKPoint(CenterX, Rect.Top + H * 0.77f);
			DrawChipSymbol(Canvas, Symbol, W * SymbolWidthRatio, Palette.Accent);
			return Symbol;
		}

		private static void DrawIdCard(SKCanvas Canvas, SKRect Rect, ScenePalette Palette)
		{
			float W = Rect.Width;
			float H = Rect.Height;

			DrawDocumentBase(Canvas, Rect, H * 0.11f, Palette);

			SKRect Photo = SKRect.Create(Rect.Left + W * 0.07f, Rect.Top + H * 0.2f, W * 0.26f, H * 0.56f);
			using (SKPaint PhotoFill = FillPaint(Fade(Palette.Accent, 0.14f)))
				Canvas.DrawRoundRect(Photo, 6, 6, PhotoFill);

			using (SKPaint Person = FillPaint(Fade(Palette.Accent, 0.4f)))
			{
				Canvas.DrawCircle(Photo.MidX, Photo.Top + Photo.Height * 0.42f, Photo.Width * 0.2f, Person);
				SKRect Shoulders = new SKRect(
					Photo.MidX - Photo.Width * 0.34f,
					Photo.Bottom - Photo.Height * 0.3f,
					Photo.MidX + Photo.Width * 0.34f,
					Photo.Bottom + Photo.Height * 0.3f);
				Canvas.DrawArc(Shoulders, 180, 180, true, Person);
			}

			float LineLeft = Photo.Right + W * 0.07f;
			float LineSpan = Rect.Right - W * 0.08f - LineLeft;
			using (SKPaint Text = StrokePaint(Fade(Palette.Accent, 0.3f), 4f))
			{
				for (int i = 0; i < 3; i++)
				{
					float Y = Rect.Top + H * (0.42f + 0.16f * i);
					Canvas.DrawLine(LineLeft, Y, LineLeft + LineSpan * (1 - 0.22f * i), Y, Text);
				}
			}

			DrawChipSymbol(Canvas, new SKPoint(Rect.Right - W * 0.14f, Rect.Top + H * 0.2f), W * 0.15f, Palette.Accent);
		}

		private static void DrawDocumentBase(SKCanvas Canvas, SKRect Rect, float Corner, ScenePalette Palette)
		{
			using (SKPaint Base = FillPaint(Palette.Surface))
				Canvas.DrawRoundRect(Rect, Corner, Corner, Base);

			using (SKPaint Tint = FillPaint(Fade(Palette.Accent, 0.14f)))
				Canvas.DrawRoundRect(Rect, Corner, Corner, Tint);

			using (SKPaint Outline = StrokePaint(Fade(Palette.Accent, 0.85f), 2.5f))
				Canvas.DrawRoundRect(Rect, Corner, Corner, Outline);
		}

		private static void DrawPhone(
			SKCanvas Canvas,
			SKPoint Center,
			float Rotation,
			float Opacity,
			float AntennaGlow,
			NfcAntennaPlacement Placement,
			ScenePalette Palette)
		{
			if (Opacity <= 0)
				return;

			Canvas.Save();
			Canvas.Translate(Center.X, Center.Y);
			Canvas.RotateDegrees(Rotation);

			SKRect Body = SKRect.Create(-phoneWidth / 2, -phoneHeight / 2, phoneWidth, phoneHeight);

			// A slightly translucent back lets the document show through, hinting at the chip underneath.
			using (SKPaint Fill = FillPaint(Fade(Palette.Surface, 0.86f * Opacity)))
				Canvas.DrawRoundRect(Body, phoneCorner, phoneCorner, Fill);

			using (SKPaint Outline = StrokePaint(Fade(Palette.Content, 0.85f * Opacity), 3f))
				Canvas.DrawRoundRect(Body, phoneCorner, phoneCorner, Outline);

			SKRect Module = SKRect.Create(Body.Left + 9, Body.Top + 9, 24, 38);
			using (SKPaint Lens = StrokePaint(Fade(Palette.Content, 0.5f * Opacity), 2f))
			{
				Canvas.DrawRoundRect(Module, 8, 8, Lens);
				Canvas.DrawCircle(Module.MidX, Module.Top + 11, 5f, Lens);
				Canvas.DrawCircle(Module.MidX, Module.Bottom - 11, 5f, Lens);
			}

			SKPoint Antenna = AntennaOffset(Placement);
			if (AntennaGlow > 0)
			{
				using SKPaint Glow = FillPaint(Fade(Palette.Accent, 0.4f * AntennaGlow * Opacity));
				Glow.MaskFilter = glowBlur;
				Canvas.DrawCircle(Antenna, 11, Glow);
			}

			using (SKPaint Dot = FillPaint(Fade(Palette.Accent, (0.55f + 0.45f * AntennaGlow) * Opacity)))
				Canvas.DrawCircle(Antenna, 4.5f, Dot);

			Canvas.Restore();
		}

		private static void DrawRipples(SKCanvas Canvas, SKPoint Center, double LocalTime, ScenePalette Palette)
		{
			if (LocalTime < 0)
				return;

			float FadeIn = Phase(LocalTime, 0, 400);
			for (int i = 0; i < 3; i++)
			{
				double RingTime = LocalTime - i * 600;
				if (RingTime < 0)
					continue;

				float Q = (float)(RingTime % 1800 / 1800);
				float Opacity = (1 - Q) * (1 - Q) * 0.8f * FadeIn;
				using SKPaint Ring = StrokePaint(Fade(Palette.Accent, Opacity), 1 + 2.5f * (1 - Q));
				Canvas.DrawCircle(Center, 14 + 70 * CubicOut(Q), Ring);
			}
		}

		private static void DrawStaticRipples(SKCanvas Canvas, SKPoint Center, ScenePalette Palette)
		{
			using (SKPaint Inner = StrokePaint(Fade(Palette.Accent, 0.55f), 2.5f))
				Canvas.DrawCircle(Center, 50, Inner);

			using (SKPaint Outer = StrokePaint(Fade(Palette.Accent, 0.3f), 2f))
				Canvas.DrawCircle(Center, 70, Outer);
		}

		private static void DrawChipCore(SKCanvas Canvas, ScenePalette Palette, float Scale, float Opacity)
		{
			using (SKPaint Disk = FillPaint(Fade(Palette.Accent, 0.09f * Opacity)))
				Canvas.DrawCircle(0, 0, coreRadius * Scale, Disk);

			DrawChipSymbol(Canvas, new SKPoint(0, 0), 78 * Scale, Fade(Palette.Accent, Opacity));
		}

		private static void DrawChipSymbol(SKCanvas Canvas, SKPoint Center, float Width, SKColor Color)
		{
			if (chipSymbolPath is null || Width <= 0)
				return;

			using SKPaint Paint = FillPaint(Color);
			Canvas.Save();
			Canvas.Translate(Center.X, Center.Y);
			Canvas.Scale(Width / chipSymbolWidth);
			Canvas.Translate(-chipSymbolCenterX, -chipSymbolCenterY);
			Canvas.DrawPath(chipSymbolPath, Paint);
			Canvas.Restore();
		}

		private static void DrawSegments(SKCanvas Canvas, float Progress, float Gap, float StrokeWidth, SKColor TrackColor, SKColor FillColor)
		{
			SKRect Rect = RingRect(ringRadius);
			float Sweep = segmentSpan - Gap;

			using SKPaint Track = StrokePaint(TrackColor, StrokeWidth);
			using SKPaint Fill = StrokePaint(FillColor, StrokeWidth);

			for (int i = 0; i < segmentCount; i++)
			{
				float Start = SegmentStart(i, Gap);
				Canvas.DrawArc(Rect, Start, Sweep, false, Track);

				float Amount = Clamp01(Progress * segmentCount - i);
				if (Amount > 0.001f)
					Canvas.DrawArc(Rect, Start, Math.Max(0.5f, Sweep * Amount), false, Fill);
			}
		}

		private static void DrawProgressHead(SKCanvas Canvas, SceneFrame Frame, ScenePalette Palette)
		{
			float Units = Frame.Progress * segmentCount;
			int Active = (int)Math.Floor(Units);
			if (Active >= segmentCount)
				return;

			float Amount = Units - Active;
			float Angle = SegmentStart(Active, segmentGap) + (segmentSpan - segmentGap) * Amount;
			SKPoint Head = PointOnCircle(ringRadius, Angle);
			float GlowOpacity = Frame.Animate ? 0.55f + 0.25f * Wave(Frame.Time, 900) : 0.6f;

			using (SKPaint Glow = FillPaint(Fade(Palette.Accent, GlowOpacity)))
			{
				Glow.MaskFilter = glowBlur;
				Canvas.DrawCircle(Head, 10, Glow);
			}

			using (SKPaint Core = FillPaint(Fade(Palette.OnAccent, 0.95f)))
				Canvas.DrawCircle(Head, 3.2f, Core);
		}

		private static void DrawCheck(SKCanvas Canvas, float Amount, SKColor Color)
		{
			SKPoint Start = new SKPoint(-30, 2);
			SKPoint Corner = new SKPoint(-10, 22);
			SKPoint End = new SKPoint(32, -22);
			float FirstLength = Distance(Start, Corner);
			float SecondLength = Distance(Corner, End);
			float Drawn = Amount * (FirstLength + SecondLength);

			using SKPathBuilder Builder = new SKPathBuilder();
			Builder.MoveTo(Start);
			if (Drawn <= FirstLength)
				Builder.LineTo(Lerp(Start, Corner, Drawn / FirstLength));
			else
			{
				Builder.LineTo(Corner);
				Builder.LineTo(Lerp(Corner, End, (Drawn - FirstLength) / SecondLength));
			}

			using SKPath Path = Builder.Detach();
			using SKPaint Paint = StrokePaint(Color, 10);
			Canvas.DrawPath(Path, Paint);
		}

		private static void DrawBurst(SKCanvas Canvas, float Burst, ScenePalette Palette)
		{
			float Spread = CubicOut(Burst);
			float Remaining = 1 - Burst;

			using (SKPaint Ring = StrokePaint(Fade(Palette.Accent, 0.5f * Remaining), 0.5f + 5 * Remaining))
				Canvas.DrawCircle(0, 0, 84 + 28 * Spread, Ring);

			using SKPaint Particle = FillPaint(Fade(Palette.Accent, Remaining));
			for (int i = 0; i < 12; i++)
			{
				bool IsLarge = i % 2 == 0;
				float Radius = 96 + (IsLarge ? 20 : 14) * Spread;
				SKPoint Position = PointOnCircle(Radius, i * 30 + 15);
				Canvas.DrawCircle(Position, 0.5f + (IsLarge ? 4.5f : 3f) * Remaining, Particle);
			}
		}

		private static SKPoint AntennaOffset(NfcAntennaPlacement Placement)
		{
			return Placement == NfcAntennaPlacement.TopEdge
				? new SKPoint(8, -phoneHeight / 2 + 12)
				: new SKPoint(0, 0);
		}

		private static float SegmentStart(int Index, float Gap) => -90 + Index * segmentSpan + Gap / 2;

		private static SKRect RingRect(float Radius) => new SKRect(-Radius, -Radius, Radius, Radius);

		private static SKPoint PointOnCircle(float Radius, float Degrees)
		{
			double Radians = Degrees * Math.PI / 180;
			return new SKPoint((float)(Radius * Math.Cos(Radians)), (float)(Radius * Math.Sin(Radians)));
		}

		private static SKPoint Rotate(SKPoint Point, float Degrees)
		{
			double Radians = Degrees * Math.PI / 180;
			float Cos = (float)Math.Cos(Radians);
			float Sin = (float)Math.Sin(Radians);
			return new SKPoint(Point.X * Cos - Point.Y * Sin, Point.X * Sin + Point.Y * Cos);
		}

		private static SKPoint Lerp(SKPoint From, SKPoint To, float Amount) =>
			new SKPoint(From.X + (To.X - From.X) * Amount, From.Y + (To.Y - From.Y) * Amount);

		private static float Distance(SKPoint From, SKPoint To)
		{
			float Dx = To.X - From.X;
			float Dy = To.Y - From.Y;
			return (float)Math.Sqrt(Dx * Dx + Dy * Dy);
		}

		private static float Clamp01(double Value) => (float)Math.Clamp(Value, 0, 1);

		private static float Phase(double Time, double Start, double Duration) => Clamp01((Time - Start) / Duration);

		private static float Wave(double Time, double PeriodMs) => (float)Math.Sin(2 * Math.PI * Time / PeriodMs);

		private static float CubicOut(float X)
		{
			float Inverse = 1 - X;
			return 1 - Inverse * Inverse * Inverse;
		}

		private static float CubicInOut(float X)
		{
			if (X < 0.5f)
				return 4 * X * X * X;

			float Inverse = -2 * X + 2;
			return 1 - Inverse * Inverse * Inverse / 2;
		}

		private static float BackOut(float X)
		{
			const float Overshoot = 1.70158f;
			float T = X - 1;
			return 1 + (Overshoot + 1) * T * T * T + Overshoot * T * T;
		}

		private static SKColor Fade(SKColor Color, float Opacity) =>
			Color.WithAlpha((byte)Math.Clamp(Color.Alpha * Opacity, 0, 255));

		private static SKPaint FillPaint(SKColor Color) => new SKPaint
		{
			IsAntialias = true,
			Style = SKPaintStyle.Fill,
			Color = Color
		};

		private static SKPaint StrokePaint(SKColor Color, float Width) => new SKPaint
		{
			IsAntialias = true,
			Style = SKPaintStyle.Stroke,
			StrokeWidth = Width,
			StrokeCap = SKStrokeCap.Round,
			StrokeJoin = SKStrokeJoin.Round,
			Color = Color
		};
	}
}
