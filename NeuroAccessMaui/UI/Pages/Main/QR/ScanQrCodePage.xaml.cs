using CommunityToolkit.Maui.Layouts;
using CommunityToolkit.Mvvm.Input;
using ZXing.Net.Maui;

namespace NeuroAccessMaui.UI.Pages.Main.QR;

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
	public ScanQrCodePage() : this(null)
	{
	}

	/// <summary>
	/// Asynchronous OnAppearing-method.
	/// </summary>
	protected override async Task OnAppearingAsync()
	{
		await base.OnAppearingAsync();

		this.CameraBarcodeReaderView.IsDetecting = true;
	}

	/// <summary>
	/// Asynchronous OnAppearing-method.
	/// </summary>
	protected override async Task OnDisappearingAsync()
	{
		this.CameraBarcodeReaderView.IsDetecting = false;

		await base.OnDisappearingAsync();
	}

	[RelayCommand]
	private async Task SwitchMode()
	{
		await this.Dispatcher.DispatchAsync(async () =>
		{
			string CurrentState = StateContainer.GetCurrentState(this.GridWithAnimation);
			bool IsAutomaticScan = string.Equals(CurrentState, "AutomaticScan", StringComparison.OrdinalIgnoreCase);

			if (!IsAutomaticScan)
			{
				this.LinkEntry.Unfocus();
			}
			else
			{
				this.CameraBarcodeReaderView.IsTorchOn = false;
				this.CameraBarcodeReaderView.IsDetecting = false;
			}

			await StateContainer.ChangeStateWithAnimation(this.GridWithAnimation,
				IsAutomaticScan ? "ManualScan" : "AutomaticScan", CancellationToken.None);

			ScanQrCodeViewModel ViewModel = this.ViewModel<ScanQrCodeViewModel>();
			await ViewModel.DoSwitchMode(IsAutomaticScan);

			if (IsAutomaticScan)
			{
				this.LinkEntry.Focus();
			}
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
		});
	}

	[RelayCommand]
	private void SwitchCamera()
	{
		this.CameraBarcodeReaderView.IsTorchOn = false;

		if (this.CameraBarcodeReaderView.CameraLocation == CameraLocation.Rear)
		{
			this.CameraBarcodeReaderView.CameraLocation = CameraLocation.Front;
		}
		else
		{
			this.CameraBarcodeReaderView.CameraLocation = CameraLocation.Rear;
		}
	}

	[RelayCommand]
	private void SwitchTorch()
	{
		if (this.CameraBarcodeReaderView.IsTorchOn)
		{
			this.CameraBarcodeReaderView.IsTorchOn = false;
		}
		else
		{
			this.CameraBarcodeReaderView.IsTorchOn = true;
		}
	}

	private async void CameraBarcodeReaderView_BarcodesDetected(object sender, BarcodeDetectionEventArgs e)
	{
		string Result = e.Results[0].Value;

		if (!string.IsNullOrWhiteSpace(Result))
		{
			await this.Dispatcher.DispatchAsync(() =>
			{
				ScanQrCodeViewModel ViewModel = this.ViewModel<ScanQrCodeViewModel>();
				ViewModel.QrText = Result.Trim();
			});
		}
	}
}
