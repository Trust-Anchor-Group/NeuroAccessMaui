using NeuroAccessMaui.Camera;
using NeuroAccessMaui.Services.Kyc.Photography;

namespace NeuroAccessMaui.UiAutomation;

/// <summary>Returns a fixed JPEG instead of using the device camera, so automation runs capture the same photo every time.</summary>
internal sealed class FixtureKycPhotoCamera : IKycPhotoCamera
{
	private const string imageResource = "NeuroAccessMaui.UiAutomation.identity-test-photo.jpg";
	private static readonly IReadOnlyList<CameraDescriptor> cameras =
	[
		new CameraDescriptor("fixture-front", CameraPosition.Front, false, false, false),
		new CameraDescriptor("fixture-rear", CameraPosition.Rear, false, false, false)
	];

	/// <inheritdoc/>
	public bool IsReady => true;
	/// <inheritdoc/>
	public bool IsPreviewRunning { get; private set; }
	/// <inheritdoc/>
	public CameraDescriptor? SelectedCamera { get; set; }
	/// <inheritdoc/>
	public Task<bool> RequestPermissionAsync() => Task.FromResult(true);
	/// <inheritdoc/>
	public Task<IReadOnlyList<CameraDescriptor>> GetAvailableCamerasAsync(CancellationToken CancellationToken) => Task.FromResult(cameras);
	/// <inheritdoc/>
	public Task SetTorchAsync(bool IsEnabled, CancellationToken CancellationToken) => Task.CompletedTask;

	/// <inheritdoc/>
	public Task StartPreviewAsync(CancellationToken CancellationToken)
	{
		this.IsPreviewRunning = true;
		return Task.CompletedTask;
	}

	/// <inheritdoc/>
	public Task StopPreviewAsync()
	{
		this.IsPreviewRunning = false;
		return Task.CompletedTask;
	}

	/// <inheritdoc/>
	public Task ReleaseAsync() => this.StopPreviewAsync();

	/// <inheritdoc/>
	public async Task<byte[]?> CapturePhotoAsync(CancellationToken CancellationToken)
	{
		await using Stream Input = typeof(FixtureKycPhotoCamera).Assembly.GetManifestResourceStream(imageResource)
			?? throw new InvalidOperationException("The automation build is missing " + imageResource + ".");
		using MemoryStream Output = new MemoryStream();
		await Input.CopyToAsync(Output, CancellationToken);
		return Output.ToArray();
	}
}
