#if ANDROID
using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.IO;
using System.Threading;
using System.Threading.Tasks;
using Android.Content;
using Android.Util;
using AndroidX.Camera.Core;
using AndroidX.Camera.Lifecycle;
using AndroidX.Camera.View;
using AndroidX.Concurrent.Futures;
using AndroidX.Core.Content;
using AndroidX.Lifecycle;
using Java.Lang;
using Java.Util.Concurrent;
using Microsoft.Maui.ApplicationModel;
using Google.Common.Util.Concurrent;

namespace NeuroAccessMaui.Camera
{
	public partial class CameraViewHandler
	{
		private ProcessCameraProvider? cameraProvider;
		private Preview? preview;
		private ImageAnalysis? imageAnalysis;
		private ImageCapture? imageCapture;
		private AndroidX.Camera.Core.ICamera? camera;
		private IExecutorService? analyzerExecutor;
		private CameraOptions options = new CameraOptions();
		private CameraDescriptor? selectedCamera;
		private DateTimeOffset lastFrameTimestamp = DateTimeOffset.MinValue;
		private readonly SemaphoreSlim cameraSemaphore = new SemaphoreSlim(1, 1);
		private bool isCameraConnected;
		private long previewRequest;

		/// <inheritdoc/>
		protected override PreviewView CreatePlatformView()
		{
			PreviewView View = new PreviewView(this.Context);
			View.SetScaleType(PreviewView.ScaleType.FillCenter);
			return View;
		}

		private partial void Initialize()
		{
			_ = this.SetCameraConnectionAsync(true);
		}

		private partial void Cleanup()
		{
			_ = this.SetCameraConnectionAsync(false);
		}

		/// <summary>
		/// Observes connection-hook failures while ordering connection changes with native cleanup.
		/// </summary>
		/// <param name="IsConnected">Whether the handler is connecting.</param>
		/// <returns>A task representing the observed connection update.</returns>
		private async Task SetCameraConnectionAsync(bool IsConnected)
		{
			try
			{
				await this.cameraSemaphore.WaitAsync().ConfigureAwait(false);
				try
				{
					await MainThread.InvokeOnMainThreadAsync(() =>
					{
						this.previewRequest++;
						this.isCameraConnected = IsConnected && !this.isDisposed && !this.isReleased;
						if (!this.isCameraConnected)
							this.StopPreviewInternal();
					}).ConfigureAwait(false);
				}
				finally
				{
					this.cameraSemaphore.Release();
				}
			}
			catch (System.Exception Ex)
			{
				LogWarning($"Camera connection update failed. Connected={IsConnected}, ExceptionType={Ex.GetType().FullName}");
			}
		}

		private partial Task StartPreviewInternalAsync(CancellationToken CancellationToken)
		{
			return this.StartPreviewCoreAsync(false, CancellationToken);
		}

