using System.ComponentModel;
using CommunityToolkit.Mvvm.ComponentModel;
using CommunityToolkit.Mvvm.Input;
using NeuroAccessMaui.Services;
using NeuroAccessMaui.Services.Localization;

namespace NeuroAccessMaui.UI.Pages.Main.QR;

/// <summary>
/// The view model to bind to when scanning a QR code.
/// </summary>
public partial class ScanQrCodeViewModel : BaseViewModel
{
	private IDispatcherTimer? countDownTimer;
	private ScanQrCodeNavigationArgs? navigationArgs;

	/// <summary>
	/// Creates a new instance of the <see cref="ScanQrCodeViewModel"/> class.
	/// </summary>
	public ScanQrCodeViewModel(ScanQrCodeNavigationArgs? NavigationArgs)
	{
		this.navigationArgs = NavigationArgs;
	}

	/// <inheritdoc />
	protected override async Task OnInitialize()
	{
		await base.OnInitialize();

		LocalizationManager.Current.PropertyChanged += this.PropertyChangedEventHandler;

		if (App.Current is not null)
		{
			this.countDownTimer = App.Current.Dispatcher.CreateTimer();
			this.countDownTimer.Interval = TimeSpan.FromMilliseconds(500);
			this.countDownTimer.Tick += this.CountDownEventHandler;
		}

		if ((this.navigationArgs is null) && ServiceRef.NavigationService.TryGetArgs(out ScanQrCodeNavigationArgs? Args))
		{
			this.navigationArgs = Args;
			this.OnPropertyChanged(nameof(this.HasAllowedSchema));
			this.OnPropertyChanged(nameof(this.LocalizedQrPageTitle));
		}
	}

	/// <inheritdoc/>
	protected override async Task OnDispose()
	{
		LocalizationManager.Current.PropertyChanged -= this.PropertyChangedEventHandler;

		if (this.countDownTimer is not null)
		{
			this.countDownTimer.Stop();
			this.countDownTimer.Tick -= this.CountDownEventHandler;
			this.countDownTimer = null;
		}

		if (this.navigationArgs?.QrCodeScanned is TaskCompletionSource<string> TaskSource)
		{
			TaskSource.TrySetResult(string.Empty);
		}

		await base.OnDispose();
	}

	private void CountDownEventHandler(object? sender, EventArgs e)
	{
		this.countDownTimer?.Stop();
		this.OnPropertyChanged(nameof(this.BackgroundIcon));
	}

	public void PropertyChangedEventHandler(object? sender, PropertyChangedEventArgs e)
	{
		this.OnPropertyChanged(nameof(this.LocalizedQrPageTitle));
	}

	public Task DoSwitchMode(bool IsAutomaticScan)
	{
		this.IsAutomaticScan = IsAutomaticScan;
		return Task.CompletedTask;
	}

	public Task SetQrText(string? qrText)
	{
		this.QrText = qrText;
		this.countDownTimer?.Stop();
		this.OnPropertyChanged(nameof(this.BackgroundIcon));

		if (this.CanOpenQr)
		{
			return this.TrySetResultAndClosePage(this.QrText!.Trim());
		}

		this.countDownTimer?.Start();
		this.OnPropertyChanged(nameof(this.BackgroundIcon));

		return Task.CompletedTask;
	}

	/// <summary>
	/// Tries to set the Scan QR Code result and close the scan page.
	/// </summary>
	/// <param name="Url">The URL to set.</param>
	private async Task TrySetResultAndClosePage(string? Url)
	{
		if (this.navigationArgs?.QrCodeScanned is not null)
		{
			TaskCompletionSource<string?> TaskSource = this.navigationArgs.QrCodeScanned;
			this.navigationArgs.QrCodeScanned = null;

			await MainThread.InvokeOnMainThreadAsync(async () =>
			{
				try
				{
					await ServiceRef.NavigationService.GoBackAsync();
					TaskSource.TrySetResult(Url);
				}
				catch (Exception ex)
				{
					ServiceRef.LogService.LogException(ex);
				}
			});
		}
	}

	#region Properties

	/// <summary>
	/// The manually typed QR text
	/// </summary>
	[ObservableProperty]
	[NotifyCanExecuteChangedFor(nameof(OpenUrlCommand))]
	private string? urlText;

	/// <summary>
	/// The scanned QR text
	/// </summary>
	[ObservableProperty]
	private string? qrText;

	/// <summary>
	/// Gets or sets whether the QR scanning is automatic or manual.
	/// </summary>
	[ObservableProperty]
	private bool isAutomaticScan = true;

	public bool HasAllowedSchema => this.navigationArgs?.AllowedSchema is not null;

	public Color BackgroundIcon
	{
		get
		{
			if (this.QrText is null)
			{
				return Colors.White;
			}

			string Url = this.QrText.Trim();

			if ((this.countDownTimer?.IsRunning ?? false) && (this.navigationArgs?.AllowedSchema is not null))
			{
				bool IsValid = System.Uri.TryCreate(Url, UriKind.Absolute, out Uri? Uri) &&
					string.Equals(Uri.Scheme, this.navigationArgs?.AllowedSchema, StringComparison.OrdinalIgnoreCase);

				return IsValid ? Colors.White : Color.FromUint(0xFFFF7585);
			}

			return Colors.White;
		}
	}

	/// <summary>
	/// The localized page title text to display.
	/// </summary>
	public string LocalizedQrPageTitle
	{
		get
		{
			if (this.navigationArgs?.QrTitle is not null)
			{
				return ServiceRef.Localizer[this.navigationArgs.QrTitle];
			}

			return string.Empty;
		}
	}

	private bool CanOpen(string? Text)
	{
		if (Text is null)
		{
			return false;
		}

		string Url = Text.Trim();

		if (this.navigationArgs?.AllowedSchema is not null)
		{
			return System.Uri.TryCreate(Url, UriKind.Absolute, out Uri? Uri) &&
				string.Equals(Uri.Scheme, this.navigationArgs?.AllowedSchema, StringComparison.OrdinalIgnoreCase);
		}

		return Url.Length > 0;
	}

	public bool CanOpenQr => this.CanOpen(this.QrText);

	public bool CanOpenUrl => this.CanOpen(this.UrlText);

	#endregion

	[RelayCommand(CanExecute = nameof(CanOpenUrl))]
	private Task OpenUrl()
	{
		return this.TrySetResultAndClosePage(this.UrlText!.Trim());
	}

	[RelayCommand]
	private Task GoBack()
	{
		return this.TrySetResultAndClosePage(null);
	}
}
