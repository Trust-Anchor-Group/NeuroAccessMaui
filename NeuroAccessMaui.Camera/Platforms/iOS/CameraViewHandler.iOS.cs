#if IOS
using System;
using System.Collections;
using System.Collections.Generic;
using System.Diagnostics;
using System.Globalization;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;
using AVFoundation;
using CoreFoundation;
using CoreMedia;
using CoreVideo;
using Foundation;
using Microsoft.Maui.Graphics;
using UIKit;

namespace NeuroAccessMaui.Camera
{
	public partial class CameraViewHandler
	{
		private static readonly NSString CaptureSessionWasInterruptedNotification = new NSString("AVCaptureSessionWasInterruptedNotification");
		private static readonly NSString CaptureSessionInterruptionEndedNotification = new NSString("AVCaptureSessionInterruptionEndedNotification");
		private static readonly NSString CaptureSessionRuntimeErrorNotification = new NSString("AVCaptureSessionRuntimeErrorNotification");
		private static readonly NSString CaptureSessionInterruptionReasonKey = new NSString("AVCaptureSessionInterruptionReasonKey");
		private static readonly NSString CaptureSessionErrorKey = new NSString("AVCaptureSessionErrorKey");

		private AVCaptureSession? session;
		private AVCaptureDeviceInput? deviceInput;
		private AVCaptureVideoDataOutput? videoOutput;
		private AVCapturePhotoOutput? photoOutput;
		private DispatchQueue? videoQueue;
		private CameraFrameSampleBufferDelegate? sampleBufferDelegate;
		private PhotoCaptureDelegate? pendingPhotoCaptureDelegate;
		private CameraOptions options = new CameraOptions();
		private CameraDescriptor? selectedCamera;
		private DateTimeOffset lastFrameTimestamp = DateTimeOffset.MinValue;
		private NSObject? sessionWasInterruptedObserver;
		private NSObject? sessionInterruptionEndedObserver;
		private NSObject? sessionRuntimeErrorObserver;
		private readonly SemaphoreSlim sessionLifecycleLock = new SemaphoreSlim(1, 1);
		private bool shouldMaintainPreview;
		private bool isSessionInterrupted;
		private int recoveryInProgress;

		protected override UIView CreatePlatformView()
		{
			CameraPreviewContainerView View = new CameraPreviewContainerView();
			View.BackgroundColor = UIColor.Black;
			return View;
		}

		private partial void Initialize()
		{
			this.options = this.VirtualView?.Options ?? new CameraOptions();
			this.selectedCamera = this.VirtualView?.SelectedCamera;
		}

		private partial void Cleanup()
		{
			CameraPreviewContainerView? ContainerView = this.PlatformView as CameraPreviewContainerView;
			CameraView? VirtualView = this.VirtualView;

			this.ResetLifecycleStateForTeardown();
			if (!this.TryAcquireLifecycleLockForCleanup())
			{
				_ = this.CleanupAsync(ContainerView, VirtualView, true);
				return;
			}

			try
			{
				this.CleanupLocked(ContainerView, VirtualView, true);
			}
			catch (Exception Exception)
			{
				LogWarning($"Failed to clean up iOS preview during handler disconnect: {Exception.Message}");
			}
			finally
			{
				this.sessionLifecycleLock.Release();
			}
		}

		private async partial Task StartPreviewInternalAsync(CancellationToken CancellationToken)
		{
			await this.StartPreviewCoreAsync(CancellationToken, true).ConfigureAwait(false);
		}

		private async partial Task StopPreviewInternalAsync()
		{
			this.shouldMaintainPreview = false;
			CameraPreviewContainerView? PreviewContainerView = this.PlatformView as CameraPreviewContainerView;
			CameraView? VirtualView = this.VirtualView;
			await this.sessionLifecycleLock.WaitAsync().ConfigureAwait(false);
			try
			{
				await this.StopPreviewCoreAsync(false, PreviewContainerView, VirtualView).ConfigureAwait(false);
			}
			finally
			{
				this.sessionLifecycleLock.Release();
			}
		}

		private async partial Task ReleaseInternalAsync()
		{
			this.shouldMaintainPreview = false;
			CameraPreviewContainerView? PreviewContainerView = this.PlatformView as CameraPreviewContainerView;
			CameraView? VirtualView = this.VirtualView;
			await this.sessionLifecycleLock.WaitAsync().ConfigureAwait(false);
			try
			{
				await this.StopPreviewCoreAsync(true, PreviewContainerView, VirtualView).ConfigureAwait(false);
			}
			finally
			{
				this.sessionLifecycleLock.Release();
			}
		}

		private async partial Task<byte[]?> CapturePhotoInternalAsync(CancellationToken CancellationToken)
		{
			await this.sessionLifecycleLock.WaitAsync(CancellationToken).ConfigureAwait(false);

			TaskCompletionSource<byte[]?> CompletionSource = new TaskCompletionSource<byte[]?>(TaskCreationOptions.RunContinuationsAsynchronously);
			CancellationTokenRegistration CancellationRegistration = default;
			PhotoCaptureDelegate? CaptureDelegate = null;
			try
			{
				CancellationToken.ThrowIfCancellationRequested();

				if (this.isDisposed || this.isReleased || !this.shouldMaintainPreview)
					return null;

				AVCapturePhotoOutput? PhotoOutput = this.photoOutput;
				AVCaptureSession? Session = this.session;
				if (PhotoOutput is null || Session is null || !Session.Running)
					return null;

				if (CancellationToken.CanBeCanceled)
				{
					CancellationRegistration = CancellationToken.Register(() =>
					{
						CompletionSource.TrySetCanceled(CancellationToken);
						CaptureDelegate?.Cancel();
					});
				}

				CaptureDelegate = new PhotoCaptureDelegate(
					CompletionSource,
					() =>
					{
						this.TryClearPendingPhotoCaptureDelegate(CaptureDelegate);
						CancellationRegistration.Dispose();
					});
				this.pendingPhotoCaptureDelegate = CaptureDelegate;

				AVCapturePhotoSettings Settings = this.CreatePhotoCaptureSettings();

				if (OperatingSystem.IsIOSVersionAtLeast(13) || OperatingSystem.IsMacCatalystVersionAtLeast(13))
				{
					AVCapturePhotoQualityPrioritization DesiredPrioritization = AVCapturePhotoQualityPrioritization.Quality;
					AVCapturePhotoQualityPrioritization MaxPrioritization = PhotoOutput.MaxPhotoQualityPrioritization;
					AVCapturePhotoQualityPrioritization EffectivePrioritization = ResolveEffectivePhotoQualityPrioritization(MaxPrioritization);
					Settings.PhotoQualityPrioritization = EffectivePrioritization;
					LogDebug($"Photo capture prioritization configured. Desired={DesiredPrioritization}, Max={MaxPrioritization}, Effective={EffectivePrioritization}, FallbackRetry=False");
				}

				try
				{
					PhotoOutput.CapturePhoto(Settings, CaptureDelegate);
				}
				catch (ObjCRuntime.ObjCException Exception) when ((OperatingSystem.IsIOSVersionAtLeast(13) || OperatingSystem.IsMacCatalystVersionAtLeast(13)) && IsPhotoQualityPrioritizationException(Exception))
				{
					LogDebug("CapturePhoto raised a photoQualityPrioritization exception. Retrying once with Speed.");
					AVCapturePhotoQualityPrioritization MaxPrioritization = PhotoOutput.MaxPhotoQualityPrioritization;
					AVCapturePhotoSettings RetrySettings = this.CreatePhotoCaptureSettings();
					RetrySettings.PhotoQualityPrioritization = AVCapturePhotoQualityPrioritization.Speed;
					LogDebug($"Photo capture prioritization configured. Desired={AVCapturePhotoQualityPrioritization.Quality}, Max={MaxPrioritization}, Effective={AVCapturePhotoQualityPrioritization.Speed}, FallbackRetry=True");
					PhotoOutput.CapturePhoto(RetrySettings, CaptureDelegate);
				}
			}
			catch
			{
				this.TryClearPendingPhotoCaptureDelegate(CaptureDelegate);
				CancellationRegistration.Dispose();
				throw;
			}
			finally
			{
				this.sessionLifecycleLock.Release();
			}

			return await CompletionSource.Task.ConfigureAwait(false);
		}

