#if ANDROID || IOS
using System;
using System.Threading;
using System.Threading.Tasks;
using Microsoft.Maui.Handlers;

#if ANDROID
using PlatformView = AndroidX.Camera.View.PreviewView;
#elif IOS
using PlatformView = UIKit.UIView;
#endif

namespace NeuroAccessMaui.Camera
{
	/// <summary>
	/// MAUI handler for the <see cref="CameraView"/> control.
	/// </summary>
	public partial class CameraViewHandler : ViewHandler<CameraView, PlatformView>, ICameraController, IDisposable
	{
		private bool isDisposed;
		private bool isReleased;

		/// <summary>
		/// Maps the <see cref="CameraView"/> properties to the handler.
		/// </summary>
		public static IPropertyMapper<CameraView, CameraViewHandler> Mapper = new PropertyMapper<CameraView, CameraViewHandler>(ViewHandler.ViewMapper)
		{
			[nameof(CameraView.Options)] = MapOptions,
			[nameof(CameraView.SelectedCamera)] = MapSelectedCamera
		};

		/// <summary>
		/// Initializes a new instance of the <see cref="CameraViewHandler"/> class.
		/// </summary>
		public CameraViewHandler()
			: base(Mapper)
		{
		}

		/// <summary>
		/// Starts the camera preview.
		/// </summary>
		/// <param name="CancellationToken">Cancellation token.</param>
		/// <returns>A task representing the asynchronous operation.</returns>
		public Task StartPreviewAsync(CancellationToken CancellationToken)
		{
			if (this.isDisposed || this.isReleased)
				return Task.CompletedTask;

			return this.StartPreviewInternalAsync(CancellationToken);
		}

		/// <summary>
		/// Stops the camera preview.
		/// </summary>
		/// <returns>A task representing the asynchronous operation.</returns>
		public Task StopPreviewAsync()
		{
			if (this.isDisposed || this.isReleased)
				return Task.CompletedTask;

			return this.StopPreviewInternalAsync();
		}

		/// <summary>
		/// Permanently releases camera resources for the current handler instance.
		/// </summary>
		/// <returns>A task representing the asynchronous operation.</returns>
		public async Task ReleaseAsync()
		{
			if (this.isDisposed || this.isReleased)
				return;

			this.isReleased = true;
			await this.ReleaseInternalAsync().ConfigureAwait(false);
		}

		/// <summary>
		/// Enables or disables the torch.
		/// </summary>
		/// <param name="IsEnabled">Whether the torch should be enabled.</param>
		/// <param name="CancellationToken">Cancellation token.</param>
		/// <returns>A task representing the asynchronous operation.</returns>
		public Task SetTorchAsync(bool IsEnabled, CancellationToken CancellationToken)
		{
			if (this.isDisposed || this.isReleased)
				return Task.CompletedTask;

			return this.SetTorchInternalAsync(IsEnabled, CancellationToken);
		}

		/// <summary>
		/// Sets the camera zoom ratio.
		/// </summary>
		/// <param name="ZoomRatio">The desired zoom ratio.</param>
		/// <param name="CancellationToken">Cancellation token.</param>
		/// <returns>A task representing the asynchronous operation.</returns>
		public Task SetZoomAsync(float ZoomRatio, CancellationToken CancellationToken)
		{
			if (this.isDisposed || this.isReleased)
				return Task.CompletedTask;

			return this.SetZoomInternalAsync(ZoomRatio, CancellationToken);
		}

		/// <summary>
		/// Sets the camera focus point.
		/// </summary>
		/// <param name="FocusPoint">Normalized focus point coordinates.</param>
		/// <param name="CancellationToken">Cancellation token.</param>
		/// <returns>A task representing the asynchronous operation.</returns>
		public Task SetFocusPointAsync(Microsoft.Maui.Graphics.Point? FocusPoint, CancellationToken CancellationToken)
		{
			if (this.isDisposed || this.isReleased)
				return Task.CompletedTask;

			return this.SetFocusPointInternalAsync(FocusPoint, CancellationToken);
		}

		/// <summary>
		/// Captures a still image from the active preview.
		/// </summary>
		/// <param name="CancellationToken">Cancellation token.</param>
		/// <returns>JPEG image bytes when supported; otherwise <c>null</c>.</returns>
		public Task<byte[]?> CapturePhotoAsync(CancellationToken CancellationToken)
		{
			if (this.isDisposed || this.isReleased)
				return Task.FromResult<byte[]?>(null);

			return this.CapturePhotoInternalAsync(CancellationToken);
		}

		/// <inheritdoc/>
		protected override void ConnectHandler(PlatformView platformView)
		{
			base.ConnectHandler(platformView);
			if (this.VirtualView is not null)
			{
				this.VirtualView.SetFrameSubscriptionChangedCallback(this.OnFrameSubscriptionsChanged);
				this.VirtualView.SetController(this);
			}
			this.Initialize();
		}

		/// <inheritdoc/>
		protected override void DisconnectHandler(PlatformView platformView)
		{
			this.Cleanup();
			if (this.VirtualView is not null)
			{
				this.VirtualView.SetFrameSubscriptionChangedCallback(null);
				this.VirtualView.SetController(null);
			}
			base.DisconnectHandler(platformView);
		}

		/// <summary>
		/// Releases resources used by the handler.
		/// </summary>
		public void Dispose()
		{
			this.Dispose(disposing: true);
			GC.SuppressFinalize(this);
		}

		/// <summary>
		/// Releases resources used by the handler.
		/// </summary>
		/// <param name="disposing">If <c>true</c>, managed resources should be disposed.</param>
		protected virtual void Dispose(bool disposing)
		{
			if (this.isDisposed)
				return;

			if (disposing)
			{
				this.Cleanup();
				if (this.VirtualView is not null)
				{
					this.VirtualView.SetFrameSubscriptionChangedCallback(null);
					this.VirtualView.SetController(null);
				}
			}

			this.isDisposed = true;
		}

		private static void MapOptions(CameraViewHandler Handler, CameraView View)
		{
			Handler.OnOptionsChanged(View.Options);
		}

		private static void MapSelectedCamera(CameraViewHandler Handler, CameraView View)
		{
			Handler.OnSelectedCameraChanged(View.SelectedCamera);
		}

		private void OnFrameSubscriptionsChanged(bool HasFrameDemand)
		{
			if (this.isDisposed || this.isReleased)
				return;

			this.HandleFrameDemandChanged(HasFrameDemand);
		}

		private partial void Initialize();
		private partial void Cleanup();
		private partial Task StartPreviewInternalAsync(CancellationToken CancellationToken);
		private partial Task StopPreviewInternalAsync();
		private partial Task ReleaseInternalAsync();
		private partial Task SetTorchInternalAsync(bool IsEnabled, CancellationToken CancellationToken);
		private partial Task SetZoomInternalAsync(float ZoomRatio, CancellationToken CancellationToken);
		private partial Task SetFocusPointInternalAsync(Microsoft.Maui.Graphics.Point? FocusPoint, CancellationToken CancellationToken);
		private partial Task<byte[]?> CapturePhotoInternalAsync(CancellationToken CancellationToken);
		private partial void OnSelectedCameraChanged(CameraDescriptor? SelectedCamera);
		private partial void OnOptionsChanged(CameraOptions Options);
		private partial void HandleFrameDemandChanged(bool HasFrameDemand);
	}
}
#endif