		/// <summary>
		/// Admits a preview request and binds it only if it remains current after provider initialization.
		/// </summary>
		/// <param name="RequireRunningPreview">Whether this is an automatic restart of an active preview.</param>
		/// <param name="CancellationToken">Cancellation token.</param>
		/// <returns>A task representing preview initialization.</returns>
		private async Task StartPreviewCoreAsync(bool RequireRunningPreview, CancellationToken CancellationToken)
		{
			long Request = 0;
			Context? PreviewContext = null;
			CameraView? View = null;
			PreviewView? PreviewControl = null;
			Task<ProcessCameraProvider>? ProviderTask = null;
			await this.cameraSemaphore.WaitAsync(CancellationToken).ConfigureAwait(false);
			try
			{
				await MainThread.InvokeOnMainThreadAsync(() =>
				{
					CancellationToken.ThrowIfCancellationRequested();
					if (!this.IsCameraAvailable() || (RequireRunningPreview && !this.VirtualView.IsPreviewRunning))
						return;

					Request = ++this.previewRequest;
					PreviewContext = this.Context;
					View = this.VirtualView;
					PreviewControl = this.PlatformView;
					// Store the task so dispatcher unwrapping cannot keep the gate held during initialization.
					ProviderTask = GetCameraProviderAsync(PreviewContext, CancellationToken);
				}).ConfigureAwait(false);
			}
			finally
			{
				this.cameraSemaphore.Release();
			}

			if (ProviderTask is null || PreviewContext is null || View is null || PreviewControl is null)
				return;

			ProcessCameraProvider Provider = await ProviderTask.ConfigureAwait(false);
			await this.cameraSemaphore.WaitAsync(CancellationToken).ConfigureAwait(false);
			try
			{
				await MainThread.InvokeOnMainThreadAsync(() =>
				{
					CancellationToken.ThrowIfCancellationRequested();
					if (Request != this.previewRequest || !this.IsCameraAvailable() ||
						!ReferenceEquals(View, this.VirtualView) || !ReferenceEquals(PreviewControl, this.PlatformView))
						return;

					this.StopPreviewInternal();
					this.cameraProvider = Provider;
					this.options = View.Options ?? new CameraOptions();
					this.selectedCamera = View.SelectedCamera;
					this.lastFrameTimestamp = DateTimeOffset.MinValue;
					try
					{
						this.BindPreview(PreviewContext, PreviewControl, Provider);
						CancellationToken.ThrowIfCancellationRequested();
						if (!this.IsCameraAvailable())
						{
							this.StopPreviewInternal();
							return;
						}
						View.IsPreviewRunning = true;
					}
					catch
					{
						try
						{
							this.StopPreviewInternal();
						}
						catch (System.Exception Ex)
						{
							LogWarning($"Camera setup cleanup failed. ExceptionType={Ex.GetType().FullName}");
						}
						throw;
					}
				}).ConfigureAwait(false);
			}
			finally
			{
				this.cameraSemaphore.Release();
			}
		}

		/// <summary>
		/// Allocates and binds owned use cases on the main thread while the camera semaphore is held.
		/// </summary>
		/// <param name="Context">The admitted Android context.</param>
		/// <param name="PreviewControl">The admitted preview control.</param>
		/// <param name="Provider">The initialized camera provider.</param>
		private void BindPreview(Context Context, PreviewView PreviewControl, ProcessCameraProvider Provider)
		{
			CameraSelector Selector = this.BuildCameraSelector();
			bool ShouldEnableFrameAnalysis = this.ShouldEnableFrameAnalysis();
			ApplyPreviewScaling(PreviewControl, this.options.PreviewScaling);
			Preview.Builder PreviewBuilder = new Preview.Builder();
			ApplyTargetResolution(PreviewBuilder, this.options);
			Preview Preview = PreviewBuilder.Build();
			this.preview = Preview;
			Preview.SetSurfaceProvider(ContextCompat.GetMainExecutor(Context), PreviewControl.SurfaceProvider);

			ImageAnalysis? Analysis = null;
			if (ShouldEnableFrameAnalysis)
			{
				ImageAnalysis.Builder AnalysisBuilder = new ImageAnalysis.Builder();
				AnalysisBuilder.SetBackpressureStrategy(ImageAnalysis.StrategyKeepOnlyLatest);
				ApplyTargetResolution(AnalysisBuilder, this.options);
				Analysis = AnalysisBuilder.Build();
				this.imageAnalysis = Analysis;
			}

			ImageCapture.Builder CaptureBuilder = new ImageCapture.Builder();
			ApplyTargetResolution(CaptureBuilder, this.options);
			CaptureBuilder.SetCaptureMode(ImageCapture.CaptureModeMaximizeQuality);
			if (this.options.JpegQuality.HasValue)
			{
				int JpegQuality = System.Math.Max(0, System.Math.Min(100, this.options.JpegQuality.Value));
				CaptureBuilder.SetJpegQuality(JpegQuality);
			}
			ImageCapture Capture = CaptureBuilder.Build();
			this.imageCapture = Capture;

			if (Analysis is not null)
			{
				this.analyzerExecutor = Executors.NewSingleThreadExecutor();
				Analysis.SetAnalyzer(this.analyzerExecutor, new FrameAnalyzer(this));
			}

			ILifecycleOwner Owner = this.GetLifecycleOwner();
			List<UseCase> UseCases = new List<UseCase>
			{
				Preview,
				Capture
			};
			if (Analysis is not null)
				UseCases.Insert(1, Analysis);

			this.camera = Provider.BindToLifecycle(Owner, Selector, UseCases.ToArray());
		}