		private AVCapturePhotoSettings CreatePhotoCaptureSettings()
		{
			bool SupportsJpeg = this.PhotoOutputSupportsJpegCodec();
			AVCapturePhotoSettings Settings;
			if (SupportsJpeg)
			{
				try
				{
					NSDictionary<NSString, NSObject> JpegFormat = new NSDictionary<NSString, NSObject>(AVVideo.CodecKey, new NSString("jpeg"));
					Settings = AVCapturePhotoSettings.FromFormat(JpegFormat);
					TrySetProcessedFileTypeJpeg(Settings);
					LogDebug("Photo capture settings configured with JPEG-preferred codec format.");
				}
				catch (Exception Exception)
				{
					LogWarning($"Failed to create JPEG-specific photo settings. Falling back to default settings. Details={Exception.Message}");
					Settings = AVCapturePhotoSettings.Create();
				}
			}
			else
			{
				Settings = AVCapturePhotoSettings.Create();
			}

			if ((OperatingSystem.IsIOSVersionAtLeast(11) && !OperatingSystem.IsIOSVersionAtLeast(16)) ||
				(OperatingSystem.IsMacCatalystVersionAtLeast(13) && !OperatingSystem.IsMacCatalystVersionAtLeast(16)))
				Settings.IsHighResolutionPhotoEnabled = true;

			return Settings;
		}

		private bool PhotoOutputSupportsJpegCodec()
		{
			if (this.photoOutput is null)
				return false;

			IEnumerable? AvailableCodecs = this.photoOutput.AvailablePhotoCodecTypes;
			if (AvailableCodecs is null)
				return false;

			foreach (object? Codec in AvailableCodecs)
			{
				string CodecName = Codec?.ToString() ?? string.Empty;
				if (CodecName.IndexOf("jpeg", StringComparison.OrdinalIgnoreCase) >= 0)
					return true;
			}

			return false;
		}

		private static void TrySetProcessedFileTypeJpeg(AVCapturePhotoSettings Settings)
		{
			if (Settings is null)
				return;

			try
			{
				Settings.SetValueForKey(new NSString("public.jpeg"), new NSString("processedFileType"));
			}
			catch
			{
				// File type hint is best-effort only.
			}
		}

		private partial Task SetTorchInternalAsync(bool IsEnabled, CancellationToken CancellationToken)
		{
			if (this.deviceInput?.Device is not AVCaptureDevice Device)
				return Task.CompletedTask;

			if (!Device.HasTorch)
				return Task.CompletedTask;

			NSError? Error;
			if (Device.LockForConfiguration(out Error))
			{
				Device.TorchMode = IsEnabled ? AVCaptureTorchMode.On : AVCaptureTorchMode.Off;
				Device.UnlockForConfiguration();
			}

			return Task.CompletedTask;
		}

		private partial Task SetZoomInternalAsync(float ZoomRatio, CancellationToken CancellationToken)
		{
			if (this.deviceInput?.Device is not AVCaptureDevice Device)
				return Task.CompletedTask;

			float TargetZoom = ZoomRatio <= 1f ? 1f : ZoomRatio;
			TargetZoom = Math.Min(TargetZoom, (float)Device.ActiveFormat.VideoMaxZoomFactor);

			NSError? Error;
			if (Device.LockForConfiguration(out Error))
			{
				Device.VideoZoomFactor = TargetZoom;
				Device.UnlockForConfiguration();
			}

			return Task.CompletedTask;
		}

		private partial Task SetFocusPointInternalAsync(Point? FocusPoint, CancellationToken CancellationToken)
		{
			if (FocusPoint is null)
				return Task.CompletedTask;
			if (this.deviceInput?.Device is not AVCaptureDevice Device)
				return Task.CompletedTask;
			if (!Device.FocusPointOfInterestSupported || !Device.IsFocusModeSupported(AVCaptureFocusMode.AutoFocus))
				return Task.CompletedTask;

			NSError? Error;
			if (Device.LockForConfiguration(out Error))
			{
				Device.FocusPointOfInterest = new CoreGraphics.CGPoint(FocusPoint.Value.X, FocusPoint.Value.Y);
				Device.FocusMode = AVCaptureFocusMode.AutoFocus;
				Device.UnlockForConfiguration();
			}

			return Task.CompletedTask;
		}

		private partial void OnSelectedCameraChanged(CameraDescriptor? SelectedCamera)
		{
			this.selectedCamera = SelectedCamera;
			if (this.VirtualView?.IsPreviewRunning ?? false)
				_ = this.RestartPreviewAsync("SelectedCameraChanged");
		}

		private partial void OnOptionsChanged(CameraOptions Options)
		{
			this.options = Options ?? new CameraOptions();
			if (this.VirtualView?.IsPreviewRunning ?? false)
				_ = this.RestartPreviewAsync("OptionsChanged");
		}

		private partial void HandleFrameDemandChanged(bool HasFrameDemand)
		{
			if (this.VirtualView?.IsPreviewRunning ?? false)
				_ = this.RestartPreviewAsync($"FrameDemandChanged:{HasFrameDemand}");
		}

