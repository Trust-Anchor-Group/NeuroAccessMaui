using System.ComponentModel;
using CommunityToolkit.Mvvm.ComponentModel;
using CommunityToolkit.Mvvm.Input;
using NeuroAccessMaui.Services;
using NeuroAccessMaui.Services.Localization;

namespace NeuroAccessMaui.UI.Pages.Main.QR
{
	/// <summary>
	/// The view model to bind to when scanning a QR code.
	/// </summary>
	public partial class ScanQrCodeViewModel(ScanQrCodeNavigationArgs? NavigationArgs) : BaseViewModel
	{
		private IDispatcherTimer? countDownTimer;
		private ScanQrCodeNavigationArgs? navigationArgs = NavigationArgs;

		/// <inheritdoc />
		protected override async Task OnInitialize()
		{
			await base.OnInitialize();

			LocalizationManager.Current.PropertyChanged += this.LocalizationManagerEventHandler;

			if (App.Current is not null)
			{
				this.countDownTimer = App.Current.Dispatcher.CreateTimer();
				this.countDownTimer.Interval = TimeSpan.FromMilliseconds(500);
				this.countDownTimer.Tick += this.CountDownEventHandler;
			}

			if ((this.navigationArgs is null) && ServiceRef.NavigationService.TryGetArgs(out ScanQrCodeNavigationArgs? Args))
			{
				this.navigationArgs = Args;
				this.OnPropertyChanged(nameof(this.HasAllowedSchemas));
				this.OnPropertyChanged(nameof(this.LocalizedQrPageTitle));
			}
		}

		/// <inheritdoc/>
		protected override async Task OnDispose()
		{
			LocalizationManager.Current.PropertyChanged -= this.LocalizationManagerEventHandler;

			if (this.countDownTimer is not null)
			{
				this.countDownTimer.Stop();
				this.countDownTimer.Tick -= this.CountDownEventHandler;
				this.countDownTimer = null;
			}

			if (this.navigationArgs?.QrCodeScanned is TaskCompletionSource<string> TaskSource)
				TaskSource.TrySetResult(string.Empty);

			await base.OnDispose();
		}

		private void CountDownEventHandler(object? sender, EventArgs e)
		{
			this.countDownTimer?.Stop();
			this.OnPropertyChanged(nameof(this.BackgroundIcon));
		}

		public void LocalizationManagerEventHandler(object? sender, PropertyChangedEventArgs e)
		{
			this.OnPropertyChanged(nameof(this.LocalizedQrPageTitle));
		}

		public Task DoSwitchMode(bool IsAutomaticScan)
		{
			this.IsAutomaticScan = IsAutomaticScan;
			return Task.CompletedTask;
		}

		public Task SetScannedText(string? ScannedText)
		{
			this.ScannedText = ScannedText ?? string.Empty;
			this.countDownTimer?.Stop();
			this.OnPropertyChanged(nameof(this.BackgroundIcon));

			if (this.CanOpenScanned)
				return this.TrySetResultAndClosePage(this.ScannedText!.Trim());

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
		private string? manualText;

		/// <summary>
		/// The scanned QR text
		/// </summary>
		[ObservableProperty]
		private string? scannedText;

		/// <summary>
		/// Gets or sets whether the QR scanning is automatic or manual.
		/// </summary>
		[ObservableProperty]
		private bool isAutomaticScan = true;

		/// <summary>
		/// If scanning of codes is restricted to a set of allowed schemas.
		/// </summary>
		public bool HasAllowedSchemas => this.navigationArgs?.AllowedSchemas is not null && this.navigationArgs.AllowedSchemas.Length > 0;

		/// <summary>
		/// Background color of displayed icon.
		/// </summary>
		public Color BackgroundIcon
		{
			get
			{
				if (string.IsNullOrEmpty(this.ScannedText))
					return Colors.White;

				string Url = this.ScannedText.Trim();

				if ((this.countDownTimer?.IsRunning ?? false) &&
					(this.navigationArgs?.AllowedSchemas is not null) &&
					this.navigationArgs.AllowedSchemas.Length > 0)
				{
					return this.IsPermittedUrl(Url) ? Colors.White : Color.FromUint(0xFFFF7585);
				}

				return Colors.White;
			}
		}

		private bool IsPermittedUrl(string Url)
		{
			if (this.navigationArgs?.AllowedSchemas is not null &&
				System.Uri.TryCreate(Url, UriKind.Absolute, out Uri? Uri))
			{
				foreach (string Schema in this.navigationArgs.AllowedSchemas)
				{
					if (string.Equals(Uri.Scheme, Schema, StringComparison.OrdinalIgnoreCase))
						return true;
				}
			}

			return false;
		}

		/// <summary>
		/// The localized page title text to display.
		/// </summary>
		public string LocalizedQrPageTitle
		{
			get
			{
				if (this.navigationArgs?.QrTitle is not null)
					return ServiceRef.Localizer[this.navigationArgs.QrTitle];

				return string.Empty;
			}
		}

		private bool CanOpen(string? Text)
		{
			string? Url = Text?.Trim();
			if (string.IsNullOrEmpty(Url))
				return false;

			if (this.navigationArgs?.AllowedSchemas is not null && this.navigationArgs.AllowedSchemas.Length > 0)
				return this.IsPermittedUrl(Url);
			else
				return Url.Length > 0;
		}

		/// <summary>
		/// If the scanned code can be opened
		/// </summary>
		public bool CanOpenScanned => this.CanOpen(this.ScannedText);

		/// <summary>
		/// If the manual code can be opened
		/// </summary>
		public bool CanOpenManual => this.CanOpen(this.ManualText);

		#endregion

		[RelayCommand(CanExecute = nameof(CanOpenManual))]
		private Task OpenUrl()
		{
			return this.TrySetResultAndClosePage(this.ManualText!.Trim());
		}

		[RelayCommand]
		private Task GoBack()
		{
			return this.TrySetResultAndClosePage(null);
		}
	}
}