		/// <summary>
		/// Checks handler availability on the main thread while the camera semaphore is held.
		/// </summary>
		/// <returns>Whether native camera work may be initiated.</returns>
		private bool IsCameraAvailable() => this.isCameraConnected && !this.isDisposed && !this.isReleased &&
			this.VirtualView is not null && this.PlatformView is not null;

		private partial Task StopPreviewInternalAsync()
		{
			return this.StopCameraAsync();
		}

		private partial Task ReleaseInternalAsync()
		{
			return this.StopCameraAsync();
		}

		/// <summary>
		/// Invalidates pending preview requests and cleans resources within the same gate acquisition.
		/// </summary>
		/// <returns>A task representing native cleanup.</returns>
		private async Task StopCameraAsync()
		{
			await this.cameraSemaphore.WaitAsync().ConfigureAwait(false);
			try
			{
				await MainThread.InvokeOnMainThreadAsync(() =>
				{
					this.previewRequest++;
					this.StopPreviewInternal();
				}).ConfigureAwait(false);
			}
			finally
			{
				this.cameraSemaphore.Release();
			}
		}

		private async partial Task<byte[]?> CapturePhotoInternalAsync(CancellationToken CancellationToken)
		{
			Task<byte[]?>? CaptureTask = null;
			await this.cameraSemaphore.WaitAsync(CancellationToken).ConfigureAwait(false);
			try
			{
				await MainThread.InvokeOnMainThreadAsync(() =>
				{
					CaptureTask = this.CapturePhotoOnMainThreadAsync(CancellationToken);
				}).ConfigureAwait(false);
			}
			finally
			{
				this.cameraSemaphore.Release();
			}

			return CaptureTask is null ? null : await CaptureTask.ConfigureAwait(false);
		}

		/// <summary>
		/// Initiates native capture on the main thread while the camera semaphore is held.
		/// </summary>
		/// <param name="CancellationToken">Cancellation token.</param>
		/// <returns>The existing callback's capture result task.</returns>
		private Task<byte[]?> CapturePhotoOnMainThreadAsync(CancellationToken CancellationToken)
		{
			CancellationToken.ThrowIfCancellationRequested();
			if (!this.IsCameraAvailable() || this.imageCapture is null)
				return Task.FromResult<byte[]?>(null);

			TaskCompletionSource<byte[]?> CompletionSource = new TaskCompletionSource<byte[]?>(TaskCreationOptions.RunContinuationsAsynchronously);
			Java.IO.File OutputFile = new Java.IO.File(this.Context.CacheDir, $"kyc_capture_{Guid.NewGuid():N}.jpg");
			LogDebug($"Capture requested. Platform=Android, Source=CameraX.ImageCapture, TargetFile={OutputFile.AbsolutePath}");
			ImageCapture.OutputFileOptions OutputOptions = new ImageCapture.OutputFileOptions.Builder(OutputFile).Build();

			if (CancellationToken.CanBeCanceled)
			{
				CancellationToken.Register(() =>
				{
					try
					{
						if (OutputFile.Exists())
							OutputFile.Delete();
					}
					catch
					{
					}

					CompletionSource.TrySetCanceled(CancellationToken);
				});
			}

			CancellationToken.ThrowIfCancellationRequested();
			this.imageCapture.TakePicture(
				OutputOptions,
				ContextCompat.GetMainExecutor(this.Context),
				new ImageSavedCallback(OutputFile, CompletionSource));

			return CompletionSource.Task;
		}

		private partial Task SetTorchInternalAsync(bool IsEnabled, CancellationToken CancellationToken)
		{
			if (this.camera is null)
				return Task.CompletedTask;
			IListenableFuture Future = this.camera.CameraControl.EnableTorch(IsEnabled);
			return AwaitTorchFutureAsync(Future, CancellationToken);
		}

		private static async Task AwaitTorchFutureAsync(IListenableFuture Future, CancellationToken CancellationToken)
		{
			try
			{
				await AwaitFutureAsync(Future, CancellationToken).ConfigureAwait(false);
			}
			catch (System.Exception Ex) when (IsCameraTorchOperationCanceled(Ex))
			{
				LogDebug("Ignoring canceled torch operation.");
			}
		}