		private bool ConfigureSession()
		{
			if (this.session is null)
				this.session = new AVCaptureSession();

			this.RegisterSessionObservers();

			this.session.BeginConfiguration();
			try
			{
				if (this.deviceInput is not null)
				{
					this.session.RemoveInput(this.deviceInput);
					this.deviceInput.Dispose();
					this.deviceInput = null;
				}

				AVCaptureDevice? Device = this.GetSelectedDevice();
				if (Device is null)
				{
					LogWarning("No suitable camera device was found for current configuration.");
					return false;
				}

				AVCaptureDeviceInput? Input = AVCaptureDeviceInput.FromDevice(Device, out NSError? InputError);
				if (InputError is not null)
				{
					if (Input is not null)
						Input.Dispose();

					LogWarning($"Unable to create camera input: {InputError.LocalizedDescription}");
					return false;
				}

				if (Input is null)
				{
					LogWarning("Unable to create camera input: factory returned null.");
					return false;
				}

				if (!this.session.CanAddInput(Input))
				{
					Input.Dispose();
					LogWarning("Unable to add camera input to capture session.");
					return false;
				}

				this.session.AddInput(Input);
				this.deviceInput = Input;

				bool ShouldEnableFrameOutput = this.ShouldEnableFrameOutput();
				if (ShouldEnableFrameOutput)
				{
					this.EnsureVideoOutputConfigured();
				}
				else
				{
					this.RemoveVideoOutputFromSession();
				}

				if (this.photoOutput is null)
					this.photoOutput = new AVCapturePhotoOutput();

				if (this.photoOutput is not null)
				{
					bool PhotoOutputAlreadyAdded = Array.Exists(this.session.Outputs, Output => Output == this.photoOutput);
					if (!PhotoOutputAlreadyAdded && this.session.CanAddOutput(this.photoOutput))
						this.session.AddOutput(this.photoOutput);

					if ((OperatingSystem.IsIOSVersionAtLeast(11) && !OperatingSystem.IsIOSVersionAtLeast(16)) ||
						(OperatingSystem.IsMacCatalystVersionAtLeast(13) && !OperatingSystem.IsMacCatalystVersionAtLeast(16)))
						this.photoOutput.IsHighResolutionCaptureEnabled = true;
				}

				if (!this.ApplySessionPreset())
				{
					LogWarning("Unable to apply any compatible session preset.");
					return false;
				}

				AVCaptureSession ConfiguredSession = this.session!;
				string SessionPreset = ConfiguredSession.SessionPreset?.ToString() ?? string.Empty;
				string CameraPositionName = this.selectedCamera?.Position.ToString() ?? NeuroAccessMaui.Camera.CameraPosition.Unknown.ToString();
				string CameraId = this.selectedCamera?.Id ?? Device.UniqueID;
				string TargetResolution = this.options.TargetResolution.HasValue
					? $"{this.options.TargetResolution.Value.Width}x{this.options.TargetResolution.Value.Height}"
					: "null";
				LogDebug($"iOS session configured. CameraId={CameraId}, CameraPosition={CameraPositionName}, SessionPreset={SessionPreset}, TargetResolution={TargetResolution}, FrameOutputEnabled={ShouldEnableFrameOutput}");

				return true;
			}
			finally
			{
				this.session.CommitConfiguration();
			}
		}

		private bool ApplySessionPreset()
		{
			if (this.session is null)
				return false;

			IReadOnlyList<NSString> PreferredPresets = this.ResolvePreferredSessionPresets();
			foreach (NSString PreferredPreset in PreferredPresets)
			{
				if (!this.session.CanSetSessionPreset(PreferredPreset))
					continue;

				this.session.SessionPreset = PreferredPreset;
				LogDebug($"Configured session preset: {PreferredPreset}");
				return true;
			}

			return false;
		}

		private Task EnsurePreviewLayerAttachedAsync(AVCaptureSession Session, AVLayerVideoGravity VideoGravity, CameraPreviewContainerView? PreviewContainerView = null)
		{
			return this.RunOnMainQueueAsync(() =>
			{
				CameraPreviewContainerView? ContainerView = PreviewContainerView ?? this.PlatformView as CameraPreviewContainerView;
				if (ContainerView is null)
					return;

				if (ContainerView.PreviewLayer is null)
				{
					ContainerView.PreviewLayer = new AVCaptureVideoPreviewLayer(Session)
					{
						VideoGravity = VideoGravity
					};
					ContainerView.Layer.AddSublayer(ContainerView.PreviewLayer);
				}
				else
				{
					ContainerView.PreviewLayer.Session = Session;
					ContainerView.PreviewLayer.VideoGravity = VideoGravity;
				}

				this.ApplyCurrentVideoOrientationToConnections(ContainerView);
			});
		}

		private Task ClearPreviewLayerSessionAsync(CameraPreviewContainerView? PreviewContainerView = null)
		{
			return this.RunOnMainQueueAsync(() =>
			{
				CameraPreviewContainerView? ContainerView = PreviewContainerView ?? this.PlatformView as CameraPreviewContainerView;
				if (ContainerView is null)
					return;

				if (ContainerView.PreviewLayer is not null)
					ContainerView.PreviewLayer.Session = null;
			});
		}

		private Task DetachPreviewLayerAsync(CameraPreviewContainerView? PreviewContainerView = null)
		{
			return this.RunOnMainQueueAsync(() =>
			{
				CameraPreviewContainerView? ContainerView = PreviewContainerView ?? this.PlatformView as CameraPreviewContainerView;
				if (ContainerView is null)
					return;

				AVCaptureVideoPreviewLayer? PreviewLayer = ContainerView.PreviewLayer;
				if (PreviewLayer is null)
					return;

				PreviewLayer.Session = null;
				PreviewLayer.RemoveFromSuperLayer();
				PreviewLayer.Dispose();
				ContainerView.PreviewLayer = null;
			});
		}

		private Task SetPreviewRunningStateAsync(bool IsRunning, CameraView? TargetView = null)
		{
			return this.RunOnMainQueueAsync(() =>
			{
				CameraView? ResolvedView = TargetView ?? this.VirtualView;
				if (ResolvedView is not null)
					ResolvedView.IsPreviewRunning = IsRunning;
			});
		}

		private void ClearPreviewLayerSession(CameraPreviewContainerView? PreviewContainerView = null)
		{
			this.RunOnMainQueueSynchronously(() =>
			{
				CameraPreviewContainerView? ContainerView = PreviewContainerView ?? this.PlatformView as CameraPreviewContainerView;
				if (ContainerView is null)
					return;

				if (ContainerView.PreviewLayer is not null)
					ContainerView.PreviewLayer.Session = null;
			});
		}

		private void DetachPreviewLayer(CameraPreviewContainerView? PreviewContainerView = null)
		{
			this.RunOnMainQueueSynchronously(() =>
			{
				CameraPreviewContainerView? ContainerView = PreviewContainerView ?? this.PlatformView as CameraPreviewContainerView;
				if (ContainerView is null)
					return;

				AVCaptureVideoPreviewLayer? PreviewLayer = ContainerView.PreviewLayer;
				if (PreviewLayer is null)
					return;

				PreviewLayer.Session = null;
				PreviewLayer.RemoveFromSuperLayer();
				PreviewLayer.Dispose();
				ContainerView.PreviewLayer = null;
			});
		}

		private void SetPreviewRunningState(bool IsRunning, CameraView? TargetView = null)
		{
			this.RunOnMainQueueSynchronously(() =>
			{
				CameraView? ResolvedView = TargetView ?? this.VirtualView;
				if (ResolvedView is not null)
					ResolvedView.IsPreviewRunning = IsRunning;
			});
		}

		private async Task StopPreviewCoreAsync(bool ReleasePreviewLayer, CameraPreviewContainerView? PreviewContainerView, CameraView? VirtualView)
		{
			AVCaptureSession? Session = this.session;
			if (Session?.Running == true)
			{
				await Task.Run(() => Session.StopRunning()).ConfigureAwait(false);
			}

			this.CancelPendingPhotoCaptureLocked();
			this.ResetLifecycleStateForTeardown();

			if (ReleasePreviewLayer)
				await this.DetachPreviewLayerAsync(PreviewContainerView).ConfigureAwait(false);
			else
				await this.ClearPreviewLayerSessionAsync(PreviewContainerView).ConfigureAwait(false);

			this.DisposeSessionResources(Session);
			await this.SetPreviewRunningStateAsync(false, VirtualView).ConfigureAwait(false);

			LogDebug(ReleasePreviewLayer ? "iOS preview released." : "iOS preview stopped.");
		}

		private bool TryAcquireLifecycleLockForCleanup()
		{
			if (NSThread.IsMain)
				return this.sessionLifecycleLock.Wait(0);

			this.sessionLifecycleLock.Wait();
			return true;
		}

