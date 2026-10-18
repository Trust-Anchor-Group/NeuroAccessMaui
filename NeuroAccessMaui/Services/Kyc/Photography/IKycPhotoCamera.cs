using NeuroAccessMaui.Camera;

namespace NeuroAccessMaui.Services.Kyc.Photography;

/// <summary>Provides the camera operations needed by the KYC capture and review page.</summary>
public interface IKycPhotoCamera
{
	/// <summary>Gets whether the preview provider is ready to start.</summary>
	bool IsReady { get; }

	/// <summary>Gets whether the preview is running.</summary>
	bool IsPreviewRunning { get; }

	/// <summary>Gets or sets the selected camera.</summary>
	CameraDescriptor? SelectedCamera { get; set; }

	/// <summary>Requests access to the provider's camera.</summary>
	/// <returns>Whether camera access was granted.</returns>
	Task<bool> RequestPermissionAsync();

	/// <summary>Lists the available cameras.</summary>
	/// <param name="CancellationToken">Cancels enumeration.</param>
	/// <returns>The available camera descriptors.</returns>
	Task<IReadOnlyList<CameraDescriptor>> GetAvailableCamerasAsync(CancellationToken CancellationToken);

	/// <summary>Starts the selected camera's preview.</summary>
	/// <param name="CancellationToken">Cancels startup.</param>
	/// <returns>A task representing preview startup.</returns>
	Task StartPreviewAsync(CancellationToken CancellationToken);

	/// <summary>Stops the preview, allowing it to be restarted later.</summary>
	/// <returns>A task representing preview shutdown.</returns>
	Task StopPreviewAsync();

	/// <summary>Permanently releases the camera session.</summary>
	/// <returns>A task representing resource release.</returns>
	Task ReleaseAsync();

	/// <summary>Captures an image from the running preview.</summary>
	/// <param name="CancellationToken">Cancels capture.</param>
	/// <returns>JPEG bytes, or null when the camera cannot produce an image.</returns>
	Task<byte[]?> CapturePhotoAsync(CancellationToken CancellationToken);

	/// <summary>Sets the selected camera's torch state.</summary>
	/// <param name="IsEnabled">Whether to enable the torch.</param>
	/// <param name="CancellationToken">Cancels the operation.</param>
	/// <returns>A task representing the state change.</returns>
	Task SetTorchAsync(bool IsEnabled, CancellationToken CancellationToken);
}