		private static bool IsCameraTorchOperationCanceled(System.Exception Ex)
		{
			string ExceptionText = Ex.ToString();
			return ExceptionText.Contains("CameraControl$OperationCanceledException", StringComparison.Ordinal);
		}

		private partial Task SetZoomInternalAsync(float ZoomRatio, CancellationToken CancellationToken)
		{
			if (this.camera is null)
				return Task.CompletedTask;

			float TargetRatio = ZoomRatio <= 0f ? 1f : ZoomRatio;
			IListenableFuture Future = this.camera.CameraControl.SetZoomRatio(TargetRatio);
			return AwaitFutureAsync(Future, CancellationToken);
		}

		private partial Task SetFocusPointInternalAsync(Microsoft.Maui.Graphics.Point? FocusPoint, CancellationToken CancellationToken)
		{
			if (this.camera is null || this.PlatformView is null || FocusPoint is null)
				return Task.CompletedTask;

			double Width = this.PlatformView.Width;
			double Height = this.PlatformView.Height;
			if (Width <= 0 || Height <= 0)
				return Task.CompletedTask;

			float X = (float)(FocusPoint.Value.X * Width);
			float Y = (float)(FocusPoint.Value.Y * Height);
			MeteringPointFactory Factory = this.PlatformView.MeteringPointFactory;
			MeteringPoint Point = Factory.CreatePoint(X, Y);
			FocusMeteringAction Action = new FocusMeteringAction.Builder(Point)
				.SetAutoCancelDuration(3, TimeUnit.Seconds)
				.Build();

			IListenableFuture Future = this.camera.CameraControl.StartFocusAndMetering(Action);
			return AwaitFutureAsync(Future, CancellationToken);
		}

		private partial void OnSelectedCameraChanged(CameraDescriptor? SelectedCamera)
		{
			_ = this.RestartPreviewAsync();
		}

		private partial void OnOptionsChanged(CameraOptions Options)
		{
			_ = this.RestartPreviewAsync();
		}

		private partial void HandleFrameDemandChanged(bool HasFrameDemand)
		{
			_ = HasFrameDemand;
			_ = this.RestartPreviewAsync();
		}

		/// <summary>
		/// Restarts only a preview that is still running when the request is admitted.
		/// </summary>
		/// <returns>A task that observes automatic restart failures.</returns>
		private async Task RestartPreviewAsync()
		{
			try
			{
				await this.StartPreviewCoreAsync(true, CancellationToken.None).ConfigureAwait(false);
			}
			catch (System.Exception Ex)
			{
				LogWarning($"Camera restart failed. ExceptionType={Ex.GetType().FullName}");
			}
		}

		/// <summary>
		/// Unbinds and disposes owned resources on the main thread while the camera semaphore is held.
		/// </summary>
		private void StopPreviewInternal()
		{
			List<UseCase> UseCases = new List<UseCase>();
			if (this.imageCapture is not null)
				UseCases.Add(this.imageCapture);
			if (this.imageAnalysis is not null)
				UseCases.Add(this.imageAnalysis);
			if (this.preview is not null)
				UseCases.Add(this.preview);

			List<System.Exception> Failures = new List<System.Exception>();
			AttemptCleanup(() => this.imageAnalysis?.ClearAnalyzer(), Failures);
			if (UseCases.Count > 0)
				AttemptCleanup(() => this.cameraProvider?.Unbind(UseCases.ToArray()), Failures);

			foreach (UseCase UseCase in UseCases)
				AttemptCleanup(UseCase.Dispose, Failures);
			AttemptCleanup(() => this.analyzerExecutor?.Shutdown(), Failures);
			AttemptCleanup(() => this.analyzerExecutor?.Dispose(), Failures);

			this.imageCapture = null;
			this.imageAnalysis = null;
			this.preview = null;
			this.analyzerExecutor = null;
			this.cameraProvider = null;
			this.camera = null;
			AttemptCleanup(() =>
			{
				if (this.VirtualView is not null)
					this.VirtualView.IsPreviewRunning = false;
			}, Failures);

			if (Failures.Count > 0)
				throw new AggregateException("Camera cleanup failed.", Failures);
		}