		private async Task CleanupAsync(CameraPreviewContainerView? PreviewContainerView, CameraView? VirtualView, bool ReleasePreviewLayer)
		{
			try
			{
				await this.sessionLifecycleLock.WaitAsync().ConfigureAwait(false);
				try
				{
					AVCaptureSession? Session = this.session;
					if (Session?.Running == true)
						await Task.Run(() => Session.StopRunning()).ConfigureAwait(false);

					this.CancelPendingPhotoCaptureLocked();
					this.ResetLifecycleStateForTeardown();
					if (ReleasePreviewLayer)
						await this.DetachPreviewLayerAsync(PreviewContainerView).ConfigureAwait(false);
					else
						await this.ClearPreviewLayerSessionAsync(PreviewContainerView).ConfigureAwait(false);

					this.DisposeSessionResources(Session);
				}
				finally
				{
					this.sessionLifecycleLock.Release();
				}

				await this.SetPreviewRunningStateAsync(false, VirtualView).ConfigureAwait(false);
				LogDebug(ReleasePreviewLayer ? "iOS preview cleaned up during handler disconnect." : "iOS preview state cleared during handler disconnect.");
			}
			catch (Exception Exception)
			{
				LogWarning($"Failed to clean up iOS preview during handler disconnect: {Exception.Message}");
			}
		}

		private void CleanupLocked(CameraPreviewContainerView? PreviewContainerView, CameraView? VirtualView, bool ReleasePreviewLayer)
		{
			AVCaptureSession? Session = this.session;
			if (Session?.Running == true)
				Task.Run(() => Session.StopRunning()).GetAwaiter().GetResult();

			this.CancelPendingPhotoCaptureLocked();
			this.ResetLifecycleStateForTeardown();

			if (ReleasePreviewLayer)
				this.DetachPreviewLayer(PreviewContainerView);
			else
				this.ClearPreviewLayerSession(PreviewContainerView);

			this.DisposeSessionResources(Session);

			this.SetPreviewRunningState(false, VirtualView);
			LogDebug(ReleasePreviewLayer ? "iOS preview cleaned up during handler disconnect." : "iOS preview state cleared during handler disconnect.");
		}

		private void ResetLifecycleStateForTeardown()
		{
			this.shouldMaintainPreview = false;
			this.isSessionInterrupted = false;
			Interlocked.Exchange(ref this.recoveryInProgress, 0);
		}

		private void CancelPendingPhotoCaptureLocked()
		{
			PhotoCaptureDelegate? PendingPhotoCaptureDelegate = this.pendingPhotoCaptureDelegate;
			this.pendingPhotoCaptureDelegate = null;
			PendingPhotoCaptureDelegate?.Cancel();
		}

		private void TryClearPendingPhotoCaptureDelegate(PhotoCaptureDelegate? CaptureDelegate)
		{
			if (CaptureDelegate is null)
				return;

			if (ReferenceEquals(this.pendingPhotoCaptureDelegate, CaptureDelegate))
				this.pendingPhotoCaptureDelegate = null;
		}

		private void DisposeSessionResources(AVCaptureSession? Session)
		{
			if (this.photoOutput is not null)
			{
				if (Session is not null && Array.Exists(Session.Outputs, Output => Output == this.photoOutput))
					Session.RemoveOutput(this.photoOutput);

				this.photoOutput.Dispose();
				this.photoOutput = null;
			}

			this.RemoveVideoOutputFromSession();

			if (this.deviceInput is not null)
			{
				if (Session is not null && Array.Exists(Session.Inputs, Input => Input == this.deviceInput))
					Session.RemoveInput(this.deviceInput);

				this.deviceInput.Dispose();
				this.deviceInput = null;
			}

			this.UnregisterSessionObservers();

			if (this.session is not null)
			{
				this.session.Dispose();
				this.session = null;
			}
		}

		private IReadOnlyList<NSString> ResolvePreferredSessionPresets()
		{
			if (this.options.TargetResolution is null)
				return new[] { AVCaptureSession.Preset1280x720, AVCaptureSession.Preset640x480 };

			Size TargetResolution = this.options.TargetResolution.Value;
			if (TargetResolution.Width <= 0 || TargetResolution.Height <= 0)
				return new[] { AVCaptureSession.Preset1280x720, AVCaptureSession.Preset640x480 };

			if (TargetResolution.Width >= 1920 || TargetResolution.Height >= 1080)
				return new[] { AVCaptureSession.Preset1920x1080, AVCaptureSession.Preset1280x720, AVCaptureSession.Preset640x480 };

			if (TargetResolution.Width >= 1280 || TargetResolution.Height >= 720)
				return new[] { AVCaptureSession.Preset1280x720, AVCaptureSession.Preset640x480 };

			return new[] { AVCaptureSession.Preset640x480 };
		}

		private AVCaptureDevice? GetSelectedDevice()
		{
			CameraPosition Position = this.selectedCamera?.Position ?? (this.options.PreferRearCamera ? CameraPosition.Rear : CameraPosition.Front);
			AVCaptureDevicePosition DesiredPosition = Position == CameraPosition.Front
				? AVCaptureDevicePosition.Front
				: AVCaptureDevicePosition.Back;

			string? RequestedDeviceId = this.selectedCamera?.Id;
			if (!string.IsNullOrWhiteSpace(RequestedDeviceId))
			{
				AVCaptureDevice? MatchedDevice = DiscoverDeviceByUniqueId(RequestedDeviceId);
				if (MatchedDevice is not null)
					return MatchedDevice;
			}

			AVCaptureDevice? PositionedDevice = DiscoverPreferredDevice(DesiredPosition);
			if (PositionedDevice is not null)
				return PositionedDevice;

			return DiscoverPreferredDevice(AVCaptureDevicePosition.Unspecified);
		}

		private static AVCaptureDevice? DiscoverDeviceByUniqueId(string UniqueId)
		{
			AVCaptureDeviceDiscoverySession DiscoverySession = AVCaptureDeviceDiscoverySession.Create(
				GetAllSupportedDeviceTypes(),
				AVMediaTypes.Video,
				AVCaptureDevicePosition.Unspecified);

			return DiscoverySession.Devices.FirstOrDefault(Device => string.Equals(Device.UniqueID, UniqueId, StringComparison.Ordinal));
		}

		private static AVCaptureDevice? DiscoverPreferredDevice(AVCaptureDevicePosition DesiredPosition)
		{
			IReadOnlyList<AVCaptureDeviceType> DeviceTypes = GetPrioritizedDeviceTypes(DesiredPosition);
			foreach (AVCaptureDeviceType DeviceType in DeviceTypes)
			{
				AVCaptureDevice? Device = AVCaptureDevice.GetDefaultDevice(DeviceType, AVMediaTypes.Video, DesiredPosition);
				if (Device is not null)
					return Device;
			}

			return null;
		}

		private static IReadOnlyList<AVCaptureDeviceType> GetPrioritizedDeviceTypes(AVCaptureDevicePosition DesiredPosition)
		{
			if (DesiredPosition == AVCaptureDevicePosition.Front)
			{
				return new[]
				{
					AVCaptureDeviceType.BuiltInTrueDepthCamera,
					AVCaptureDeviceType.BuiltInWideAngleCamera
				};
			}

			return new[]
			{
				AVCaptureDeviceType.BuiltInTripleCamera,
				AVCaptureDeviceType.BuiltInDualWideCamera,
				AVCaptureDeviceType.BuiltInDualCamera,
				AVCaptureDeviceType.BuiltInUltraWideCamera,
				AVCaptureDeviceType.BuiltInWideAngleCamera
			};
		}

