using System;

namespace NeuroAccessMaui.Camera
{
	/// <summary>
	/// Exposes events for receiving camera frame buffers.
	/// </summary>
	public interface ICameraFrameSource
	{
		/// <summary>
		/// Occurs when a new camera frame is available.
		/// </summary>
		event EventHandler<CameraFrame> FrameReady;
	}
}