		/// <summary>
		/// Records a cleanup failure while allowing remaining native resources to be released.
		/// </summary>
		/// <param name="CleanupAction">The native cleanup step.</param>
		/// <param name="Failures">The failures to return to the initiating caller.</param>
		private static void AttemptCleanup(System.Action CleanupAction, List<System.Exception> Failures)
		{
			try
			{
				CleanupAction();
			}
			catch (System.Exception Ex)
			{
				Failures.Add(Ex);
				LogWarning($"Camera resource cleanup failed. ExceptionType={Ex.GetType().FullName}");
			}
		}

		private CameraSelector BuildCameraSelector()
		{
			CameraPosition Position = this.selectedCamera?.Position ?? (this.options.PreferRearCamera ? CameraPosition.Rear : CameraPosition.Front);
			CameraSelector.Builder Builder = new CameraSelector.Builder();
			if (Position == CameraPosition.Front)
				Builder.RequireLensFacing(CameraSelector.LensFacingFront);
			else
				Builder.RequireLensFacing(CameraSelector.LensFacingBack);

			return Builder.Build();
		}

		private static void ApplyPreviewScaling(PreviewView PreviewControl, CameraPreviewScaling PreviewScaling)
		{
			PreviewView.ScaleType ScaleType = PreviewScaling switch
			{
				CameraPreviewScaling.Fit => PreviewView.ScaleType.FitCenter,
				_ => PreviewView.ScaleType.FillCenter
			};
			PreviewControl.SetScaleType(ScaleType);
		}

		private static void ApplyTargetResolution(Preview.Builder Builder, CameraOptions Options)
		{
			if (Options.TargetResolution is null)
				return;

			Microsoft.Maui.Graphics.Size Target = Options.TargetResolution.Value;
			if (Target.Width <= 0 || Target.Height <= 0)
				return;

			Android.Util.Size PreferredResolution = new Android.Util.Size((int)Target.Width, (int)Target.Height);
			AndroidX.Camera.Core.ResolutionSelector.ResolutionStrategy Strategy =
				new AndroidX.Camera.Core.ResolutionSelector.ResolutionStrategy(
					PreferredResolution,
					AndroidX.Camera.Core.ResolutionSelector.ResolutionStrategy.FallbackRuleClosestHigherThenLower);
			AndroidX.Camera.Core.ResolutionSelector.ResolutionSelector Selector =
				new AndroidX.Camera.Core.ResolutionSelector.ResolutionSelector.Builder()
					.SetResolutionStrategy(Strategy)
					.Build();

			Builder.SetResolutionSelector(Selector);
		}

		private static void ApplyTargetResolution(ImageAnalysis.Builder Builder, CameraOptions Options)
		{
			if (Options.TargetResolution is null)
				return;

			Microsoft.Maui.Graphics.Size Target = Options.TargetResolution.Value;
			if (Target.Width <= 0 || Target.Height <= 0)
				return;

			Android.Util.Size PreferredResolution = new Android.Util.Size((int)Target.Width, (int)Target.Height);
			AndroidX.Camera.Core.ResolutionSelector.ResolutionStrategy Strategy =
				new AndroidX.Camera.Core.ResolutionSelector.ResolutionStrategy(
					PreferredResolution,
					AndroidX.Camera.Core.ResolutionSelector.ResolutionStrategy.FallbackRuleClosestHigherThenLower);
			AndroidX.Camera.Core.ResolutionSelector.ResolutionSelector Selector =
				new AndroidX.Camera.Core.ResolutionSelector.ResolutionSelector.Builder()
					.SetResolutionStrategy(Strategy)
					.Build();

			Builder.SetResolutionSelector(Selector);
		}

		private static void ApplyTargetResolution(ImageCapture.Builder Builder, CameraOptions Options)
		{
			if (Options.TargetResolution is null)
				return;

			Microsoft.Maui.Graphics.Size Target = Options.TargetResolution.Value;
			if (Target.Width <= 0 || Target.Height <= 0)
				return;

			Android.Util.Size PreferredResolution = new Android.Util.Size((int)Target.Width, (int)Target.Height);
			AndroidX.Camera.Core.ResolutionSelector.ResolutionStrategy Strategy =
				new AndroidX.Camera.Core.ResolutionSelector.ResolutionStrategy(
					PreferredResolution,
					AndroidX.Camera.Core.ResolutionSelector.ResolutionStrategy.FallbackRuleClosestHigherThenLower);
			AndroidX.Camera.Core.ResolutionSelector.ResolutionSelector Selector =
				new AndroidX.Camera.Core.ResolutionSelector.ResolutionSelector.Builder()
					.SetResolutionStrategy(Strategy)
					.Build();

			Builder.SetResolutionSelector(Selector);
		}

