using System.ComponentModel;
using CommunityToolkit.Maui.Layouts;
using Microsoft.Extensions.DependencyInjection;
using NeuroAccessMaui.Animations;
using NeuroAccessMaui.Camera;
using NeuroAccessMaui.Services;

namespace NeuroAccessMaui.UI.Pages.Main.QR
{
	/// <summary>Hosts the application camera and presents automatic and manual QR input.</summary>
	public partial class ScanQrCodePage : IAsyncDisposable
	{
		private readonly SemaphoreSlim cameraGate = new SemaphoreSlim(1, 1);
		private CameraView? cameraView;
		private bool pageVisible;
		private bool disposed;
		private Task? disposalTask;
		private int feedbackVersion;
		private int cameraGateUsers;
		private TaskCompletionSource<bool>? cameraGateIdle;

		/// <summary>Initializes the scanner presentation and its view model.</summary>
		public ScanQrCodePage()
		{
			this.BindingContext = new ScanQrCodeViewModel();
			this.InitializeComponent();
			this.LinkEntry.Keyboard = Keyboard.Url;
			this.LinkEntry.IsSpellCheckEnabled = false;
			this.LinkEntry.IsTextPredictionEnabled = false;
			this.ScanFrameHost.SizeChanged += this.ScanFrameHostSizeChanged;
			StateContainer.SetCurrentState(this.GridWithAnimation, "AutomaticScan");
		}

		/// <inheritdoc/>
		public override async Task OnAppearingAsync()
		{
			await base.OnAppearingAsync();
			await this.Dispatcher.DispatchAsync(async () =>
			{
				if (this.disposed)
					return;
				this.pageVisible = true;
				ScanQrCodeViewModel ViewModel = this.ViewModel<ScanQrCodeViewModel>();
				ViewModel.BeginScannerSession();
				ViewModel.PropertyChanged -= this.ViewModelPropertyChanged;
				ViewModel.PropertyChanged += this.ViewModelPropertyChanged;
				await this.SynchronizePresentationAsync();
			});
		}

		/// <inheritdoc/>
		public override async Task OnDisappearingAsync()
		{
			try
			{
				await this.Dispatcher.DispatchAsync(async () =>
				{
					this.HideScanner();
					if (!await this.EnterCameraGateAsync())
						return;
					try { await this.StopCameraAsync(); }
					finally { this.ExitCameraGate(); }
				});
			}
			finally { await base.OnDisappearingAsync(); }
		}

		/// <inheritdoc/>
		public override Task OnDisposeAsync() => this.Dispatcher.DispatchAsync(() => this.disposalTask ??= this.DisposeCoreAsync());

		/// <summary>Releases camera resources through the page's asynchronous lifecycle.</summary>
		/// <returns>A task representing completion of page cleanup.</returns>
		public virtual async ValueTask DisposeAsync()
		{
			await this.OnDisposeAsync().ConfigureAwait(false);
			GC.SuppressFinalize(this);
		}

		private async Task DisposeCoreAsync()
		{
			this.disposed = true;
			this.ScanFrameHost.SizeChanged -= this.ScanFrameHostSizeChanged;
			this.HideScanner();
			await this.EnterCameraGateAsync(AllowDisposing: true);
			try
			{
				if (this.cameraView is not null)
				{
					this.cameraView.Loaded -= this.CameraLoaded;
					try
					{
						try { await this.StopCameraAsync(); }
						finally
						{
							this.ViewModel<ScanQrCodeViewModel>().AttachCamera(null);
							await this.cameraView.ReleaseAsync();
						}
					}
					finally
					{
						this.cameraView.Handler?.DisconnectHandler();
						this.CameraHost.Children.Clear();
						this.cameraView = null;
					}
				}
			}
			finally
			{
				this.ExitCameraGate();
				try
				{
					if (this.cameraGateIdle is not null)
						await this.cameraGateIdle.Task;
					this.cameraGate.Dispose();
				}
				finally { await base.OnDisposeAsync(); }
			}
		}

		private void ScanFrameHostSizeChanged(object? Sender, EventArgs e)
		{
			double Size = Math.Max(0, Math.Min(240, Math.Min(this.ScanFrameHost.Width, this.ScanFrameHost.Height)));
			this.ScanFeedback.WidthRequest = Size;
			this.ScanFeedback.HeightRequest = Size;
		}

		private void HideScanner()
		{
			this.feedbackVersion++;
			this.pageVisible = false;
			this.ScanFeedback.CancelAnimations();
			this.ManualFeedback.CancelAnimations();
			ScanQrCodeViewModel ViewModel = this.ViewModel<ScanQrCodeViewModel>();
			ViewModel.PropertyChanged -= this.ViewModelPropertyChanged;
			ViewModel.EndScannerSession();
		}

		private async void ViewModelPropertyChanged(object? Sender, PropertyChangedEventArgs e)
		{
			try
			{
				// Apply the completed view-model change, including its dependent properties.
				await Task.Yield();
				await this.Dispatcher.DispatchAsync(async () =>
				{
					if (!this.pageVisible || this.disposed)
						return;
					ScanQrCodeViewModel ViewModel = this.ViewModel<ScanQrCodeViewModel>();
					if (e.PropertyName == nameof(ScanQrCodeViewModel.State))
					{
						if (ViewModel.State is QrScannerState.Starting or QrScannerState.CameraUnavailable or QrScannerState.Closing)
							await this.SynchronizePresentationAsync();
						await this.AnimateFeedbackAsync();
					}
					else if (e.PropertyName is nameof(ScanQrCodeViewModel.IsAutomaticScan) or
						nameof(ScanQrCodeViewModel.IsTorchOn) or nameof(ScanQrCodeViewModel.SelectedCamera))
						await this.SynchronizePresentationAsync();
				});
			}
			catch (OperationCanceledException) { }
			catch (Exception Ex) { ServiceRef.LogService.LogException(Ex); }
		}

