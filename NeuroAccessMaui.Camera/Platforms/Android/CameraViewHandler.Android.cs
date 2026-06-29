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

		protected override PreviewView CreatePlatformView()
		{
			PreviewView View = new PreviewView(this.Context);
			View.SetScaleType(PreviewView.ScaleType.FillCenter);
			return View;
		}

		private partial void Initialize()
		{
			this.options = this.VirtualView?.Options ?? new CameraOptions();
			this.selectedCamera = this.VirtualView?.SelectedCamera;
		}

		private partial void Cleanup()
		{
			this.StopPreviewInternal();
		}

		private async partial Task StartPreviewInternalAsync(CancellationToken CancellationToken)
		{
			if (this.VirtualView is null || this.PlatformView is null)
				return;

			this.options = this.VirtualView.Options ?? new CameraOptions();

			Context Context = this.Context;
			ProcessCameraProvider Provider = await GetCameraProviderAsync(Context, CancellationToken).ConfigureAwait(false);
			this.cameraProvider = Provider;
			await MainThread.InvokeOnMainThreadAsync(() =>
			{
				this.StopPreviewInternal();
				this.lastFrameTimestamp = DateTimeOffset.MinValue;

				CameraSelector Selector = this.BuildCameraSelector();
				bool ShouldEnableFrameAnalysis = this.ShouldEnableFrameAnalysis();
				ApplyPreviewScaling(this.PlatformView, this.options.PreviewScaling);
				Preview.Builder PreviewBuilder = new Preview.Builder();
				ApplyTargetResolution(PreviewBuilder, this.options);
				Preview Preview = PreviewBuilder.Build();
				Preview.SetSurfaceProvider(ContextCompat.GetMainExecutor(Context), this.PlatformView.SurfaceProvider);

				ImageAnalysis? Analysis = null;
				if (ShouldEnableFrameAnalysis)
				{
					ImageAnalysis.Builder AnalysisBuilder = new ImageAnalysis.Builder();
					AnalysisBuilder.SetBackpressureStrategy(ImageAnalysis.StrategyKeepOnlyLatest);
					ApplyTargetResolution(AnalysisBuilder, this.options);
					Analysis = AnalysisBuilder.Build();
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
				this.preview = Preview;
				this.imageAnalysis = Analysis;
				this.imageCapture = Capture;

				if (this.VirtualView is not null)
					this.VirtualView.IsPreviewRunning = true;
			});
		}

		private partial Task StopPreviewInternalAsync()
		{
			return MainThread.InvokeOnMainThreadAsync(() => this.StopPreviewInternal());
		}

		private partial Task ReleaseInternalAsync()
		{
			return MainThread.InvokeOnMainThreadAsync(() => this.StopPreviewInternal());
		}

		private partial Task<byte[]?> CapturePhotoInternalAsync(CancellationToken CancellationToken)
		{
			if (this.imageCapture is null)
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
			this.selectedCamera = SelectedCamera;
			if (this.VirtualView?.IsPreviewRunning ?? false)
				_ = this.StartPreviewInternalAsync(CancellationToken.None);
		}

		private partial void OnOptionsChanged(CameraOptions Options)
		{
			this.options = Options ?? new CameraOptions();
			if (this.VirtualView?.IsPreviewRunning ?? false)
				_ = this.StartPreviewInternalAsync(CancellationToken.None);
		}

		private partial void HandleFrameDemandChanged(bool HasFrameDemand)
		{
			_ = HasFrameDemand;
			if (this.VirtualView?.IsPreviewRunning ?? false)
				_ = this.StartPreviewInternalAsync(CancellationToken.None);
		}

			private void StopPreviewInternal()
			{
				if (this.imageCapture is not null)
				{
					this.imageCapture.Dispose();
					this.imageCapture = null;
				}

				if (this.imageAnalysis is not null)
				{
					this.imageAnalysis.ClearAnalyzer();
				this.imageAnalysis.Dispose();
				this.imageAnalysis = null;
			}

			if (this.preview is not null)
			{
				this.preview.Dispose();
				this.preview = null;
			}

			if (this.cameraProvider is not null)
				this.cameraProvider.UnbindAll();

			if (this.analyzerExecutor is not null)
			{
				this.analyzerExecutor.Shutdown();
				this.analyzerExecutor.Dispose();
				this.analyzerExecutor = null;
			}

			this.camera = null;
			MainThread.BeginInvokeOnMainThread(() =>
			{
				if (this.VirtualView is not null)
					this.VirtualView.IsPreviewRunning = false;
			});
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
			Debug.WriteLine($"[CameraViewHandler] {Message}");
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
					LogWarning($"Unhandled Android frame analysis error. Message={Exception.Message}");
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

				public void OnImageSaved(ImageCapture.OutputFileResults? OutputFileResults)
				{
					_ = OutputFileResults;

					try
					{
						string Path = this.outputFile.AbsolutePath;
						if (string.IsNullOrWhiteSpace(Path) || !File.Exists(Path))
						{
							this.completionSource.TrySetResult(null);
							return;
						}

						byte[] Bytes = File.ReadAllBytes(Path);
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
			}
		}
	}
#endif