		private ILifecycleOwner GetLifecycleOwner()
		{
			if (this.MauiContext?.Context is ILifecycleOwner Owner)
				return Owner;

			throw new InvalidOperationException("Camera preview requires a lifecycle owner.");
		}

		private static Task<ProcessCameraProvider> GetCameraProviderAsync(Context Context, CancellationToken CancellationToken)
		{
			TaskCompletionSource<ProcessCameraProvider> Completion = new TaskCompletionSource<ProcessCameraProvider>(TaskCreationOptions.RunContinuationsAsynchronously);
			IListenableFuture Future = ProcessCameraProvider.GetInstance(Context);
			if (CancellationToken.CanBeCanceled)
			{
				CancellationToken.Register(() => Completion.TrySetCanceled(CancellationToken));
			}

			Future.AddListener(new Runnable(() =>
			{
				try
				{
					ProcessCameraProvider Provider = (ProcessCameraProvider)Future.Get();
					Completion.TrySetResult(Provider);
				}
				catch (System.Exception Ex)
				{
					Completion.TrySetException(Ex);
				}
			}), ContextCompat.GetMainExecutor(Context));

			return Completion.Task;
		}

		private static Task AwaitFutureAsync(IListenableFuture Future, CancellationToken CancellationToken)
		{
			TaskCompletionSource<bool> Completion = new TaskCompletionSource<bool>(TaskCreationOptions.RunContinuationsAsynchronously);
			if (CancellationToken.CanBeCanceled)
				CancellationToken.Register(() => Completion.TrySetCanceled(CancellationToken));

			Future.AddListener(new Runnable(() =>
			{
				try
				{
					Future.Get();
					Completion.TrySetResult(true);
				}
				catch (System.Exception Ex)
				{
					Completion.TrySetException(Ex);
				}
			}), ContextCompat.GetMainExecutor(Android.App.Application.Context));

			return Completion.Task;
		}

		private static void LogDebug(string Message)
		{
			Debug.WriteLine($"[CameraViewHandler] {Message}");
		}

		private static void LogWarning(string Message)
		{
			Android.Util.Log.Warn(nameof(CameraViewHandler), Message);
		}

