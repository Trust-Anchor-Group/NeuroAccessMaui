using SkiaSharp;

namespace NeuroAccessMaui.UI.Controls
{
	/// <summary>
	/// Draws the scenes of <see cref="NfcScanVisual"/> in a fixed design space centered on the origin.
	/// </summary>
	/// <remarks>
	/// All coordinates are design units inside a <see cref="DesignSize"/> square centered on (0, 0).
	/// The caller scales the canvas to the view and chooses the scene opacity, so each scene only depends
	/// on its own elapsed time and the values in <see cref="SceneFrame"/>.
	/// The painter reuses a small set of paints for every frame instead of allocating new Skia objects per
	/// draw call, so an instance must only be used from one thread at a time and disposed when no longer needed.
	/// </remarks>
	internal sealed class NfcScanVisualPainter : IDisposable
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

		private readonly SKPaint fillPaint = new SKPaint
		{
			IsAntialias = true,
			Style = SKPaintStyle.Fill
		};

		private readonly SKPaint strokePaint = new SKPaint
		{
			IsAntialias = true,
			Style = SKPaintStyle.Stroke,
			StrokeCap = SKStrokeCap.Round,
			StrokeJoin = SKStrokeJoin.Round
		};

		private readonly SKPaint layerPaint = new SKPaint();
		private bool disposed;

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
		/// Draws the requested scene at the given opacity.
		/// </summary>
		/// <param name="Canvas">Canvas already transformed into design space.</param>
		/// <param name="State">The scene to draw.</param>
		/// <param name="Frame">Per-frame inputs.</param>
		/// <param name="Palette">Colors to draw with.</param>
		/// <param name="Opacity">Opacity of the whole scene, from 0 to 1, used for crossfades.</param>
		public void Draw(SKCanvas Canvas, NfcScanVisualState State, SceneFrame Frame, ScenePalette Palette, float Opacity = 1)
		{
			ObjectDisposedException.ThrowIf(this.disposed, this);
			if (Opacity <= 0.001f)
				return;

			if (Opacity >= 0.999f)
			{
				this.DrawScene(Canvas, State, Frame, Palette);
				return;
			}

			// A layer fades the scene as a whole, so overlapping shapes do not show through each other.
			this.layerPaint.Color = SKColors.Black.WithAlpha((byte)(Opacity * 255));
			Canvas.SaveLayer(this.layerPaint);
			this.DrawScene(Canvas, State, Frame, Palette);
			Canvas.Restore();
		}

		/// <summary>
		/// Releases the reusable native paints.
		/// </summary>
		public void Dispose()
		{
			if (this.disposed)
				return;

			this.disposed = true;
			this.fillPaint.Dispose();
			this.strokePaint.Dispose();
			this.layerPaint.Dispose();
		}

		private void DrawScene(SKCanvas Canvas, NfcScanVisualState State, SceneFrame Frame, ScenePalette Palette)
		{
			switch (State)
			{
				case NfcScanVisualState.Intro:
					this.DrawIntro(Canvas, Frame, Palette);
					break;

				case NfcScanVisualState.Preparing:
					this.DrawPreparing(Canvas, Frame, Palette);
					break;

				case NfcScanVisualState.Searching:
					this.DrawPlacement(Canvas, Frame, Palette, true);
					break;

				case NfcScanVisualState.Paused:
					this.DrawPlacement(Canvas, Frame, Palette, false);
					break;

				case NfcScanVisualState.Reading:
					this.DrawReading(Canvas, Frame, Palette);
					break;

				case NfcScanVisualState.Success:
					this.DrawSuccess(Canvas, Frame, Palette);
					break;

				case NfcScanVisualState.Failure:
					this.DrawFailure(Canvas, Frame, Palette);
					break;
			}
		}

		private void DrawIntro(SKCanvas Canvas, SceneFrame Frame, ScenePalette Palette)
		{
			float Entrance = Frame.Animate ? CubicOut(Phase(Frame.Time, 0, 520)) : 1;
			float Bob = Frame.Animate ? 3f * Wave(Frame.Time, 3200) : 0;

			Canvas.Save();
			Canvas.Translate(0, Bob);
			Canvas.Scale(0.94f + 0.06f * Entrance);

			SKPoint Symbol = this.DrawPassport(Canvas, SKRect.Create(-64, -92, 128, 184), Palette, 0.4f);
			const float HighlightRadius = 36f;

			Canvas.DrawCircle(Symbol, HighlightRadius, this.Stroke(Fade(Palette.Accent, 0.9f), 2.5f));

			double PulseTime = Frame.Time - 450;
			if (Frame.Animate && PulseTime > 0)
			{
				float Q = (float)(PulseTime % 2200 / 2200);
				Canvas.DrawCircle(
					Symbol,
					HighlightRadius + 22 * CubicOut(Q),
					this.Stroke(Fade(Palette.Accent, 0.55f * (1 - Q)), 0.5f + 3f * (1 - Q)));
			}

			Canvas.Restore();
		}

