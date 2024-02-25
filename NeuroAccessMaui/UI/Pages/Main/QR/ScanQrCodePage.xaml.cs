using CommunityToolkit.Maui.Layouts;
using CommunityToolkit.Mvvm.Input;
using CommunityToolkit.Mvvm.Messaging;
using Microsoft.Maui.Controls.PlatformConfiguration;
using Microsoft.Maui.Controls.PlatformConfiguration.iOSSpecific;
using NeuroAccessMaui.Services;
using ZXing.Net.Maui;
using ZXing.Net.Maui.Readers;

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
		/// <param name="NavigationArgs">
		/// Navigation arguments, which are manually passed to the constructor when Shell navigation is not available, namely during on-boarding.
		/// </param>
		public ScanQrCodePage(ScanQrCodeNavigationArgs? NavigationArgs)
		{
			this.InitializeComponent();

			ScanQrCodeViewModel ViewModel = new(NavigationArgs);
			this.ContentPageModel = ViewModel;

			this.LinkEntry.Entry.Keyboard = Keyboard.Url;
			this.LinkEntry.Entry.IsSpellCheckEnabled = false;
			this.LinkEntry.Entry.IsTextPredictionEnabled = false;

			StateContainer.SetCurrentState(this.GridWithAnimation, "AutomaticScan");

			this.CameraBarcodeReaderView.Options = new BarcodeReaderOptions
			{
				Formats = BarcodeFormats.TwoDimensional,
				AutoRotate = true,
				TryHarder = true,
				TryInverted = true,
				Multiple = false
			};

			ViewModel.DoSwitchMode(true);
		}

		/// <summary>
		/// Creates a new instance of the <see cref="ScanQrCodePage"/> class.
		/// </summary>
		/// <remarks>
		/// A parameterless constructor is required for shell routing system (it uses <c>Activator.CreateInstance</c>).
		/// </remarks>
		public ScanQrCodePage()
			: this(ServiceRef.UiService.PopLatestArgs<ScanQrCodeNavigationArgs>())
		{
		}

		/// <inheritdoc/>
		protected override async Task OnAppearingAsync()
		{
			await base.OnAppearingAsync();

			this.CameraBarcodeReaderView.IsDetecting = true;
			WeakReferenceMessenger.Default.Register<KeyboardSizeMessage>(this, this.HandleKeyboardSizeMessage);

		}

		/// <inheritdoc/>
		protected override async Task OnDisappearingAsync()
		{
			this.CameraBarcodeReaderView.IsDetecting = false;
			WeakReferenceMessenger.Default.Unregister<KeyboardSizeMessage>(this);

			await base.OnDisappearingAsync();
		}

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
		private static async Task PickPhoto()
		{
			FileResult Result = await MediaPicker.PickPhotoAsync();

			if (Result is null)
				return;

			byte[] Bin = File.ReadAllBytes(Result.FullPath);

			PixelBufferHolder Data = new()
			{
				//!!! System dependent code. Not implemented yet
#if ANDROID
#else
#endif
			};

			if (ServiceRef.BarcodeReader is not ZXingBarcodeReader BarcodeReader)
				return;

			BarcodeReader.Decode(Data);
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
