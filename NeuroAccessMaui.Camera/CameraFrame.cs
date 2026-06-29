using System;

namespace NeuroAccessMaui.Camera
{
	/// <summary>
	/// Represents a single camera frame buffer.
	/// </summary>
	public sealed class CameraFrame
	{
		/// <summary>
		/// Initializes a new instance of the <see cref="CameraFrame"/> class.
		/// </summary>
		/// <param name="Width">Frame width in pixels.</param>
		/// <param name="Height">Frame height in pixels.</param>
		/// <param name="Format">Pixel format for the buffer.</param>
		/// <param name="RotationDegrees">Rotation degrees to orient the frame upright.</param>
		/// <param name="Timestamp">Timestamp for when the frame was captured.</param>
		/// <param name="Buffer">Frame buffer data.</param>
		public CameraFrame(
			int Width,
			int Height,
			CameraFrameFormat Format,
			int RotationDegrees,
			DateTimeOffset Timestamp,
			byte[] Buffer)
		{
			ArgumentNullException.ThrowIfNull(Buffer);

			this.Width = Width;
			this.Height = Height;
			this.Format = Format;
			this.RotationDegrees = RotationDegrees;
			this.Timestamp = Timestamp;
			this.Buffer = Buffer;
		}

		/// <summary>
		/// Gets the frame width in pixels.
		/// </summary>
		public int Width { get; }

		/// <summary>
		/// Gets the frame height in pixels.
		/// </summary>
		public int Height { get; }

		/// <summary>
		/// Gets the frame pixel format.
		/// </summary>
		public CameraFrameFormat Format { get; }

		/// <summary>
		/// Gets the rotation (in degrees) required to orient the frame upright.
		/// </summary>
		public int RotationDegrees { get; }

		/// <summary>
		/// Gets the timestamp associated with this frame.
		/// </summary>
		public DateTimeOffset Timestamp { get; }

		/// <summary>
		/// Gets the raw frame buffer.
		/// </summary>
		public ReadOnlyMemory<byte> Buffer { get; }
	}
}
