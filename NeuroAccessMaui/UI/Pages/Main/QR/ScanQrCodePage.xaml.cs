
using CommunityToolkit.Maui.Layouts;
using CommunityToolkit.Mvvm.Input;
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
		/// <summary>
		/// Creates a new instance of the <see cref="ScanQrCodePage"/> class.
		/// </summary>
		public ScanQrCodePage()
		{
			this.InitializeComponent();

			ScanQrCodeNavigationArgs? args = ServiceRef.UiService.PopLatestArgs<ScanQrCodeNavigationArgs>();
			ScanQrCodeViewModel ViewModel = new(args);
			this.ContentPageModel = ViewModel;

			this.navigationComplete = args?.NavigationCompletionSource;

			this.LinkEntry.Entry.Keyboard = Keyboard.Url;
			this.LinkEntry.Entry.IsSpellCheckEnabled = false;
			this.LinkEntry.Entry.IsTextPredictionEnabled = false;

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

			ViewModel.DoSwitchMode(true);
		}


		/// <inheritdoc/>
		protected override async Task OnAppearingAsync()
		{
			await base.OnAppearingAsync();

			if (this.navigationComplete is not null)
				await this.navigationComplete.Task;

			this.CameraBarcodeReaderView.IsDetecting = true;
			WeakReferenceMessenger.Default.Register<KeyboardSizeMessage>(this, this.HandleKeyboardSizeMessage);
		}

		/// <inheritdoc/>
		protected override async Task OnDisappearingAsync()
		{
			await MainThread.InvokeOnMainThreadAsync(this.CloseCamera);


			WeakReferenceMessenger.Default.Unregister<KeyboardSizeMessage>(this);

			await base.OnDisappearingAsync();
		}

		/// <summary>
		/// As the camera is not closed properly by the 3rd party framework, we need to close it manually using reflection.
		/// This is a problem on certain android phones where facial recognition is used.
		/// https://github.com/Redth/ZXing.Net.Maui/issues/38
		/// </summary>
		/// TODO: Check if the camera is closed properly by the 3rd party framework and remove this method if it is.
		private async Task CloseCamera()
		{
			try
			{
				this.CameraBarcodeReaderView.IsDetecting = false;
#if ANDROID
				ICameraInternal? preview = await this.GetCameraPreview(this.CameraBarcodeReaderView);
				preview?.Close();
#endif
			}
			catch (Exception e)
			{
				ServiceRef.LogService.LogException(e);
			}
		}
#if ANDROID
		/// <summary>
		/// Android specific method to get the camera preview from the 3rd party framework using reflection.
		/// https://github.com/Redth/ZXing.Net.Maui/issues/164   
		/// </summary>
		/// <param name="cameraBarcodeReaderView">The camera view in use</param>
		/// <returns>The Camera preview</returns>
		private async Task<ICameraInternal?> GetCameraPreview(CameraBarcodeReaderView cameraBarcodeReaderView)
		{

			PermissionStatus status = await Permissions.CheckStatusAsync<Permissions.Camera>();
			if (status == PermissionStatus.Denied)
				return null;

			BindingFlags bindingFlags = BindingFlags.NonPublic | BindingFlags.Instance;
			PropertyInfo? strongHandlerProperty = typeof(CameraBarcodeReaderView).GetProperty("StrongHandler", bindingFlags);
			CameraBarcodeReaderViewHandler? cameraBarcodeReaderViewHandler = strongHandlerProperty?.GetValue(cameraBarcodeReaderView) as CameraBarcodeReaderViewHandler;
			FieldInfo? manageField = typeof(CameraBarcodeReaderViewHandler).GetField("cameraManager", bindingFlags);
			object? cameraManager = manageField?.GetValue(cameraBarcodeReaderViewHandler);
			FieldInfo? cameraPreviewField = typeof(CameraBarcodeReaderViewHandler).Assembly.GetType("ZXing.Net.Maui.CameraManager")?.GetField("cameraPreview", bindingFlags);
			Preview? preview = cameraPreviewField?.GetValue(cameraManager) as Preview;

			return preview?.Camera;
		}
