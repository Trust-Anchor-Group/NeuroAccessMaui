using System.Threading;
using System.Threading.Tasks;
using Microsoft.Maui.Graphics;

namespace NeuroAccessMaui.Camera
{
	/// <summary>
	/// Provides control over camera preview behavior.
	/// </summary>
	public interface ICameraController
	{
		/// <summary>
		/// Starts the camera preview.
		/// </summary>
		/// <param name="CancellationToken">Cancellation token.</param>
		/// <returns>A task representing the asynchronous operation.</returns>
		Task StartPreviewAsync(CancellationToken CancellationToken);

		/// <summary>
		/// Stops the camera preview.
		/// </summary>
		/// <returns>A task representing the asynchronous operation.</returns>
		Task StopPreviewAsync();

		/// <summary>
		/// Permanently releases camera resources for the current handler instance.
		/// </summary>
		/// <returns>A task representing the asynchronous operation.</returns>
		Task ReleaseAsync();

		/// <summary>
		/// Enables or disables the torch if supported.
		/// </summary>
		/// <param name="IsEnabled">Whether the torch should be enabled.</param>
		/// <param name="CancellationToken">Cancellation token.</param>
		/// <returns>A task representing the asynchronous operation.</returns>
		Task SetTorchAsync(bool IsEnabled, CancellationToken CancellationToken);

		/// <summary>
		/// Sets the camera zoom ratio if supported.
		/// </summary>
		/// <param name="ZoomRatio">The desired zoom ratio.</param>
		/// <param name="CancellationToken">Cancellation token.</param>
		/// <returns>A task representing the asynchronous operation.</returns>
		Task SetZoomAsync(float ZoomRatio, CancellationToken CancellationToken);

		/// <summary>
		/// Sets the focus point in normalized coordinates if supported.
		/// </summary>
		/// <param name="FocusPoint">The focus point, where (0,0) is top-left and (1,1) is bottom-right.</param>
		/// <param name="CancellationToken">Cancellation token.</param>
		/// <returns>A task representing the asynchronous operation.</returns>
		Task SetFocusPointAsync(Point? FocusPoint, CancellationToken CancellationToken);

		/// <summary>
		/// Captures a still image from the active camera preview.
		/// </summary>
		/// <param name="CancellationToken">Cancellation token.</param>
		/// <returns>JPEG image bytes when supported; otherwise <c>null</c>.</returns>
		Task<byte[]?> CapturePhotoAsync(CancellationToken CancellationToken);
	}
}
