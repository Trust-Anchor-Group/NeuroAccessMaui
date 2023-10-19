namespace NeuroAccessMaui.UI.Pages.Main.QR;

/// <summary>
/// A page to display for scanning of a QR code, either automatically via the camera, or by entering the code manually.
/// </summary>
public partial class ScanQrCodePage
{
	private volatile int scannedQrCodesCount = 0;
	private bool scannerRendered = false;

	/// <summary>
	/// Creates a new instance of the <see cref="ScanQrCodePage"/> class.
	/// </summary>
	/// <param name="NavigationArgs">
	/// Navigation arguments, which are manually passed to the constructor when Shell navigation is not available, namely during on-boarding.
	/// </param>
	public ScanQrCodePage(ScanQrCodeNavigationArgs? NavigationArgs)
	{
		//!!! this.ViewModel = new ScanQrCodeViewModel(NavigationArgs);
		this.InitializeComponent();

		//!!!
		/*
		this.Scanner.Options = new MobileBarcodeScanningOptions
		{
			PossibleFormats = new List<BarcodeFormat> { BarcodeFormat.QR_CODE },
			TryHarder = true
		};
		*/
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

		//!!!
		/*
		if (this.ViewModel is ScanQrCodeViewModel ScanQrCodeViewModel)
		{
			ScanQrCodeViewModel.ModeChanged += this.ViewModel_ModeChanged;
		}

		if (this.scannerRendered)
		{
			this.Scanner.IsScanning = true;
			this.Scanner.IsAnalyzing = true;
		}
		*/
	}

	/// <summary>
	/// Asynchronous OnAppearing-method.
	/// </summary>
	protected override async Task OnDisappearingAsync()
	{
		//!!!
		/*
		this.Scanner.IsAnalyzing = false;
		this.Scanner.IsScanning = false;

		if (this.ViewModel is ScanQrCodeViewModel ScanQrCodeViewModel)
		{
			ScanQrCodeViewModel.ModeChanged -= this.ViewModel_ModeChanged;
		}
		*/
		await base.OnDisappearingAsync();
	}

	private void ViewModel_ModeChanged(object Sender, EventArgs e)
	{
		//!!!
		/*
		if (this.ViewModel is ScanQrCodeViewModel ScanQrCodeViewModel && ScanQrCodeViewModel.ScanIsManual)
		{
			this.LinkEntry.Focus();
		}
		*/
	}

	//!!!
	/*
	private void Scanner_OnScanResult(Result result)
	{
		if (!string.IsNullOrWhiteSpace(result.Text))
		{
			// We want to stop analysis after the first recognized frame until we navigate away. However, on modern quick devices
			// frames are processed so quickly that a second result might arrive before we had enough time to stop analysis here.
			// P.S.
			// Do not use this.Scanner.IsAnalyzing property for synchronization, use your own field instead. Locking will not help
			// because after setting this.Scanner.IsAnalyzing to false, the value of this.Scanner.IsAnalyzing will still be true
			// for some time, the value is not updated quickly enough (don't know why, I just tried it and it failed).
			if (Interlocked.CompareExchange(ref this.scannedQrCodesCount, 1, 0) == 1)
				return;

			this.Scanner.IsAnalyzing = false;

			string Url = result.Text?.Trim();

			if (this.ViewModel is ScanQrCodeViewModel ScanQrCodeViewModel)
			{
				ScanQrCodeViewModel.Url = Url;
				ScanQrCodeViewModel.TrySetResultAndClosePage(Url);
			}
		}
	}
	*/

	private async void OpenButton_Click(object Sender, EventArgs e)
	{
		//!!!
		/*
		if (this.ViewModel is ScanQrCodeViewModel ScanQrCodeViewModel)
		{
			string Url = ScanQrCodeViewModel.LinkText?.Trim();
			try
			{
				string Scheme = Constants.UriSchemes.GetScheme(Url);

				if (string.IsNullOrWhiteSpace(Scheme))
				{
					await this.ViewModel.UiSerializer.DisplayAlert(LocalizationResourceManager.Current["ErrorTitle"], LocalizationResourceManager.Current["UnsupportedUriScheme"], LocalizationResourceManager.Current["Ok"]);
					return;
				}
			}
			catch (Exception ex)
			{
				await this.ViewModel.UiSerializer.DisplayAlert(LocalizationResourceManager.Current["ErrorTitle"], ex.Message, LocalizationResourceManager.Current["Ok"]);
				return;
			}

			ScanQrCodeViewModel.TrySetResultAndClosePage(Url);
		}
		*/
	}

	private void ZXingScannerView_SizeChanged(object Sender, EventArgs e)
	{
		// cf. https://github.com/Redth/ZXing.Net.Mobile/issues/808
		//!!!
		/*
		try
		{
			this.Scanner.Options.CameraResolutionSelector = this.SelectLowestResolutionMatchingDisplayAspectRatio;

			this.Scanner.IsScanning = true;
			this.Scanner.IsAnalyzing = true;

			this.scannerRendered = true;
		}
		catch (Exception Exception)
		{
			this.ViewModel.LogService.LogException(Exception);
		}
		*/
	}

	//!!!
	/*
	private CameraResolution SelectLowestResolutionMatchingDisplayAspectRatio(List<CameraResolution> AvailableResolutions)
	{
		CameraResolution Result = null;
		double AspectTolerance = 0.1;

		double DisplayOrientationHeight = this.Scanner.Width;
		double DisplayOrientationWidth = this.Scanner.Height;

		double TargetRatio = DisplayOrientationHeight / DisplayOrientationWidth;
		double TargetHeight = DisplayOrientationHeight;
		double MinDiff = double.MaxValue;

		foreach (CameraResolution Resolution in AvailableResolutions)
		{
			if (Math.Abs(((double)Resolution.Width / Resolution.Height) - TargetRatio) >= AspectTolerance)
				continue;

			if (Math.Abs(Resolution.Height - TargetHeight) < MinDiff)
			{
				MinDiff = Math.Abs(Resolution.Height - TargetHeight);
				Result = Resolution;
			}
		}

		if (Result is null)
		{
			foreach (CameraResolution Resolution in AvailableResolutions)
			{
				if (Math.Abs(Resolution.Height - TargetHeight) < MinDiff)
				{
					MinDiff = Math.Abs(Resolution.Height - TargetHeight);
					Result = Resolution;
				}
			}
		}

		return Result;
	}
	*/
}
