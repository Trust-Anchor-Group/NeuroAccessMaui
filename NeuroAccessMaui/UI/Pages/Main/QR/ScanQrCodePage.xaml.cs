
using CommunityToolkit.Maui.Layouts;
// using CommunityToolkit.Mvvm.Input; // Removed page-level commands; commands now reside in ViewModel
using CommunityToolkit.Mvvm.Messaging;
using Microsoft.Maui.Controls.PlatformConfiguration;
using Microsoft.Maui.Controls.PlatformConfiguration.iOSSpecific;
using NeuroAccessMaui.Services;
using ZXing.Net.Maui;
#if ANDROID
using Android.Graphics;
using AndroidX.Camera.Core;
using AndroidX.Camera.Core.Impl;
using Java.Nio;
using System.Reflection;
using ZXing.Net.Maui.Controls;
using ZXing.Net.Maui.Readers;
#endif

namespace NeuroAccessMaui.UI.Pages.Main.QR
{
	/// <summary>
	/// A page to display for scanning of a QR code, either automatically via the camera, or by entering the code manually.
	/// </summary>
	public partial class ScanQrCodePage
	{
		public ScanQrCodePage()
		{
			// Create VM (it will PopLatestArgs internally)
			ScanQrCodeViewModel vm = new();
			this.BindingContext = vm; // set before InitializeComponent for compiled bindings

			this.InitializeComponent();

			// Post-XAML control configuration
			this.LinkEntry.Keyboard = Keyboard.Url;
			this.LinkEntry.IsSpellCheckEnabled = false;
			this.LinkEntry.IsTextPredictionEnabled = false;

			StateContainer.SetCurrentState(this.GridWithAnimation, "AutomaticScan");

			this.CameraBarcodeReaderView.IsDetecting = false;
			this.CameraBarcodeReaderView.Options = new BarcodeReaderOptions
			{
				Formats = BarcodeFormats.TwoDimensional,
				AutoRotate = true,
				TryHarder = true,
				TryInverted = true,
				Multiple = false,
			};

			vm.DoSwitchMode(true);
		}


		/// <inheritdoc/>
		public override async Task OnAppearingAsync()
		{
			// Base Appearing executes ViewModel lifecycle
			await base.OnAppearingAsync();

			// Sync initial states from VM
			if (this.BindingContext is ScanQrCodeViewModel vm)
			{
				this.ApplyState(vm.IsAutomaticScan, initial: true);
				this.CameraBarcodeReaderView.IsTorchOn = vm.IsTorchOn;
				this.CameraBarcodeReaderView.CameraLocation = vm.CameraLocation;
				vm.PropertyChanged += this.VmOnPropertyChanged;
			}
		}

		/// <inheritdoc/>
		public override async Task OnDisappearingAsync()
		{
			if (this.BindingContext is ScanQrCodeViewModel vm)
				vm.PropertyChanged -= this.VmOnPropertyChanged;
			await MainThread.InvokeOnMainThreadAsync(this.CloseCamera);
			await base.OnDisappearingAsync();
		}

		private async void VmOnPropertyChanged(object? sender, System.ComponentModel.PropertyChangedEventArgs e)
		{
			if (sender is not ScanQrCodeViewModel vm)
				return;

			switch (e.PropertyName)
			{
				case nameof(ScanQrCodeViewModel.IsAutomaticScan):
					await this.Dispatcher.DispatchAsync(() => this.ApplyState(vm.IsAutomaticScan));
					break;
				case nameof(ScanQrCodeViewModel.IsTorchOn):
					this.CameraBarcodeReaderView.IsTorchOn = vm.IsTorchOn;
					break;
				case nameof(ScanQrCodeViewModel.IsDetecting):
					this.CameraBarcodeReaderView.IsDetecting = vm.IsDetecting;
					break;
				case nameof(ScanQrCodeViewModel.CameraLocation):
					this.CameraBarcodeReaderView.CameraLocation = vm.CameraLocation;
					break;
			}
		}

