using NeuroAccessMaui.Services;
using NeuroFeatures;
using SkiaSharp;
using Svg.Skia;

namespace NeuroAccessMaui.UI.Pages.Wallet.MyWallet.ObjectModels
{
	/// <summary>
	/// A token's glyph, sized to fit a square box. Tokens without a glyph, or with a glyph that can't be read, get the
	/// token placeholder instead.
	/// </summary>
	public sealed class TokenGlyph
	{
		/// <summary>
		/// Size of the box glyphs fit in, in token lists.
		/// </summary>
		public const double ListBoxSize = 40;

		/// <summary>
		/// Size of the box the glyph fits in, in token details.
		/// </summary>
		public const double DetailsBoxSize = 120;

		/// <summary>
		/// The placeholder is drawn smaller than the box, so it doesn't look heavier than a real glyph.
		/// </summary>
		private const double PlaceholderScale = 0.6;

		/// <summary>
		/// SVG glyphs are rendered at this multiple of their display size, so they stay sharp on high-density screens.
		/// </summary>
		private const double SvgRenderScale = 3;

		private TokenGlyph(ImageSource? Image, double Width, double Height)
		{
			this.Image = Image;
			this.Width = Width;
			this.Height = Height;
		}

		/// <summary>
		/// The glyph image, or null when the placeholder is shown.
		/// </summary>
		public ImageSource? Image { get; }

		/// <summary>
		/// Display width, in device-independent units.
		/// </summary>
		public double Width { get; }

		/// <summary>
		/// Display height, in device-independent units.
		/// </summary>
		public double Height { get; }

		/// <summary>
		/// If the token placeholder is shown instead of a glyph.
		/// </summary>
		public bool IsPlaceholder => this.Image is null;

		/// <summary>
		/// Creates the placeholder for a box.
		/// </summary>
		/// <param name="BoxSize">Size of the square box.</param>
		/// <returns>The placeholder.</returns>
		public static TokenGlyph Placeholder(double BoxSize)
		{
			return new TokenGlyph(null, BoxSize * PlaceholderScale, BoxSize * PlaceholderScale);
		}

		/// <summary>
		/// Gets the scale that fits a glyph in a square box, without enlarging it.
		/// </summary>
		/// <param name="Width">Glyph width.</param>
		/// <param name="Height">Glyph height.</param>
		/// <param name="BoxSize">Size of the square box.</param>
		/// <returns>The scale, or 0 if the glyph or the box has no size.</returns>
		public static double GetScale(double Width, double Height, double BoxSize)
		{
			if (Width <= 0 || Height <= 0 || BoxSize <= 0)
				return 0;

			return Math.Min(Math.Min(BoxSize / Width, BoxSize / Height), 1);
		}

		/// <summary>
		/// Creates the glyph of a token, sized to fit a square box. Decoding and SVG rendering run off the UI thread.
		/// </summary>
		/// <param name="Token">The token.</param>
		/// <param name="BoxSize">Size of the square box.</param>
		/// <returns>The glyph, or the placeholder.</returns>
		public static Task<TokenGlyph> CreateAsync(Token Token, double BoxSize)
		{
			return Task.Run(() => Create(Token, BoxSize));
		}

		private static TokenGlyph Create(Token Token, double BoxSize)
		{
			try
			{
				byte[]? Glyph = Token.Glyph;
				string? ContentType = Token.GlyphContentType;

				if (Glyph is not null && Glyph.Length > 0 && !string.IsNullOrEmpty(ContentType))
				{
					if (ContentType.StartsWith("image/svg", StringComparison.OrdinalIgnoreCase))
						return CreateFromSvg(Glyph, BoxSize);

					if (ContentType.StartsWith("image/", StringComparison.OrdinalIgnoreCase))
						return CreateFromBitmap(Glyph, BoxSize);
				}
			}
			catch (Exception Ex)
			{
				ServiceRef.LogService.LogException(Ex);
			}

			return Placeholder(BoxSize);
		}

		/// <summary>
		/// Creates a glyph from a bitmap. The image header is read to get the real size, and images that can't be
		/// decoded get the placeholder.
		/// </summary>
		/// <param name="Glyph">Encoded image.</param>
		/// <param name="BoxSize">Size of the square box.</param>
		/// <returns>The glyph, or the placeholder.</returns>
		private static TokenGlyph CreateFromBitmap(byte[] Glyph, double BoxSize)
		{
			using SKData Data = SKData.CreateCopy(Glyph);
			using SKCodec? Codec = SKCodec.Create(Data);

			if (Codec is null)
				return Placeholder(BoxSize);

			double Width = Codec.Info.Width;
			double Height = Codec.Info.Height;
			double Scale = GetScale(Width, Height, BoxSize);

			if (Scale <= 0)
				return Placeholder(BoxSize);

			return new TokenGlyph(ImageSource.FromStream(() => new MemoryStream(Glyph)), Width * Scale, Height * Scale);
		}

		/// <summary>
		/// Creates a glyph from an SVG image, rendered to PNG. Vector glyphs are scaled to fill the box, since they
		/// don't lose sharpness when enlarged.
		/// </summary>
		/// <param name="Glyph">SVG document.</param>
		/// <param name="BoxSize">Size of the square box.</param>
		/// <returns>The glyph, or the placeholder.</returns>
		private static TokenGlyph CreateFromSvg(byte[] Glyph, double BoxSize)
		{
			using SKSvg Svg = new();

			using (MemoryStream Input = new(Glyph))
			{
				Svg.Load(Input);
			}

			SKPicture? Picture = Svg.Picture;
			if (Picture is null)
				return Placeholder(BoxSize);

			SKRect Bounds = Picture.CullRect;
			if (Bounds.Width <= 0 || Bounds.Height <= 0)
				return Placeholder(BoxSize);

			double Scale = Math.Min(BoxSize / Bounds.Width, BoxSize / Bounds.Height);
			float RenderScale = (float)(Scale * SvgRenderScale);

			using MemoryStream Output = new();

			if (!Picture.ToImage(Output, SKColors.Transparent, SKEncodedImageFormat.Png, 100, RenderScale, RenderScale,
				SKColorType.Rgba8888, SKAlphaType.Premul, SKColorSpace.CreateSrgb()))
			{
				return Placeholder(BoxSize);
			}

			byte[] Png = Output.ToArray();

			return new TokenGlyph(ImageSource.FromStream(() => new MemoryStream(Png)), Bounds.Width * Scale, Bounds.Height * Scale);
		}
	}
}