		private static AVCaptureDeviceType[] GetAllSupportedDeviceTypes()
		{
			HashSet<AVCaptureDeviceType> DeviceTypes = new HashSet<AVCaptureDeviceType>(GetPrioritizedDeviceTypes(AVCaptureDevicePosition.Back));
			foreach (AVCaptureDeviceType DeviceType in GetPrioritizedDeviceTypes(AVCaptureDevicePosition.Front))
			{
				DeviceTypes.Add(DeviceType);
			}

			return DeviceTypes.ToArray();
		}

		private Task RunOnMainQueueAsync(Action Action)
		{
			if (NSThread.IsMain)
			{
				Action();
				return Task.CompletedTask;
			}

			TaskCompletionSource<bool> CompletionSource = new TaskCompletionSource<bool>(TaskCreationOptions.RunContinuationsAsynchronously);
			DispatchQueue.MainQueue.DispatchAsync(() =>
			{
				try
				{
					Action();
					CompletionSource.TrySetResult(true);
				}
				catch (Exception Exception)
				{
					CompletionSource.TrySetException(Exception);
				}
			});

			return CompletionSource.Task;
		}

		private Task RunOnMainQueueAsync(Func<Task> Action)
		{
			if (NSThread.IsMain)
				return Action();

			TaskCompletionSource<bool> CompletionSource = new TaskCompletionSource<bool>(TaskCreationOptions.RunContinuationsAsynchronously);
			DispatchQueue.MainQueue.DispatchAsync(async () =>
			{
				try
				{
					await Action().ConfigureAwait(false);
					CompletionSource.TrySetResult(true);
				}
				catch (Exception Exception)
				{
					CompletionSource.TrySetException(Exception);
				}
			});

			return CompletionSource.Task;
		}

		private void RunOnMainQueueSynchronously(Action Action)
		{
			if (NSThread.IsMain)
			{
				Action();
				return;
			}

			this.RunOnMainQueueAsync(Action).GetAwaiter().GetResult();
		}

		private void UpdatePreviewRunningState(bool IsRunning)
		{
			DispatchQueue.MainQueue.DispatchAsync(() =>
			{
				if (this.VirtualView is not null)
					this.VirtualView.IsPreviewRunning = IsRunning;
			});
		}

		private void HandleSampleBuffer(CMSampleBuffer SampleBuffer, int RotationDegrees)
		{
			if (this.VirtualView is null)
				return;
			if (!this.ShouldEnableFrameOutput())
				return;

			DateTimeOffset Now = DateTimeOffset.UtcNow;
			TimeSpan Interval = this.options.FrameDeliveryInterval;
			if (Interval > TimeSpan.Zero && Now - this.lastFrameTimestamp < Interval)
				return;

			CVPixelBuffer? PixelBuffer = SampleBuffer.GetImageBuffer() as CVPixelBuffer;
			if (PixelBuffer is null)
				return;

			PixelBuffer.Lock(CVPixelBufferLock.ReadOnly);
			try
			{
				int Width = (int)PixelBuffer.Width;
				int Height = (int)PixelBuffer.Height;
				if (!TryExtractLuminance(PixelBuffer, Width, Height, out byte[]? Luminance, out string FailureReason) || Luminance is null)
				{
					this.LogFrameDrop(FailureReason, PixelBuffer, Width, Height);
					return;
				}

				this.lastFrameTimestamp = Now;
				CameraFrame Frame = new CameraFrame(Width, Height, CameraFrameFormat.Grayscale8, RotationDegrees, Now, Luminance);
				this.VirtualView.RaiseFrameReady(Frame);
			}
			finally
			{
				PixelBuffer.Unlock(CVPixelBufferLock.ReadOnly);
			}
		}

		private async Task RestartPreviewAsync(string Reason)
		{
			if (!(this.VirtualView?.IsPreviewRunning ?? false))
				return;

			LogDebug($"iOS preview restart requested. Reason={Reason}");
			await this.StartPreviewCoreAsync(CancellationToken.None, false).ConfigureAwait(false);
		}

		private void EnsureVideoOutputConfigured()
		{
			if (this.session is null)
				return;

			if (this.videoOutput is null)
			{
				this.videoOutput = new AVCaptureVideoDataOutput();
				this.videoOutput.AlwaysDiscardsLateVideoFrames = true;
				NSDictionary Settings = new NSDictionary(CVPixelBuffer.PixelFormatTypeKey, CVPixelFormatType.CV420YpCbCr8BiPlanarFullRange);
				this.videoOutput.WeakVideoSettings = Settings;

				this.sampleBufferDelegate = new CameraFrameSampleBufferDelegate(this);
				this.videoQueue = new DispatchQueue("NeuroAccessMaui.Camera.FrameQueue");
				this.videoOutput.SetSampleBufferDelegate(this.sampleBufferDelegate, this.videoQueue);
			}

			bool OutputAlreadyAdded = Array.Exists(this.session.Outputs, Output => Output == this.videoOutput);
			if (!OutputAlreadyAdded && this.session.CanAddOutput(this.videoOutput))
				this.session.AddOutput(this.videoOutput);
		}

		private void RemoveVideoOutputFromSession()
		{
			if (this.videoOutput is not null)
			{
				if (this.session is not null && Array.Exists(this.session.Outputs, Output => Output == this.videoOutput))
					this.session.RemoveOutput(this.videoOutput);

				this.videoOutput.SetSampleBufferDelegate(null, null);
				this.videoOutput.Dispose();
				this.videoOutput = null;
			}

			if (this.videoQueue is not null)
			{
				this.videoQueue.Dispose();
				this.videoQueue = null;
			}

			this.sampleBufferDelegate = null;
		}

		private bool ShouldEnableFrameOutput()
		{
			return this.VirtualView?.HasFrameSubscribers ?? false;
		}

		private void LogFrameDrop(string Reason, CVPixelBuffer PixelBuffer, int Width, int Height)
		{
			int BytesPerRow = (int)PixelBuffer.BytesPerRow;
			int PlaneBytesPerRow = PixelBuffer.IsPlanar && PixelBuffer.PlaneCount > 0
				? (int)PixelBuffer.GetBytesPerRowOfPlane(0)
				: 0;
			string CameraId = this.selectedCamera?.Id ?? this.deviceInput?.Device?.UniqueID ?? "Unknown";
			string CameraPositionName = this.selectedCamera?.Position.ToString()
				?? (this.options.PreferRearCamera ? NeuroAccessMaui.Camera.CameraPosition.Rear.ToString() : NeuroAccessMaui.Camera.CameraPosition.Front.ToString());
			LogWarning(
				$"Dropped iOS preview frame. Reason={Reason}, CameraId={CameraId}, CameraPosition={CameraPositionName}, Width={Width}, Height={Height}, PixelFormat={PixelBuffer.PixelFormatType}, BytesPerRow={BytesPerRow}, PlaneBytesPerRow={PlaneBytesPerRow}, PlaneCount={PixelBuffer.PlaneCount}");
		}

		private void RegisterSessionObservers()
		{
			if (this.session is null)
				return;

			if (this.sessionWasInterruptedObserver is null)
			{
				this.sessionWasInterruptedObserver = NSNotificationCenter.DefaultCenter.AddObserver(
					CaptureSessionWasInterruptedNotification,
					this.HandleCaptureSessionWasInterrupted,
					this.session);
			}

			if (this.sessionInterruptionEndedObserver is null)
			{
				this.sessionInterruptionEndedObserver = NSNotificationCenter.DefaultCenter.AddObserver(
					CaptureSessionInterruptionEndedNotification,
					this.HandleCaptureSessionInterruptionEnded,
					this.session);
			}

			if (this.sessionRuntimeErrorObserver is null)
			{
				this.sessionRuntimeErrorObserver = NSNotificationCenter.DefaultCenter.AddObserver(
					CaptureSessionRuntimeErrorNotification,
					this.HandleCaptureSessionRuntimeError,
					this.session);
			}
		}