		private void HandleImageProxy(IImageProxy Image)
		{
			if (this.VirtualView is null)
				return;
			if (!this.ShouldEnableFrameAnalysis())
				return;

			DateTimeOffset Now = DateTimeOffset.UtcNow;
			TimeSpan Interval = this.options.FrameDeliveryInterval;
			if (Interval > TimeSpan.Zero && Now - this.lastFrameTimestamp < Interval)
				return;

			int Width = Image.Width;
			int Height = Image.Height;
			if (Width <= 0 || Height <= 0)
			{
				this.LogFrameDrop("InvalidDimensions", Width, Height, 0, 0, 0, 0, 0);
				return;
			}

			IImageProxyPlaneProxy[]? Planes = Image.GetPlanes();
			int PlaneCount = Planes?.Length ?? 0;
			if (Planes is null || PlaneCount == 0)
			{
				this.LogFrameDrop("MissingLumaPlane", Width, Height, PlaneCount, 0, 0, 0, 0);
				return;
			}

			IImageProxyPlaneProxy LumaPlane = Planes[0];
			Java.Nio.ByteBuffer Buffer = LumaPlane.Buffer.Duplicate();
			int RowStride = LumaPlane.RowStride;
			int PixelStride = LumaPlane.PixelStride;
			int BufferCapacity = Buffer.Capacity();
			Buffer.Rewind();
			int BufferRemaining = Buffer.Remaining();
			if (RowStride <= 0 || PixelStride <= 0 || BufferCapacity <= 0 || BufferRemaining <= 0)
			{
				this.LogFrameDrop("InvalidPlaneMetadata", Width, Height, PlaneCount, RowStride, PixelStride, BufferRemaining, BufferCapacity);
				return;
			}

			long RequiredRowBytesLong = ((long)(Width - 1) * PixelStride) + 1L;
			if (RequiredRowBytesLong <= 0 || RequiredRowBytesLong > RowStride || RequiredRowBytesLong > int.MaxValue)
			{
				this.LogFrameDrop("UnsupportedRowLayout", Width, Height, PlaneCount, RowStride, PixelStride, BufferRemaining, BufferCapacity);
				return;
			}

			int RequiredRowBytes = (int)RequiredRowBytesLong;

			byte[] Luminance = new byte[Width * Height];
			byte[] RowBuffer = new byte[RowStride];

			for (int Row = 0; Row < Height; Row++)
			{
				int AvailableRowBytes = System.Math.Min(RowStride, Buffer.Remaining());
				if (AvailableRowBytes < RequiredRowBytes)
				{
					this.LogFrameDrop("InsufficientRowData", Width, Height, PlaneCount, RowStride, PixelStride, Buffer.Remaining(), BufferCapacity);
					return;
				}

				Buffer.Get(RowBuffer, 0, AvailableRowBytes);

				int RowOffset = Row * Width;
				for (int Column = 0; Column < Width; Column++)
				{
					int SourceIndex = Column * PixelStride;
					if (SourceIndex >= AvailableRowBytes)
					{
						this.LogFrameDrop("PixelStrideOutsideRow", Width, Height, PlaneCount, RowStride, PixelStride, AvailableRowBytes, BufferCapacity);
						return;
					}

					Luminance[RowOffset + Column] = RowBuffer[SourceIndex];
				}
			}

			this.lastFrameTimestamp = Now;
			int Rotation = Image.ImageInfo.RotationDegrees;
			CameraFrame Frame = new CameraFrame(Width, Height, CameraFrameFormat.Grayscale8, Rotation, Now, Luminance);
			this.VirtualView.RaiseFrameReady(Frame);
		}

		private bool ShouldEnableFrameAnalysis()
		{
			return this.VirtualView?.HasFrameSubscribers ?? false;
		}

		private void LogFrameDrop(string Reason, int Width, int Height, int PlaneCount, int RowStride, int PixelStride, int BufferRemaining, int BufferCapacity)
		{
			string CameraId = this.selectedCamera?.Id ?? "Unknown";
			string CameraPositionName = this.selectedCamera?.Position.ToString()
				?? (this.options.PreferRearCamera ? NeuroAccessMaui.Camera.CameraPosition.Rear.ToString() : NeuroAccessMaui.Camera.CameraPosition.Front.ToString());
			LogWarning(
				$"Dropped Android preview frame. Reason={Reason}, CameraId={CameraId}, CameraPosition={CameraPositionName}, Width={Width}, Height={Height}, PlaneCount={PlaneCount}, RowStride={RowStride}, PixelStride={PixelStride}, BufferRemaining={BufferRemaining}, BufferCapacity={BufferCapacity}");
		}

			private sealed class FrameAnalyzer : Java.Lang.Object, ImageAnalysis.IAnalyzer
			{
			private readonly CameraViewHandler handler;

			public FrameAnalyzer(CameraViewHandler Handler)
			{
				this.handler = Handler;
			}

			public Android.Util.Size? DefaultTargetResolution
			{
				get
				{
					return null;
				}
			}

			public int TargetCoordinateSystem
			{
				get
				{
					return ImageAnalysis.CoordinateSystemOriginal;
				}
			}

			public void Analyze(IImageProxy? Image)
			{
				if (Image is null)
					return;

				try
				{
					this.handler.HandleImageProxy(Image);
				}
				catch (System.Exception Exception)
				{
					LogWarning($"Unhandled Android frame analysis error. ExceptionType={Exception.GetType().FullName}");
				}
				finally
				{
					Image.Close();
				}
			}

			public void UpdateTransform(Android.Graphics.Matrix? Matrix)
			{
				_ = Matrix;
			}
			}

			private sealed class ImageSavedCallback : Java.Lang.Object, ImageCapture.IOnImageSavedCallback
			{
				private readonly Java.IO.File outputFile;
				private readonly TaskCompletionSource<byte[]?> completionSource;

