using System;
using Microsoft.Maui.Graphics;

namespace NeuroAccessMaui.Camera
{
	/// <summary>
	/// Defines configuration options for camera preview and frame delivery.
	/// </summary>
	public sealed class CameraOptions
	{
		/// <summary>
		/// Initializes a new instance of the <see cref="CameraOptions"/> class.
		/// </summary>
		public CameraOptions()
		{
			this.PreferRearCamera = true;
			this.FrameDeliveryInterval = TimeSpan.FromMilliseconds(200);
			this.PreviewScaling = CameraPreviewScaling.Fill;
		}

		/// <summary>
		/// Gets or sets the preferred camera position.
		/// </summary>
		public bool PreferRearCamera { get; set; }

		/// <summary>
		/// Gets or sets the desired target resolution for the preview stream.
		/// </summary>
		public Size? TargetResolution { get; set; }

		/// <summary>
		/// Gets or sets the preferred frames per second for frame delivery.
		/// </summary>
		public int? TargetFps { get; set; }

		/// <summary>
		/// Gets or sets the minimum interval between delivered frames.
		/// </summary>
		public TimeSpan FrameDeliveryInterval { get; set; }

		/// <summary>
		/// Gets or sets how the camera preview should be scaled in the host view.
		/// </summary>
		public CameraPreviewScaling PreviewScaling { get; set; }

		/// <summary>
		/// Gets or sets the optional JPEG quality (0-100) for still capture.
		/// </summary>
		public int? JpegQuality { get; set; }
	}
}
