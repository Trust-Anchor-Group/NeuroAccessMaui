#if WINDOWS
using System.Threading;
using System.Threading.Tasks;
using Microsoft.Maui.Handlers;
using WinGrid = Microsoft.UI.Xaml.Controls.Grid;

namespace NeuroAccessMaui.Camera
{
	/// <summary>
	/// Windows stub handler for <see cref="CameraView"/>.
	/// </summary>
	public sealed class CameraViewHandler : ViewHandler<CameraView, WinGrid>, ICameraController
	{
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

		/// <inheritdoc/>
		public Task StartPreviewAsync(CancellationToken CancellationToken)
		{
			if (this.VirtualView is not null)
				this.VirtualView.IsPreviewRunning = false;
			return Task.CompletedTask;
		}

		/// <inheritdoc/>
		public Task StopPreviewAsync()
		{
			if (this.VirtualView is not null)
				this.VirtualView.IsPreviewRunning = false;
			return Task.CompletedTask;
		}

		/// <inheritdoc/>
		public Task ReleaseAsync()
		{
			if (this.VirtualView is not null)
				this.VirtualView.IsPreviewRunning = false;
			return Task.CompletedTask;
		}

		/// <inheritdoc/>
		public Task SetTorchAsync(bool IsEnabled, CancellationToken CancellationToken)
		{
			return Task.CompletedTask;
		}

		/// <inheritdoc/>
		public Task SetZoomAsync(float ZoomRatio, CancellationToken CancellationToken)
		{
			return Task.CompletedTask;
		}

		/// <inheritdoc/>
		public Task SetFocusPointAsync(Microsoft.Maui.Graphics.Point? FocusPoint, CancellationToken CancellationToken)
		{
			return Task.CompletedTask;
		}

		/// <inheritdoc/>
		public Task<byte[]?> CapturePhotoAsync(CancellationToken CancellationToken)
		{
			return Task.FromResult<byte[]?>(null);
		}

		/// <inheritdoc/>
		protected override WinGrid CreatePlatformView()
		{
			return new WinGrid();
		}

		/// <inheritdoc/>
		protected override void ConnectHandler(WinGrid PlatformView)
		{
			base.ConnectHandler(PlatformView);
			this.VirtualView?.SetController(this);
		}

		/// <inheritdoc/>
		protected override void DisconnectHandler(WinGrid PlatformView)
		{
			if (this.VirtualView is not null)
				this.VirtualView.SetController(null);
			base.DisconnectHandler(PlatformView);
		}

		private static void MapOptions(CameraViewHandler Handler, CameraView View)
		{
		}

		private static void MapSelectedCamera(CameraViewHandler Handler, CameraView View)
		{
		}
	}
}
#endif
