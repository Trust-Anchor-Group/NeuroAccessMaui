namespace NeuroAccessMaui.Camera
{
	/// <summary>
	/// Defines how the camera preview should be scaled within the view bounds.
	/// </summary>
	public enum CameraPreviewScaling
	{
		/// <summary>
		/// Scales the preview to fill the view bounds, potentially cropping edges.
		/// </summary>
		Fill = 0,

		/// <summary>
		/// Scales the preview to fit fully within the view bounds, preserving full frame visibility.
		/// </summary>
		Fit = 1
	}
}