		private void DrawPreparing(SKCanvas Canvas, SceneFrame Frame, ScenePalette Palette)
		{
			this.DrawChipCore(Canvas, Palette, 1, 1);
			Canvas.DrawCircle(0, 0, ringRadius, this.Stroke(Palette.Track, 6));

			float Rotation = Frame.Animate ? (float)(Frame.Time * 0.3 % 360) : 0;
			float Sweep = Frame.Animate
				? 40 + 200 * (0.5f - 0.5f * (float)Math.Cos(2 * Math.PI * Frame.Time / 1500))
				: 90;

			Canvas.DrawArc(RingRect(ringRadius), Rotation - 90, Sweep, false, this.Stroke(Palette.Accent, 6));
		}

		private void DrawPlacement(SKCanvas Canvas, SceneFrame Frame, ScenePalette Palette, bool IsSearching)
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
				this.DrawPassport(Canvas, DocumentRect, Palette, 0.36f);
			else
				this.DrawIdCard(Canvas, DocumentRect, Palette);

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
				this.DrawRipples(Canvas, AntennaPoint, T - 700, Palette);
			else if (IsSearching)
				this.DrawStaticRipples(Canvas, AntennaPoint, Palette);

			float AntennaGlow = IsMoving ? 0.5f + 0.5f * Wave(T, 1800) : 0;
			this.DrawPhone(Canvas, PhoneCenter, Rotation, PhoneOpacity, AntennaGlow, Frame.Placement, Palette);