		private void UnregisterSessionObservers()
		{
			RemoveObserver(ref this.sessionWasInterruptedObserver);
			RemoveObserver(ref this.sessionInterruptionEndedObserver);
			RemoveObserver(ref this.sessionRuntimeErrorObserver);
		}

		private static void RemoveObserver(ref NSObject? Observer)
		{
			if (Observer is null)
				return;

			NSNotificationCenter.DefaultCenter.RemoveObserver(Observer);
			Observer.Dispose();
			Observer = null;
		}

		private void HandleCaptureSessionWasInterrupted(NSNotification Notification)
		{
			this.isSessionInterrupted = true;
			string Reason = GetInterruptionReasonText(Notification);
			LogWarning($"iOS capture session interrupted. Reason={Reason}");
			this.UpdatePreviewRunningState(false);
		}

		private void HandleCaptureSessionInterruptionEnded(NSNotification Notification)
		{
			_ = Notification;
			this.isSessionInterrupted = false;
			LogDebug("iOS capture session interruption ended.");
			_ = this.TryRecoverPreviewAsync("InterruptionEnded");
		}

		private void HandleCaptureSessionRuntimeError(NSNotification Notification)
		{
			NSError? RuntimeError = TryGetRuntimeError(Notification);
			string ErrorDescription = RuntimeError?.LocalizedDescription ?? "Unknown";
			LogWarning($"iOS capture session runtime error. Error={ErrorDescription}");
			this.UpdatePreviewRunningState(false);
			_ = this.TryRecoverPreviewAsync("RuntimeError");
		}

		private async Task TryRecoverPreviewAsync(string Reason)
		{
			if (!this.shouldMaintainPreview || this.isDisposed)
			{
				LogDebug($"Skipped iOS preview recovery. Reason={Reason}, MaintainPreview={this.shouldMaintainPreview}, IsDisposed={this.isDisposed}");
				return;
			}

			if (Interlocked.CompareExchange(ref this.recoveryInProgress, 1, 0) != 0)
			{
				LogDebug($"Skipped iOS preview recovery. Reason={Reason}, RecoveryAlreadyInProgress=True");
				return;
			}

			try
			{
				LogDebug($"Attempting iOS preview recovery. Reason={Reason}");
				await Task.Delay(TimeSpan.FromMilliseconds(150)).ConfigureAwait(false);
				if (!this.shouldMaintainPreview || this.isDisposed)
				{
					LogDebug($"Skipped iOS preview recovery after delay. Reason={Reason}, MaintainPreview={this.shouldMaintainPreview}, IsDisposed={this.isDisposed}");
					return;
				}

				await this.StartPreviewCoreAsync(CancellationToken.None, false).ConfigureAwait(false);
				bool IsRunning = this.VirtualView?.IsPreviewRunning ?? false;
				if (IsRunning)
					LogDebug($"iOS preview recovery succeeded. Reason={Reason}");
				else
					LogWarning($"iOS preview recovery completed but preview is not running. Reason={Reason}");
			}
			catch (Exception Exception)
			{
				LogWarning($"iOS preview recovery failed. Reason={Reason}, Error={Exception.Message}");
			}
			finally
			{
				Interlocked.Exchange(ref this.recoveryInProgress, 0);
			}
		}

		private static string GetInterruptionReasonText(NSNotification Notification)
		{
			if (Notification.UserInfo is null)
				return "Unknown";

			NSObject? ReasonObject = Notification.UserInfo[CaptureSessionInterruptionReasonKey];
			if (ReasonObject is null)
				return "Unknown";

			if (ReasonObject is NSNumber ReasonValue)
			{
				Type EnumType = typeof(AVCaptureSessionInterruptionReason);
				Type UnderlyingType = Enum.GetUnderlyingType(EnumType);
				object? NumericReason = TryConvertNSNumberToUnderlyingValue(ReasonValue, UnderlyingType);

				if (NumericReason is null)
					return ReasonObject.ToString() ?? "Unknown";

				long NumericReasonValue = Convert.ToInt64(NumericReason, CultureInfo.InvariantCulture);
				if (Enum.IsDefined(EnumType, NumericReason))
				{
					AVCaptureSessionInterruptionReason InterruptionReason =
						(AVCaptureSessionInterruptionReason)Enum.ToObject(EnumType, NumericReason);
					return $"{InterruptionReason}({NumericReasonValue})";
				}

				return $"Code({NumericReasonValue})";
			}

			return ReasonObject.ToString() ?? "Unknown";
		}

		private static object? TryConvertNSNumberToUnderlyingValue(NSNumber Number, Type UnderlyingType)
		{
			if (UnderlyingType == typeof(long))
				return Number.Int64Value;

			if (UnderlyingType == typeof(ulong))
				return Number.UInt64Value;

			if (UnderlyingType == typeof(int) ||
				UnderlyingType == typeof(short) ||
				UnderlyingType == typeof(sbyte))
			{
				return Convert.ChangeType(Number.Int64Value, UnderlyingType, CultureInfo.InvariantCulture);
			}

			if (UnderlyingType == typeof(uint) ||
				UnderlyingType == typeof(ushort) ||
				UnderlyingType == typeof(byte))
			{
				return Convert.ChangeType(Number.UInt64Value, UnderlyingType, CultureInfo.InvariantCulture);
			}

			return null;
		}

		private static NSError? TryGetRuntimeError(NSNotification Notification)
		{
			if (Notification.UserInfo is null)
				return null;

			NSObject? ErrorObject = Notification.UserInfo[CaptureSessionErrorKey];
			if (ErrorObject is null)
				return null;

			return ErrorObject as NSError;
		}

		private async Task StartPreviewCoreAsync(CancellationToken CancellationToken, bool IsExplicitStartRequest)
		{
			if (IsExplicitStartRequest)
				this.shouldMaintainPreview = true;
			if (this.VirtualView is null || this.PlatformView is null)
				return;

			try
			{
				await this.sessionLifecycleLock.WaitAsync(CancellationToken).ConfigureAwait(false);
				try
				{
					CancellationToken.ThrowIfCancellationRequested();

					if (!this.shouldMaintainPreview)
					{
						this.UpdatePreviewRunningState(false);
						LogDebug("Skipped iOS preview start because preview maintenance is disabled.");
						return;
					}

					if (this.VirtualView is null || this.PlatformView is null)
						return;

					this.options = this.VirtualView.Options ?? new CameraOptions();

					if (this.session?.Running == true)
					{
						this.session.StopRunning();
						LogDebug("Stopped existing iOS capture session before preview reconfiguration.");
					}

					bool IsConfigured = this.ConfigureSession();
					if (!IsConfigured)
					{
						this.UpdatePreviewRunningState(false);
						return;
					}

					AVCaptureSession? Session = this.session;
					if (Session is null)
					{
						LogWarning("Unable to start preview: capture session is null after configuration.");
						this.UpdatePreviewRunningState(false);
						return;
					}

					AVLayerVideoGravity VideoGravity = ResolveVideoGravity(this.options.PreviewScaling);
					await this.EnsurePreviewLayerAttachedAsync(Session, VideoGravity).ConfigureAwait(false);

					this.lastFrameTimestamp = DateTimeOffset.MinValue;
					await Task.Run(() =>
					{
						CancellationToken.ThrowIfCancellationRequested();
						Session.StartRunning();
					}, CancellationToken).ConfigureAwait(false);

					this.isSessionInterrupted = false;
					await this.SetPreviewRunningStateAsync(true).ConfigureAwait(false);
					string SessionPreset = Session.SessionPreset?.ToString() ?? string.Empty;
					string CameraId = this.selectedCamera?.Id ?? this.deviceInput?.Device?.UniqueID ?? string.Empty;
					string CameraPositionName = this.selectedCamera?.Position.ToString() ?? NeuroAccessMaui.Camera.CameraPosition.Unknown.ToString();
					LogDebug($"iOS preview started. CameraId={CameraId}, CameraPosition={CameraPositionName}, SessionPreset={SessionPreset}, FrameOutputEnabled={this.videoOutput is not null}, Interrupted={this.isSessionInterrupted}");
				}
				finally
				{
					this.sessionLifecycleLock.Release();
				}
			}
			catch (OperationCanceledException)
			{
				this.UpdatePreviewRunningState(false);
				LogDebug("iOS preview start canceled.");
			}
			catch (Exception Exception)
			{
				LogWarning($"Failed to start iOS preview session: {Exception.Message}");
				this.UpdatePreviewRunningState(false);
			}
		}

