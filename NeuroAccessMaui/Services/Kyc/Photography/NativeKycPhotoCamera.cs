using NeuroAccessMaui.Camera;

namespace NeuroAccessMaui.Services.Kyc.Photography;

/// <summary>Adapts the native camera view without changing its capture or permission behavior.</summary>
internal sealed class NativeKycPhotoCamera : IKycPhotoCamera
{
	private readonly CameraView preview;

	internal NativeKycPhotoCamera(CameraView Preview, bool IsSelfie)
	{
		this.preview = Preview;
		this.preview.Options = new CameraOptions
		{
			PreferRearCamera = !IsSelfie,
			FrameDeliveryInterval = TimeSpan.FromMilliseconds(250),
			PreviewScaling = CameraPreviewScaling.Fill,
			TargetFps = 15,
			TargetResolution = new Size(1280d, 720d),
			JpegQuality = 96
		};
	}

	/// <inheritdoc/>
	public bool IsReady => this.preview.Controller is not null;
	/// <inheritdoc/>
	public bool IsPreviewRunning => this.preview.IsPreviewRunning;
	/// <inheritdoc/>
	public CameraDescriptor? SelectedCamera { get => this.preview.SelectedCamera; set => this.preview.SelectedCamera = value; }
	/// <inheritdoc/>
	public Task<bool> RequestPermissionAsync() => ServiceRef.PermissionService.CheckCameraPermissionAsync();
	/// <inheritdoc/>
	public Task<IReadOnlyList<CameraDescriptor>> GetAvailableCamerasAsync(CancellationToken CancellationToken) => CameraView.GetAvailableCamerasAsync(CancellationToken);
	/// <inheritdoc/>
	public Task StartPreviewAsync(CancellationToken CancellationToken) => this.preview.StartPreviewAsync(CancellationToken);
	/// <inheritdoc/>
	public Task StopPreviewAsync() => this.preview.StopPreviewAsync();
	/// <inheritdoc/>
	public Task ReleaseAsync() => this.preview.ReleaseAsync();
	/// <inheritdoc/>
	public Task<byte[]?> CapturePhotoAsync(CancellationToken CancellationToken) => this.preview.CapturePhotoAsync(CancellationToken);
	/// <inheritdoc/>
	public Task SetTorchAsync(bool IsEnabled, CancellationToken CancellationToken) =>
		this.preview.Controller?.SetTorchAsync(IsEnabled, CancellationToken) ?? Task.CompletedTask;
}