			Canvas.Restore();
		}

		private void DrawReading(SKCanvas Canvas, SceneFrame Frame, ScenePalette Palette)
		{
			float Entrance = Frame.Animate ? CubicOut(Phase(Frame.Time, 0, 420)) : 1;
			float Breath = Frame.Animate ? 1 + 0.025f * Wave(Frame.Time, 1700) : 1;

			Canvas.Save();
			Canvas.Scale(0.86f + 0.14f * Entrance);

			this.DrawChipCore(Canvas, Palette, Breath, 1);

			// A faint ring expands from the chip each time data arrives, so stalls are visible.
			if (Frame.Animate && Frame.PulseAge < 650)
			{
				float Q = (float)(Frame.PulseAge / 650);
				Canvas.DrawCircle(0, 0, coreRadius + 2 + 14 * CubicOut(Q), this.Stroke(Fade(Palette.Accent, 0.4f * (1 - Q)), 2.5f));
			}

			this.DrawSegments(Canvas, Frame.Progress, segmentGap, ringStroke, Palette.Track, Palette.Accent);

			if (Frame.Animate)
			{
				for (int i = 0; i < segmentCount && i < Frame.SegmentFlashAges.Length; i++)
				{
					double Age = Frame.SegmentFlashAges[i];
					if (Age >= 500)
						continue;

					float Flash = 1 - (float)(Age / 500);
					SKPaint Bloom = this.Stroke(Fade(Palette.Accent, 0.35f * Flash), ringStroke + 10 * Flash, bloomBlur);
					Canvas.DrawArc(RingRect(ringRadius), SegmentStart(i, segmentGap), segmentSpan - segmentGap, false, Bloom);
				}
			}

			this.DrawProgressHead(Canvas, Frame, Palette);
			Canvas.Restore();
		}

		private void DrawSuccess(SKCanvas Canvas, SceneFrame Frame, ScenePalette Palette)
		{
			double U = Frame.Animate ? Frame.Time - Frame.SuccessDelay - successLeadInMs : SuccessDurationMs;

			// Until the reveal starts, keep the reading ring so the handover from reading is seamless.
			if (U < 0)
			{
				this.DrawChipCore(Canvas, Palette, 1, 1);
				this.DrawSegments(Canvas, Frame.Progress, segmentGap, ringStroke, Palette.Track, Palette.Accent);
				return;
			}

			float Close = CubicInOut(Phase(U, 0, 280));
			float Gap = segmentGap * (1 - Close);
			float RingFade = Phase(U, 320, 320);

			SKPaint Ring = this.Stroke(Fade(Palette.Accent, 1 - 0.75f * RingFade), ringStroke - 6 * RingFade);
			if (Gap < 0.5f)
				Canvas.DrawCircle(0, 0, ringRadius, Ring);
			else
			{
				for (int i = 0; i < segmentCount; i++)
					Canvas.DrawArc(RingRect(ringRadius), SegmentStart(i, Gap), segmentSpan - Gap, false, Ring);
			}

			float ChipOpacity = 1 - Phase(U, 150, 200);
			if (ChipOpacity > 0)
				this.DrawChipCore(Canvas, Palette, 1, ChipOpacity);

			float Disk = BackOut(Phase(U, 180, 420));
			if (Disk > 0)
				Canvas.DrawCircle(0, 0, successDiskRadius * Disk, this.Fill(Palette.Accent));

			float Check = CubicOut(Phase(U, 430, 380));
			if (Check > 0)
				this.DrawCheck(Canvas, Check, Palette.OnAccent);

			float Burst = Phase(U, 400, 750);
			if (Frame.Animate && Burst > 0 && Burst < 1)
				this.DrawBurst(Canvas, Burst, Palette);
		}

		private void DrawFailure(SKCanvas Canvas, SceneFrame Frame, ScenePalette Palette)
		{
			float Shake = Frame.Animate
				? 9f * (float)(Math.Sin(2 * Math.PI * Frame.Time / 90) * Math.Exp(-Frame.Time / 170))
				: 0;
			float Pop = Frame.Animate ? Math.Max(0.01f, BackOut(Phase(Frame.Time, 0, 380))) : 1;

			Canvas.Save();
			Canvas.Translate(Shake, 0);

			this.DrawSegments(Canvas, Frame.Progress, segmentGap, ringStroke, Fade(Palette.Warning, 0.18f), Fade(Palette.Warning, 0.85f));

			Canvas.Scale(Pop);
			Canvas.DrawCircle(0, 0, coreRadius, this.Fill(Fade(Palette.Warning, 0.14f)));
			Canvas.DrawLine(0, -28, 0, 6, this.Stroke(Palette.Warning, 11));
			Canvas.DrawCircle(0, 27, 6.5f, this.Fill(Palette.Warning));

			Canvas.Restore();
		}

		private SKPoint DrawPassport(SKCanvas Canvas, SKRect Rect, ScenePalette Palette, float SymbolWidthRatio)
		{
			float W = Rect.Width;
			float H = Rect.Height;
			float Corner = W * 0.1f;

			this.DrawDocumentBase(Canvas, Rect, Corner, Palette);

			float SpineX = Rect.Left + W * 0.09f;
			Canvas.DrawLine(SpineX, Rect.Top + 10, SpineX, Rect.Bottom - 10, this.Stroke(Fade(Palette.Accent, 0.25f), 2f));

			float CenterX = Rect.MidX + W * 0.045f;
			float FirstLine = Rect.Top + H * 0.14f;
			SKPaint Text = this.Stroke(Fade(Palette.Accent, 0.35f), 4f);
			Canvas.DrawLine(CenterX - W * 0.22f, FirstLine, CenterX + W * 0.22f, FirstLine, Text);
			Canvas.DrawLine(CenterX - W * 0.13f, FirstLine + 10, CenterX + W * 0.13f, FirstLine + 10, Text);

			float EmblemY = Rect.Top + H * 0.37f;
			Canvas.DrawCircle(CenterX, EmblemY, W * 0.15f, this.Stroke(Fade(Palette.Accent, 0.35f), 2.5f));
			Canvas.DrawCircle(CenterX, EmblemY, W * 0.07f, this.Fill(Fade(Palette.Accent, 0.2f)));

			SKPoint Symbol = new SKPoint(CenterX, Rect.Top + H * 0.77f);
			this.DrawChipSymbol(Canvas, Symbol, W * SymbolWidthRatio, Palette.Accent);
			return Symbol;
		}

		private void DrawIdCard(SKCanvas Canvas, SKRect Rect, ScenePalette Palette)
		{
			float W = Rect.Width;
			float H = Rect.Height;

			this.DrawDocumentBase(Canvas, Rect, H * 0.11f, Palette);

			SKRect Photo = SKRect.Create(Rect.Left + W * 0.07f, Rect.Top + H * 0.2f, W * 0.26f, H * 0.56f);
			Canvas.DrawRoundRect(Photo, 6, 6, this.Fill(Fade(Palette.Accent, 0.14f)));

			SKPaint Person = this.Fill(Fade(Palette.Accent, 0.4f));
			Canvas.DrawCircle(Photo.MidX, Photo.Top + Photo.Height * 0.42f, Photo.Width * 0.2f, Person);
			SKRect Shoulders = new SKRect(
				Photo.MidX - Photo.Width * 0.34f,
				Photo.Bottom - Photo.Height * 0.3f,
				Photo.MidX + Photo.Width * 0.34f,
				Photo.Bottom + Photo.Height * 0.3f);
			Canvas.DrawArc(Shoulders, 180, 180, true, Person);

			float LineLeft = Photo.Right + W * 0.07f;
			float LineSpan = Rect.Right - W * 0.08f - LineLeft;
			SKPaint Text = this.Stroke(Fade(Palette.Accent, 0.3f), 4f);
			for (int i = 0; i < 3; i++)
			{
				float Y = Rect.Top + H * (0.42f + 0.16f * i);
				Canvas.DrawLine(LineLeft, Y, LineLeft + LineSpan * (1 - 0.22f * i), Y, Text);
			}

			this.DrawChipSymbol(Canvas, new SKPoint(Rect.Right - W * 0.14f, Rect.Top + H * 0.2f), W * 0.15f, Palette.Accent);
		}

		private void DrawDocumentBase(SKCanvas Canvas, SKRect Rect, float Corner, ScenePalette Palette)
		{
			Canvas.DrawRoundRect(Rect, Corner, Corner, this.Fill(Palette.Surface));
			Canvas.DrawRoundRect(Rect, Corner, Corner, this.Fill(Fade(Palette.Accent, 0.14f)));
			Canvas.DrawRoundRect(Rect, Corner, Corner, this.Stroke(Fade(Palette.Accent, 0.85f), 2.5f));
		}

		private void DrawPhone(
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
			Canvas.DrawRoundRect(Body, phoneCorner, phoneCorner, this.Fill(Fade(Palette.Surface, 0.86f * Opacity)));
			Canvas.DrawRoundRect(Body, phoneCorner, phoneCorner, this.Stroke(Fade(Palette.Content, 0.85f * Opacity), 3f));

			SKRect Module = SKRect.Create(Body.Left + 9, Body.Top + 9, 24, 38);
			SKPaint Lens = this.Stroke(Fade(Palette.Content, 0.5f * Opacity), 2f);
			Canvas.DrawRoundRect(Module, 8, 8, Lens);
			Canvas.DrawCircle(Module.MidX, Module.Top + 11, 5f, Lens);
			Canvas.DrawCircle(Module.MidX, Module.Bottom - 11, 5f, Lens);

			SKPoint Antenna = AntennaOffset(Placement);
			if (AntennaGlow > 0)
				Canvas.DrawCircle(Antenna, 11, this.Fill(Fade(Palette.Accent, 0.4f * AntennaGlow * Opacity), glowBlur));

			Canvas.DrawCircle(Antenna, 4.5f, this.Fill(Fade(Palette.Accent, (0.55f + 0.45f * AntennaGlow) * Opacity)));

			Canvas.Restore();
		}

		private void DrawRipples(SKCanvas Canvas, SKPoint Center, double LocalTime, ScenePalette Palette)
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
				Canvas.DrawCircle(Center, 14 + 70 * CubicOut(Q), this.Stroke(Fade(Palette.Accent, Opacity), 1 + 2.5f * (1 - Q)));
			}
		}

		private void DrawStaticRipples(SKCanvas Canvas, SKPoint Center, ScenePalette Palette)
		{
			Canvas.DrawCircle(Center, 50, this.Stroke(Fade(Palette.Accent, 0.55f), 2.5f));
			Canvas.DrawCircle(Center, 70, this.Stroke(Fade(Palette.Accent, 0.3f), 2f));
		}

		private void DrawChipCore(SKCanvas Canvas, ScenePalette Palette, float Scale, float Opacity)
		{
			Canvas.DrawCircle(0, 0, coreRadius * Scale, this.Fill(Fade(Palette.Accent, 0.09f * Opacity)));
			this.DrawChipSymbol(Canvas, new SKPoint(0, 0), 78 * Scale, Fade(Palette.Accent, Opacity));
		}

		private void DrawChipSymbol(SKCanvas Canvas, SKPoint Center, float Width, SKColor Color)
		{
			if (chipSymbolPath is null || Width <= 0)
				return;

			Canvas.Save();
			Canvas.Translate(Center.X, Center.Y);
			Canvas.Scale(Width / chipSymbolWidth);
			Canvas.Translate(-chipSymbolCenterX, -chipSymbolCenterY);
			Canvas.DrawPath(chipSymbolPath, this.Fill(Color));
			Canvas.Restore();
		}

		private void DrawSegments(SKCanvas Canvas, float Progress, float Gap, float StrokeWidth, SKColor TrackColor, SKColor FillColor)
		{
			SKRect Rect = RingRect(ringRadius);
			float Sweep = segmentSpan - Gap;

			// Track and fill share the reusable stroke paint, so it is configured right before each arc.
			for (int i = 0; i < segmentCount; i++)
			{
				float Start = SegmentStart(i, Gap);
				Canvas.DrawArc(Rect, Start, Sweep, false, this.Stroke(TrackColor, StrokeWidth));

				float Amount = Clamp01(Progress * segmentCount - i);
				if (Amount > 0.001f)
					Canvas.DrawArc(Rect, Start, Math.Max(0.5f, Sweep * Amount), false, this.Stroke(FillColor, StrokeWidth));
			}
		}

		private void DrawProgressHead(SKCanvas Canvas, SceneFrame Frame, ScenePalette Palette)
		{
			float Units = Frame.Progress * segmentCount;
			int Active = (int)Math.Floor(Units);
			if (Active >= segmentCount)
				return;

			float Amount = Units - Active;
			float Angle = SegmentStart(Active, segmentGap) + (segmentSpan - segmentGap) * Amount;
			SKPoint Head = PointOnCircle(ringRadius, Angle);
			float GlowOpacity = Frame.Animate ? 0.55f + 0.25f * Wave(Frame.Time, 900) : 0.6f;

			Canvas.DrawCircle(Head, 10, this.Fill(Fade(Palette.Accent, GlowOpacity), glowBlur));
			Canvas.DrawCircle(Head, 3.2f, this.Fill(Fade(Palette.OnAccent, 0.95f)));
		}

		private void DrawCheck(SKCanvas Canvas, float Amount, SKColor Color)
		{
			SKPoint Start = new SKPoint(-30, 2);
			SKPoint Corner = new SKPoint(-10, 22);
			SKPoint End = new SKPoint(32, -22);
			float FirstLength = Distance(Start, Corner);
			float SecondLength = Distance(Corner, End);
			float Drawn = Amount * (FirstLength + SecondLength);

			// Two round-capped lines meet in a round joint, so the check mark draws in without building a path.
			SKPaint Paint = this.Stroke(Color, 10);
			if (Drawn <= FirstLength)
			{
				SKPoint Tip = Lerp(Start, Corner, Drawn / FirstLength);
				Canvas.DrawLine(Start.X, Start.Y, Tip.X, Tip.Y, Paint);
				return;
			}

			SKPoint SecondTip = Lerp(Corner, End, (Drawn - FirstLength) / SecondLength);
			Canvas.DrawLine(Start.X, Start.Y, Corner.X, Corner.Y, Paint);
			Canvas.DrawLine(Corner.X, Corner.Y, SecondTip.X, SecondTip.Y, Paint);
		}

		private void DrawBurst(SKCanvas Canvas, float Burst, ScenePalette Palette)
		{
			float Spread = CubicOut(Burst);
			float Remaining = 1 - Burst;

			Canvas.DrawCircle(0, 0, 84 + 28 * Spread, this.Stroke(Fade(Palette.Accent, 0.5f * Remaining), 0.5f + 5 * Remaining));

			SKPaint Particle = this.Fill(Fade(Palette.Accent, Remaining));
			for (int i = 0; i < 12; i++)
			{
				bool IsLarge = i % 2 == 0;
				float Radius = 96 + (IsLarge ? 20 : 14) * Spread;
				SKPoint Position = PointOnCircle(Radius, i * 30 + 15);
				Canvas.DrawCircle(Position, 0.5f + (IsLarge ? 4.5f : 3f) * Remaining, Particle);
			}
		}

		/// <summary>
		/// Configures and returns the shared fill paint. The result is only valid until the next paint request.
		/// </summary>
		private SKPaint Fill(SKColor Color, SKMaskFilter? MaskFilter = null)
		{
			this.fillPaint.Color = Color;
			this.fillPaint.MaskFilter = MaskFilter;
			return this.fillPaint;
		}

		/// <summary>
		/// Configures and returns the shared stroke paint. The result is only valid until the next paint request.
		/// </summary>
		private SKPaint Stroke(SKColor Color, float Width, SKMaskFilter? MaskFilter = null)
		{
			this.strokePaint.Color = Color;
			this.strokePaint.StrokeWidth = Width;
			this.strokePaint.MaskFilter = MaskFilter;
			return this.strokePaint;
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
	}
}