		private static bool TryExtractLuminance(CVPixelBuffer PixelBuffer, int Width, int Height, out byte[]? Luminance, out string FailureReason)
		{
			Luminance = null;
			FailureReason = string.Empty;
			if (Width <= 0 || Height <= 0)
			{
				FailureReason = "InvalidDimensions";
				return false;
			}

			CVPixelFormatType Format = PixelBuffer.PixelFormatType;
			if (Format == CVPixelFormatType.OneComponent8)
			{
				return TryReadPackedLuminance(PixelBuffer.BaseAddress, (int)PixelBuffer.BytesPerRow, Width, Height, Width, out Luminance, out FailureReason);
			}

			if (Format == CVPixelFormatType.CV420YpCbCr8BiPlanarFullRange
				|| Format == CVPixelFormatType.CV420YpCbCr8BiPlanarVideoRange)
			{
				return TryReadPlanarLuminance(PixelBuffer, Width, Height, out Luminance, out FailureReason);
			}

			if (Format == CVPixelFormatType.CV32BGRA)
			{
				return TryReadBgraLuminance(PixelBuffer.BaseAddress, (int)PixelBuffer.BytesPerRow, Width, Height, out Luminance, out FailureReason);
			}

			FailureReason = $"UnsupportedPixelFormat:{Format}";
			return false;
		}

		private static bool TryReadPackedLuminance(IntPtr BaseAddress, int BytesPerRow, int Width, int Height, int MinimumRowBytes, out byte[]? Luminance, out string FailureReason)
		{
			Luminance = null;
			FailureReason = string.Empty;
			if (BaseAddress == IntPtr.Zero)
			{
				FailureReason = "MissingBaseAddress";
				return false;
			}
			if (BytesPerRow < MinimumRowBytes)
			{
				FailureReason = "InsufficientBytesPerRow";
				return false;
			}

			Luminance = new byte[Width * Height];
			for (int Row = 0; Row < Height; Row++)
			{
				IntPtr RowPtr = IntPtr.Add(BaseAddress, Row * BytesPerRow);
				System.Runtime.InteropServices.Marshal.Copy(RowPtr, Luminance, Row * Width, Width);
			}

			return true;
		}

		private static bool TryReadPlanarLuminance(CVPixelBuffer PixelBuffer, int Width, int Height, out byte[]? Luminance, out string FailureReason)
		{
			Luminance = null;
			FailureReason = string.Empty;
			if (!PixelBuffer.IsPlanar || PixelBuffer.PlaneCount < 1)
			{
				FailureReason = "MissingLumaPlane";
				return false;
			}

			IntPtr BaseAddress = PixelBuffer.GetBaseAddress(0);
			int BytesPerRow = (int)PixelBuffer.GetBytesPerRowOfPlane(0);
			if (BaseAddress == IntPtr.Zero || BytesPerRow <= 0)
			{
				FailureReason = "InvalidPlanarMetadata";
				return false;
			}

			return TryReadPackedLuminance(BaseAddress, BytesPerRow, Width, Height, Width, out Luminance, out FailureReason);
		}

		private static bool TryReadBgraLuminance(IntPtr BaseAddress, int BytesPerRow, int Width, int Height, out byte[]? Luminance, out string FailureReason)
		{
			Luminance = null;
			FailureReason = string.Empty;
			long MinimumRowBytes = (long)Width * 4L;
			if (BaseAddress == IntPtr.Zero)
			{
				FailureReason = "MissingBaseAddress";
				return false;
			}
			if (BytesPerRow < MinimumRowBytes)
			{
				FailureReason = "InsufficientBgraBytesPerRow";
				return false;
			}

			Luminance = new byte[Width * Height];
			byte[] RowBuffer = new byte[BytesPerRow];

			for (int Row = 0; Row < Height; Row++)
			{
				IntPtr RowPtr = IntPtr.Add(BaseAddress, Row * BytesPerRow);
				System.Runtime.InteropServices.Marshal.Copy(RowPtr, RowBuffer, 0, BytesPerRow);

				int RowOffset = Row * Width;
				for (int Column = 0; Column < Width; Column++)
				{
					int PixelOffset = Column * 4;
					int Blue = RowBuffer[PixelOffset];
					int Green = RowBuffer[PixelOffset + 1];
					int Red = RowBuffer[PixelOffset + 2];
					int Luma = (Red * 77 + Green * 150 + Blue * 29) >> 8;
					Luminance[RowOffset + Column] = (byte)Luma;
				}
			}

			return true;
		}

		private static void LogWarning(string Message)
		{
			Debug.WriteLine($"[CameraViewHandler] {Message}");
		}

		private static void LogDebug(string Message)
		{
			Debug.WriteLine($"[CameraViewHandler] {Message}");
		}

		private static AVCapturePhotoQualityPrioritization ResolveEffectivePhotoQualityPrioritization(AVCapturePhotoQualityPrioritization MaxPrioritization)
		{
			return MaxPrioritization switch
			{
				AVCapturePhotoQualityPrioritization.Speed => AVCapturePhotoQualityPrioritization.Speed,
				AVCapturePhotoQualityPrioritization.Balanced => AVCapturePhotoQualityPrioritization.Balanced,
				AVCapturePhotoQualityPrioritization.Quality => AVCapturePhotoQualityPrioritization.Quality,
				_ => AVCapturePhotoQualityPrioritization.Balanced
			};
		}

		private static bool IsPhotoQualityPrioritizationException(ObjCRuntime.ObjCException Exception)
		{
			if (Exception is null || string.IsNullOrWhiteSpace(Exception.Message))
				return false;

			return Exception.Message.IndexOf("photoQualityPrioritization", StringComparison.OrdinalIgnoreCase) >= 0;
		}

		private static AVLayerVideoGravity ResolveVideoGravity(CameraPreviewScaling PreviewScaling)
		{
			return PreviewScaling switch
			{
				CameraPreviewScaling.Fit => AVLayerVideoGravity.ResizeAspect,
				_ => AVLayerVideoGravity.ResizeAspectFill
			};
		}

		private int ResolveFrameRotationDegrees()
		{
			return 0;
		}

