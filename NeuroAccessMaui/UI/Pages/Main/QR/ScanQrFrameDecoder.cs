using System.Runtime.InteropServices;
using NeuroAccessMaui.Camera;
using ZXing;
using ZXing.Common;

namespace NeuroAccessMaui.UI.Pages.Main.QR
{
	/// <summary>Decodes the scanner's two-dimensional formats from application camera frames.</summary>
	internal sealed class ScanQrFrameDecoder
	{
		private readonly BarcodeReaderGeneric reader = new BarcodeReaderGeneric
		{
			AutoRotate = true,
			Options = new DecodingOptions
			{
				PossibleFormats = new List<BarcodeFormat>
				{
					BarcodeFormat.AZTEC, BarcodeFormat.DATA_MATRIX, BarcodeFormat.MAXICODE,
					BarcodeFormat.PDF_417, BarcodeFormat.QR_CODE
				},
				TryHarder = true,
				TryInverted = true
			}
		};

		/// <summary>Decodes one frame on a background worker; callers serialize access.</summary>
		/// <param name="Frame">The packed grayscale frame.</param>
		/// <param name="CancellationToken">Cancellation for the current scanner session.</param>
		/// <returns>The unmodified decoded text, or null if no code was found.</returns>
		internal Task<string?> DecodeAsync(CameraFrame Frame, CancellationToken CancellationToken)
		{
			if (Frame.Format != CameraFrameFormat.Grayscale8 || Frame.Width <= 0 || Frame.Height <= 0 ||
				(long)Frame.Width * Frame.Height != Frame.Buffer.Length)
				return Task.FromResult<string?>(null);

			return Task.Run(() =>
			{
				CancellationToken.ThrowIfCancellationRequested();
				byte[] Luminance = MemoryMarshal.TryGetArray(Frame.Buffer, out ArraySegment<byte> Segment) &&
					Segment.Offset == 0 && Segment.Array is not null ? Segment.Array : Frame.Buffer.ToArray();
				PlanarYUVLuminanceSource Source = new PlanarYUVLuminanceSource(
					Luminance, Frame.Width, Frame.Height, 0, 0, Frame.Width, Frame.Height, false);
				Result? Result = this.reader.Decode(Source);
				CancellationToken.ThrowIfCancellationRequested();
				return Result?.Text;
			}, CancellationToken);
		}
	}
}