				public ImageSavedCallback(Java.IO.File OutputFile, TaskCompletionSource<byte[]?> CompletionSource)
				{
					this.outputFile = OutputFile;
					this.completionSource = CompletionSource;
				}

				public void OnError(ImageCaptureException? Exception)
				{
					try
					{
						if (this.outputFile.Exists())
							this.outputFile.Delete();
					}
					catch
					{
					}

					LogDebug($"Capture failed. Platform=Android, Source=CameraX.ImageCapture, Error={Exception?.Message ?? "Unknown"}");
					this.completionSource.TrySetException(new InvalidOperationException(Exception?.Message ?? "Failed to capture photo."));
				}

				public void OnCaptureStarted()
				{
					// No-op: required by newer CameraX callback surface.
				}

				public void OnCaptureProcessProgressed(int Progress)
				{
					_ = Progress;
				}

				public void OnPostviewBitmapAvailable(Android.Graphics.Bitmap? Bitmap)
				{
					_ = Bitmap;
				}

				/// <summary>
				/// Schedules saved-image processing away from the native callback's main executor.
				/// </summary>
				/// <param name="OutputFileResults">The native saved-file result.</param>
				public void OnImageSaved(ImageCapture.OutputFileResults? OutputFileResults)
				{
					_ = OutputFileResults;
					_ = Task.Run(this.ProcessSavedImage);
				}

				/// <summary>
				/// Reads the saved JPEG, completes the capture, and attempts temporary-file cleanup.
				/// </summary>
				private void ProcessSavedImage()
				{
					try
					{
						string Path = this.outputFile.AbsolutePath;
						if (string.IsNullOrWhiteSpace(Path) || !File.Exists(Path))
						{
							this.completionSource.TrySetResult(null);
							return;
						}

						byte[] Bytes = File.ReadAllBytes(Path);
						long OriginalLength = Bytes.LongLength;
						Bytes = TrimJpegPadding(Bytes);
						if (Bytes.LongLength != OriginalLength)
							LogDebug($"Capture trimmed trailing padding. Platform=Android, Source=CameraX.ImageCapture, OriginalBytes={OriginalLength}, TrimmedBytes={Bytes.LongLength}");

						LogDebug($"Capture succeeded. Platform=Android, Source=CameraX.ImageCapture, OutputBytes={Bytes.LongLength}");
						this.completionSource.TrySetResult(Bytes);
					}
					catch (System.Exception Ex)
					{
						this.completionSource.TrySetException(Ex);
					}
					finally
					{
						try
						{
							if (this.outputFile.Exists())
								this.outputFile.Delete();
						}
						catch
						{
						}
					}
				}

				/// <summary>
				/// Trims trailing bytes that some Android CameraX/HAL implementations write after the
				/// JPEG End-Of-Image (EOI) marker (0xFF 0xD9). Without trimming, the captured payload
				/// contains valid JPEG data followed by up to several megabytes of null padding, which
				/// bloats attachment uploads with base64 'A' characters representing zero bytes.
				/// </summary>
				private static byte[] TrimJpegPadding(byte[] Bytes)
				{
					if (Bytes is null || Bytes.LongLength < 4)
						return Bytes ?? System.Array.Empty<byte>();

					// Only trim payloads that begin with the JPEG Start-Of-Image marker (0xFF 0xD8).
					if (Bytes[0] != 0xFF || Bytes[1] != 0xD8)
						return Bytes;

					// Scan backwards from the end for the EOI marker (0xFF 0xD9). Trailing padding
					// is nearly always zero bytes, so this loop typically returns immediately once
					// the padding block is skipped.
					for (long i = Bytes.LongLength - 2; i >= 2; i--)
					{
						if (Bytes[i] == 0xFF && Bytes[i + 1] == 0xD9)
						{
							long TrimmedLength = i + 2;
							if (TrimmedLength == Bytes.LongLength)
								return Bytes;

							byte[] Trimmed = new byte[TrimmedLength];
							System.Buffer.BlockCopy(Bytes, 0, Trimmed, 0, (int)TrimmedLength);
							return Trimmed;
						}
					}

					return Bytes;
				}
			}
		}
	}
#endif