#endif


		private TaskCompletionSource<bool>? navigationComplete;

		private async void HandleKeyboardSizeMessage(object Recipient, KeyboardSizeMessage Message)
		{
			await this.Dispatcher.DispatchAsync(() =>
			{
				double Bottom = 0;
				if (DeviceInfo.Platform == DevicePlatform.iOS)
				{
					Thickness SafeInsets = this.On<iOS>().SafeAreaInsets();
					Bottom = SafeInsets.Bottom;
				}

				Thickness Margin = new(0, 0, 0, Message.KeyboardSize - Bottom);
				this.ManualScanGrid.Margin = Margin;
			});
		}

		[RelayCommand]
		private async Task SwitchMode()
		{
			await this.Dispatcher.DispatchAsync(async () =>
			{
				try
				{
					string CurrentState = StateContainer.GetCurrentState(this.GridWithAnimation);
					bool IsAutomaticScan = string.Equals(CurrentState, "AutomaticScan", StringComparison.OrdinalIgnoreCase);

					if (!IsAutomaticScan)
						this.LinkEntry.Entry.Unfocus();
					else
					{
						this.CameraBarcodeReaderView.IsTorchOn = false;
						this.CameraBarcodeReaderView.IsDetecting = false;
					}

					DateTime Start = DateTime.Now;

					while (!StateContainer.GetCanStateChange(this.GridWithAnimation) && DateTime.Now.Subtract(Start).TotalSeconds < 2)
						await Task.Delay(100);

					await StateContainer.ChangeStateWithAnimation(this.GridWithAnimation,
						IsAutomaticScan ? "ManualScan" : "AutomaticScan", CancellationToken.None);

					ScanQrCodeViewModel ViewModel = this.ViewModel<ScanQrCodeViewModel>();
					await ViewModel.DoSwitchMode(IsAutomaticScan);

					if (IsAutomaticScan)
						this.LinkEntry.Entry.Focus();
					else
					{
						// reinitialize the camera by switching it
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

						this.CameraBarcodeReaderView.IsDetecting = true;
					}
				}
				catch (Exception ex)
				{
					ServiceRef.LogService.LogException(ex);
				}
			});
		}

		[RelayCommand]
		private void SwitchCamera()
		{
			this.CameraBarcodeReaderView.IsTorchOn = false;

			if (this.CameraBarcodeReaderView.CameraLocation == CameraLocation.Rear)
				this.CameraBarcodeReaderView.CameraLocation = CameraLocation.Front;
			else
				this.CameraBarcodeReaderView.CameraLocation = CameraLocation.Rear;
		}

		[RelayCommand]
		private void SwitchTorch()
		{
			if (this.CameraBarcodeReaderView.IsTorchOn)
				this.CameraBarcodeReaderView.IsTorchOn = false;
			else
				this.CameraBarcodeReaderView.IsTorchOn = true;
		}

		[RelayCommand]
		private async Task PickPhoto()
		{
			FileResult? result = await MediaPicker.PickPhotoAsync();
			if (result is null)
				return;

#if ANDROID
			// For Android, convert the picked image to a Bitmap and then to pixel data
			Console.WriteLine($"Picked photo {result.FullPath}");
			await using Stream stream = await result.OpenReadAsync();
			Bitmap? bitmap = await BitmapFactory.DecodeStreamAsync(stream);
			Console.WriteLine($"Loaded photo {result.FullPath}");

			if (bitmap != null)
			{
				int width = bitmap.Width;
				int height = bitmap.Height;
				int[] pixels = new int[width * height];
				bitmap.GetPixels(pixels, 0, width, 0, 0, width, height);

				// Convert pixel data to ByteBuffer
				var byteBuffer = ByteBuffer.Allocate(pixels.Length * sizeof(int));
				Console.WriteLine($"{pixels.Length * sizeof(int)} : buffer: {byteBuffer.Capacity()}");
				byteBuffer.AsIntBuffer().Put(pixels);
				byteBuffer.Flip();

				// Initialize PixelBufferHolder
				PixelBufferHolder data = new PixelBufferHolder
				{
					Size = new Size(width, height),
					Data = byteBuffer
				};

				ZXingBarcodeReader reader = new ZXingBarcodeReader();
				BarcodeReaderOptions options = new BarcodeReaderOptions()
				{
					AutoRotate = true,
					TryHarder = true,
					TryInverted = true,
					Formats = BarcodeFormat.QrCode,
					Multiple = false
				};
				reader.Options = options;

				BarcodeResult[]? scanned = reader.Decode(data);
				if (scanned != null)
				{
					foreach (BarcodeResult item in scanned)
					{
						Console.WriteLine($"Barcode found: {item.Value}");
					}
				}
				else
				{
					Console.WriteLine("No barcode found");
				}
			}
#elif IOS
			return;
#endif
		}

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