		private async Task AnimateFeedbackAsync()
		{
			int Version = ++this.feedbackVersion;
			this.ScanFeedback.CancelAnimations();
			this.ManualFeedback.CancelAnimations();
			this.ScanFeedback.Scale = 1;
			this.ManualFeedback.Scale = 1;
			if (!this.pageVisible || ServiceRef.Provider.GetService<IMotionSettings>()?.ReduceMotion == true)
				return;
			ScanQrCodeViewModel ViewModel = this.ViewModel<ScanQrCodeViewModel>();
			VisualElement Feedback = ViewModel.IsAutomaticScan ? this.ScanFeedback : this.ManualFeedback;
			QrScannerState State = ViewModel.State;
			await Feedback.ScaleToAsync(State is QrScannerState.Accepted or QrScannerState.Rejected ? 0.94 : 1, 140, Easing.CubicOut);
			if (State == QrScannerState.Rejected && Version == this.feedbackVersion && this.pageVisible)
				await Feedback.ScaleToAsync(1, 140, Easing.CubicOut);
		}

		private async void CameraLoaded(object? Sender, EventArgs e)
		{
			try { await this.Dispatcher.DispatchAsync(this.SynchronizePresentationAsync); }
			catch (Exception Ex) { ServiceRef.LogService.LogException(Ex); }
		}

		private async Task SynchronizePresentationAsync()
		{
			if (!await this.EnterCameraGateAsync())
				return;
			try
			{
				if (!this.pageVisible || this.disposed)
					return;
				ScanQrCodeViewModel ViewModel = this.ViewModel<ScanQrCodeViewModel>();
				string DesiredState = ViewModel.IsAutomaticScan ? "AutomaticScan" : "ManualScan";
				if (StateContainer.GetCurrentState(this.GridWithAnimation) != DesiredState)
				{
					await this.StopCameraAsync();
					this.LinkEntry.Unfocus();
					if (ServiceRef.Provider.GetService<IMotionSettings>()?.ReduceMotion == true)
						StateContainer.SetCurrentState(this.GridWithAnimation, DesiredState);
					else
						await StateContainer.ChangeStateWithAnimation(this.GridWithAnimation, DesiredState, CancellationToken.None);
					if (!this.pageVisible || this.disposed || ViewModel.IsAutomaticScan != (DesiredState == "AutomaticScan"))
						return;
					if (!ViewModel.IsAutomaticScan)
						this.LinkEntry.Focus();
				}
				if (!ViewModel.IsAutomaticScan || ViewModel.State is QrScannerState.CameraUnavailable or QrScannerState.Closing)
				{
					await this.StopCameraAsync();
					return;
				}
				if (this.cameraView is null)
				{
					this.cameraView = new CameraView
					{
						HorizontalOptions = LayoutOptions.Fill,
						VerticalOptions = LayoutOptions.Fill,
						Options = new CameraOptions
						{
							PreferRearCamera = true,
							ContinuousAutoFocus = true,
							TargetResolution = new Size(1280, 720),
							FrameDeliveryInterval = TimeSpan.FromMilliseconds(200)
						}
					};
					this.cameraView.Loaded += this.CameraLoaded;
					ViewModel.AttachCamera(this.cameraView);
					this.CameraHost.Children.Add(this.cameraView);
				}
				await ViewModel.StartCameraAsync();
				if (!this.pageVisible || !ViewModel.IsAutomaticScan || ViewModel.State is QrScannerState.CameraUnavailable or QrScannerState.Closing)
					await this.StopCameraAsync();
				else if (this.cameraView.Controller is ICameraController Controller)
					await Controller.SetTorchAsync(ViewModel.IsTorchOn, CancellationToken.None);
			}
			catch (OperationCanceledException) { }
			catch (Exception)
			{
				this.ViewModel<ScanQrCodeViewModel>().HandleCameraFailure();
				await this.StopCameraAsync();
			}
			finally { this.ExitCameraGate(); }
		}

		/// <summary>Tracks admitted camera operations so disposal can wait for all of them.</summary>
		/// <param name="AllowDisposing">Whether this operation performs final camera cleanup.</param>
		/// <returns>Whether the operation entered the camera gate.</returns>
		private async Task<bool> EnterCameraGateAsync(bool AllowDisposing = false)
		{
			// Admission and user counts are confined to the page dispatcher.
			if (this.disposed && !AllowDisposing)
				return false;

			if (this.cameraGateUsers++ == 0)
				this.cameraGateIdle = new TaskCompletionSource<bool>(TaskCreationOptions.RunContinuationsAsynchronously);
			await this.cameraGate.WaitAsync();
			return true;
		}

		/// <summary>Releases the camera gate and signals when all admitted operations have exited.</summary>
		private void ExitCameraGate()
		{
			this.cameraGate.Release();
			if (--this.cameraGateUsers == 0)
				this.cameraGateIdle?.TrySetResult(true);
		}

		private async Task StopCameraAsync()
		{
			if (this.cameraView is null)
				return;
			try
			{
				if (this.cameraView.Controller is ICameraController Controller)
					await Controller.SetTorchAsync(false, CancellationToken.None);
			}
			finally { await this.cameraView.StopPreviewAsync(); }
		}
	}
}
