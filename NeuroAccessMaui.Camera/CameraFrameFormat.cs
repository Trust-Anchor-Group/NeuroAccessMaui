namespace NeuroAccessMaui.Camera
{
	/// <summary>
	/// Describes the pixel format of a camera frame buffer.
	/// </summary>
	public enum CameraFrameFormat
	{
		/// <summary>
		/// The pixel format is not known.
		/// </summary>
		Unknown = 0,

		/// <summary>
		/// 8-bit grayscale buffer (one byte per pixel).
		/// </summary>
		Grayscale8 = 1,

		/// <summary>
		/// 4:2:0 YUV buffer (platform-specific layout).
		/// </summary>
		Yuv420 = 2,

		/// <summary>
		/// 32-bit BGRA buffer.
		/// </summary>
		Bgra8888 = 3
	}
}