		private async void ApplyState(bool isAutomatic, bool initial = false)
		{
			try
			{
				string current = StateContainer.GetCurrentState(this.GridWithAnimation);
				bool currentlyAutomatic = string.Equals(current, "AutomaticScan", StringComparison.OrdinalIgnoreCase);
				if (currentlyAutomatic == isAutomatic && !initial)
					return;

				if (initial)
				{
					// Initial setup: avoid animation & camera flip hack to prevent black screen
					if (isAutomatic)
					{
						if (!currentlyAutomatic)
							StateContainer.SetCurrentState(this.GridWithAnimation, "AutomaticScan");
						this.LinkEntry.Unfocus();
						// slight delay to allow native camera initialization before enabling detection
						await Task.Delay(250);
						this.CameraBarcodeReaderView.IsDetecting = true;
					}
					else
					{
						StateContainer.SetCurrentState(this.GridWithAnimation, "ManualScan");
						this.CameraBarcodeReaderView.IsTorchOn = false;
						this.CameraBarcodeReaderView.IsDetecting = false;
						this.LinkEntry.Focus();
					}
					return;
				}

				if (!isAutomatic)
				{
					// Enter manual: stop camera
					this.CameraBarcodeReaderView.IsTorchOn = false;
					this.CameraBarcodeReaderView.IsDetecting = false;
					await StateContainer.ChangeStateWithAnimation(this.GridWithAnimation, "ManualScan", CancellationToken.None);
					this.LinkEntry.Focus();
				}
				else
				{
					this.LinkEntry.Unfocus();
					// Re-init camera by flipping (runtime toggle only)
					if (this.CameraBarcodeReaderView.CameraLocation == CameraLocation.Rear)
					{
						this.CameraBarcodeReaderView.CameraLocation = CameraLocation.Front;
						this.CameraBarcodeReaderView.CameraLocation = CameraLocation.Rear;
					}
					else
					{
						this.CameraBarcodeReaderView.CameraLocation = CameraLocation.Rear;
						this.CameraBarcodeReaderView.CameraLocation = CameraLocation.Front;
					}
					await StateContainer.ChangeStateWithAnimation(this.GridWithAnimation, "AutomaticScan", CancellationToken.None);
					this.CameraBarcodeReaderView.IsDetecting = true;
				}
			}
			catch (Exception ex)
			{
				ServiceRef.LogService.LogException(ex);
			}
		}

		/// <summary>
		/// As the camera is not closed properly by the 3rd party framework, we need to close it manually using reflection.
		/// This is a problem on certain android phones where facial recognition is used.
		/// https://github.com/Redth/ZXing.Net.Maui/issues/38
		/// </summary>
		/// TODO: Check if the camera is closed properly by the 3rd party framework and remove this method if it is.
		private
#if ANDROID
			async
#endif
			Task CloseCamera()
		{
			try
			{
				this.CameraBarcodeReaderView.IsDetecting = false;
#if ANDROID
				ICameraInternal? preview = await GetCameraPreview(this.CameraBarcodeReaderView);
				preview?.Close();
#endif
			}
			catch (Exception e)
			{
				ServiceRef.LogService.LogException(e);
			}
#if !ANDROID
			return Task.CompletedTask;
#endif
		}
#if ANDROID
		/// <summary>
		/// Android specific method to get the camera preview from the 3rd party framework using reflection.
		/// https://github.com/Redth/ZXing.Net.Maui/issues/164   
		/// </summary>
		/// <param name="cameraBarcodeReaderView">The camera view in use</param>
		/// <returns>The Camera preview</returns>
		private static async Task<ICameraInternal?> GetCameraPreview(CameraBarcodeReaderView cameraBarcodeReaderView)
		{

			PermissionStatus status = await Permissions.CheckStatusAsync<Permissions.Camera>();
			if (status == PermissionStatus.Denied)
				return null;

			BindingFlags BindingFlags = BindingFlags.NonPublic | BindingFlags.Instance;
			PropertyInfo? StrongHandlerProperty = typeof(CameraBarcodeReaderView).GetProperty("StrongHandler", BindingFlags);
			CameraBarcodeReaderViewHandler? CameraBarcodeReaderViewHandler = StrongHandlerProperty?.GetValue(cameraBarcodeReaderView) as CameraBarcodeReaderViewHandler;
			FieldInfo? ManageField = typeof(CameraBarcodeReaderViewHandler).GetField("cameraManager", BindingFlags);
			object? CameraManager = ManageField?.GetValue(CameraBarcodeReaderViewHandler);
			FieldInfo? CameraPreviewField = typeof(CameraBarcodeReaderViewHandler).Assembly.GetType("ZXing.Net.Maui.CameraManager")?.GetField("cameraPreview", BindingFlags);
			Preview? Preview = CameraPreviewField?.GetValue(CameraManager) as Preview;

			return Preview?.Camera;
		}
#endif


		private async void CameraBarcodeReaderView_BarcodesDetected(object sender, BarcodeDetectionEventArgs e)
		{
			string? Result = (e.Results.Length > 0) ? e.Results[0].Value : null;

			await this.Dispatcher.DispatchAsync(async () =>
			{
				ScanQrCodeViewModel ViewModel = this.ViewModel<ScanQrCodeViewModel>();
				await ViewModel.SetScannedText(Result);
			});
		}
	}
}