		private void ApplyCurrentVideoOrientationToConnections(CameraPreviewContainerView ContainerView)
		{
			AVCaptureVideoOrientation Orientation = ResolveCurrentVideoOrientation();

			ApplyVideoOrientation(ContainerView.PreviewLayer?.Connection, Orientation);
			AVCaptureConnection? VideoOutputConnection = this.videoOutput?.ConnectionFromMediaType(AVMediaTypes.Video.GetConstant());
			ApplyVideoOrientation(VideoOutputConnection, Orientation);
		}

		private static void ApplyVideoOrientation(AVCaptureConnection? Connection, AVCaptureVideoOrientation Orientation)
		{
			if (Connection is null || !Connection.SupportsVideoOrientation)
			{
				return;
			}

			Connection.VideoOrientation = Orientation;
		}

		private static AVCaptureVideoOrientation ResolveCurrentVideoOrientation()
		{
			UIInterfaceOrientation InterfaceOrientation = ResolveCurrentInterfaceOrientation();
			return InterfaceOrientation switch
			{
				UIInterfaceOrientation.Portrait => AVCaptureVideoOrientation.Portrait,
				UIInterfaceOrientation.PortraitUpsideDown => AVCaptureVideoOrientation.PortraitUpsideDown,
				UIInterfaceOrientation.LandscapeLeft => AVCaptureVideoOrientation.LandscapeLeft,
				UIInterfaceOrientation.LandscapeRight => AVCaptureVideoOrientation.LandscapeRight,
				_ => ResolveDeviceVideoOrientation()
			};
		}

		private static UIInterfaceOrientation ResolveCurrentInterfaceOrientation()
		{
			UIWindowScene? ForegroundScene = UIApplication.SharedApplication.ConnectedScenes
				.OfType<UIWindowScene>()
				.FirstOrDefault(static Scene => Scene.ActivationState == UISceneActivationState.ForegroundActive);

			return ForegroundScene?.InterfaceOrientation ?? UIInterfaceOrientation.Unknown;
		}

		private static AVCaptureVideoOrientation ResolveDeviceVideoOrientation()
		{
			return UIDevice.CurrentDevice.Orientation switch
			{
				UIDeviceOrientation.Portrait => AVCaptureVideoOrientation.Portrait,
				UIDeviceOrientation.PortraitUpsideDown => AVCaptureVideoOrientation.PortraitUpsideDown,
				UIDeviceOrientation.LandscapeLeft => AVCaptureVideoOrientation.LandscapeRight,
				UIDeviceOrientation.LandscapeRight => AVCaptureVideoOrientation.LandscapeLeft,
				_ => AVCaptureVideoOrientation.Portrait
			};
		}

		private sealed class CameraFrameSampleBufferDelegate : AVCaptureVideoDataOutputSampleBufferDelegate
		{
			private readonly CameraViewHandler handler;

			public CameraFrameSampleBufferDelegate(CameraViewHandler Handler)
			{
				this.handler = Handler;
			}

			public override void DidOutputSampleBuffer(AVCaptureOutput Output, CMSampleBuffer SampleBuffer, AVCaptureConnection Connection)
			{
				try
				{
					int RotationDegrees = this.handler.ResolveFrameRotationDegrees();
					this.handler.HandleSampleBuffer(SampleBuffer, RotationDegrees);
				}
				catch (Exception Exception)
				{
					LogWarning($"Unhandled iOS frame analysis error. Message={Exception.Message}");
				}
			}
		}

		private sealed class PhotoCaptureDelegate : AVCapturePhotoCaptureDelegate
		{
			private readonly TaskCompletionSource<byte[]?> completionSource;
			private readonly Action onCompleted;
			private readonly object syncRoot = new object();
			private byte[]? capturedBytes;
			private Exception? capturedException;
			private NSError? finalError;
			private bool hasProcessingResult;
			private bool hasFinalResult;
			private bool isCompleted;

			public PhotoCaptureDelegate(TaskCompletionSource<byte[]?> CompletionSource, Action OnCompleted)
			{
				this.completionSource = CompletionSource;
				this.onCompleted = OnCompleted;
			}

			public void Cancel()
			{
				bool ShouldNotifyCompletion = false;
				lock (this.syncRoot)
				{
					if (this.isCompleted)
						return;

					this.isCompleted = true;
					this.capturedBytes = null;
					this.capturedException = null;
					this.finalError = null;
					ShouldNotifyCompletion = true;
				}

				this.completionSource.TrySetResult(null);
				if (ShouldNotifyCompletion)
					this.onCompleted();
			}

			public override void DidFinishProcessingPhoto(AVCapturePhotoOutput Output, AVCapturePhoto Photo, NSError? Error)
			{
				bool ShouldNotifyCompletion = false;
				try
				{
					Exception? LocalCapturedException = null;
					byte[]? LocalCapturedBytes = null;

					if (Error is not null)
					{
						LocalCapturedException = new InvalidOperationException(Error.LocalizedDescription);
					}
					else
					{
						using NSData? Data = Photo?.FileDataRepresentation;
						if (Data is not null)
						{
							LocalCapturedBytes = new byte[Data.Length];
							System.Runtime.InteropServices.Marshal.Copy(Data.Bytes, LocalCapturedBytes, 0, (int)Data.Length);
						}
					}

					lock (this.syncRoot)
					{
						if (this.isCompleted || this.hasProcessingResult)
							return;

						this.capturedBytes = LocalCapturedBytes;
						this.capturedException = LocalCapturedException;
						this.hasProcessingResult = true;
						ShouldNotifyCompletion = this.TryCompleteLocked();
					}
				}
				catch (Exception Ex)
				{
					lock (this.syncRoot)
					{
						if (this.isCompleted || this.hasProcessingResult)
							return;

						this.capturedException = Ex;
						this.hasProcessingResult = true;
						ShouldNotifyCompletion = this.TryCompleteLocked();
					}
				}

				if (ShouldNotifyCompletion)
					this.onCompleted();
			}

			public override void DidFinishCapture(AVCapturePhotoOutput Output, AVCaptureResolvedPhotoSettings ResolvedSettings, NSError? Error)
			{
				bool ShouldNotifyCompletion;
				lock (this.syncRoot)
				{
					if (this.isCompleted)
						return;

					this.finalError = Error;
					this.hasFinalResult = true;
					ShouldNotifyCompletion = this.TryCompleteLocked();
				}

				if (ShouldNotifyCompletion)
					this.onCompleted();
			}

			private bool TryCompleteLocked()
			{
				if (this.isCompleted || !this.hasFinalResult)
					return false;

				if (this.finalError is null && !this.hasProcessingResult)
					return false;

				this.isCompleted = true;

				if (this.finalError is not null)
				{
					this.completionSource.TrySetException(new InvalidOperationException(this.finalError.LocalizedDescription));
				}
				else if (this.capturedException is not null)
				{
					this.completionSource.TrySetException(this.capturedException);
				}
				else
				{
					this.completionSource.TrySetResult(this.capturedBytes);
				}

				this.capturedBytes = null;
				this.capturedException = null;
				this.finalError = null;
				return true;
			}
		}

		private sealed class CameraPreviewContainerView : UIView
		{
			public AVCaptureVideoPreviewLayer? PreviewLayer { get; set; }

			public override void LayoutSubviews()
			{
				base.LayoutSubviews();
				if (this.PreviewLayer is not null)
					this.PreviewLayer.Frame = this.Bounds;
			}
		}
	}
}
#endif
