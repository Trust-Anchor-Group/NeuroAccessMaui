using SkiaSharp;

namespace NeuroAccessMaui.Services.Profiles
{
	/// <summary>
	/// Makes the small square photo that is shown for a person: turned the right way up, cut to a square around the middle,
	/// made small, and saved as JPEG.
	/// </summary>
	public static class ProfilePhotos
	{
		/// <summary>
		/// The width and height of a profile photo, in pixels.
		/// </summary>
		public const int Size = 256;

		/// <summary>
		/// The content type of a profile photo.
		/// </summary>
		public const string ContentType = "image/jpeg";

		private const int jpegQuality = 85;

		/// <summary>
		/// Makes a profile photo from a picture.
		/// </summary>
		/// <param name="Data">The picture, in any format the phone can read.</param>
		/// <returns>The profile photo, or null if the picture can't be read.</returns>
		public static byte[]? Make(byte[] Data)
		{
			try
			{
				using SKData Encoded = SKData.CreateCopy(Data);
				using SKCodec? Codec = SKCodec.Create(Encoded);
				if (Codec is null)
					return null;

				using SKBitmap? Decoded = SKBitmap.Decode(Codec);
				if (Decoded is null)
					return null;

				using SKBitmap Upright = Rotate(Decoded, Codec.EncodedOrigin);
				int Side = Math.Min(Upright.Width, Upright.Height);
				SKRectI Square = SKRectI.Create((Upright.Width - Side) / 2, (Upright.Height - Side) / 2, Side, Side);

				using SKBitmap Result = new(Size, Size);
				using (SKCanvas Canvas = new(Result))
				{
					using SKImage Image = SKImage.FromBitmap(Upright);
					Canvas.DrawImage(Image, Square, SKRect.Create(0, 0, Size, Size), new SKSamplingOptions(SKCubicResampler.Mitchell));
				}

				using SKData Jpeg = Result.Encode(SKEncodedImageFormat.Jpeg, jpegQuality);
				return Jpeg?.ToArray();
			}
			catch (Exception Ex)
			{
				ServiceRef.LogService.LogException(Ex);
				return null;
			}
		}

		/// <summary>
		/// Turns a picture the way the camera says it was held.
		/// </summary>
		private static SKBitmap Rotate(SKBitmap Bitmap, SKEncodedOrigin Origin)
		{
			int Degrees = Origin switch
			{
				SKEncodedOrigin.RightTop => 90,
				SKEncodedOrigin.BottomRight => 180,
				SKEncodedOrigin.LeftBottom => 270,
				_ => 0
			};

			bool Sideways = Degrees == 90 || Degrees == 270;
			SKBitmap Result = new(Sideways ? Bitmap.Height : Bitmap.Width, Sideways ? Bitmap.Width : Bitmap.Height);

			using SKCanvas Canvas = new(Result);
			Canvas.Translate(Result.Width / 2f, Result.Height / 2f);
			Canvas.RotateDegrees(Degrees);
			Canvas.Translate(-Bitmap.Width / 2f, -Bitmap.Height / 2f);

			using SKImage Image = SKImage.FromBitmap(Bitmap);
			Canvas.DrawImage(Image, 0, 0, new SKSamplingOptions(SKFilterMode.Linear));

			return Result;
		}
	}
}
