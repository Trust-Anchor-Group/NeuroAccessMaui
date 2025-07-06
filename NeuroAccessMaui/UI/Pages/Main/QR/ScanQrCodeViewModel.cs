using System.Collections.ObjectModel;
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
	public partial class ScanQrCodeViewModel : BaseViewModel
	{
		private readonly ScanQrCodeNavigationArgs? navigationArgs;
		private IDispatcherTimer? countDownTimer;

		/// <summary>
		/// The view model to bind to when scanning a QR code.
		/// </summary>
		/// <param name="Args">Navigation arguments.</param>
		public ScanQrCodeViewModel(ScanQrCodeNavigationArgs? Args)
		{
			this.navigationArgs = Args;
			if (this.navigationArgs is not null &&
				this.navigationArgs.AllowedSchemas is not null &&
				this.navigationArgs.AllowedSchemas.Length > 0 &&
				this.Icons.Count == 0)
			{
				foreach (string Schema in this.navigationArgs.AllowedSchemas)
				{
					switch (Schema)
					{
						case Constants.UriSchemes.IotId:
							this.Icons.Add(new UriSchemaIcon(Geometries.UserIconPath, Geometries.UserColor, this));
							break;

						case Constants.UriSchemes.IotDisco:
							this.Icons.Add(new UriSchemaIcon(Geometries.ThingsIconPath, Geometries.ThingsColor, this));
							break;

						case Constants.UriSchemes.IotSc:
							this.Icons.Add(new UriSchemaIcon(Geometries.ContractIconPath, Geometries.ContractColor, this));
							break;

						case Constants.UriSchemes.TagSign:
							this.Icons.Add(new UriSchemaIcon(Geometries.SignatureIconPath, Geometries.SignatureColor, this));
							break;

						case Constants.UriSchemes.EDaler:
							this.Icons.Add(new UriSchemaIcon(Geometries.EDalerIconPath, Geometries.EDalerColor, this));
							break;

						case Constants.UriSchemes.NeuroFeature:
							this.Icons.Add(new UriSchemaIcon(Geometries.TokenIconPath, Geometries.TokenColor, this));
							break;

						case Constants.UriSchemes.Onboarding:
							this.Icons.Add(new UriSchemaIcon(Geometries.OnboardingIconPath, Geometries.OnboardingColor, this));
							break;

						case Constants.UriSchemes.Aes256:
							this.Icons.Add(new UriSchemaIcon(Geometries.Aes256IconPath, Geometries.Aes256Color, this));
							break;

						case Constants.UriSchemes.Xmpp:
						default:
							break;
					}
				}

				if (this.Icons.Count == 0)
					this.Icons.Add(new UriSchemaIcon(Geometries.SignatureIconPath, Geometries.SignatureColor, this));
			}
		}

		/// <inheritdoc />
		public override async Task OnInitializeAsync()
		{
			await base.OnInitializeAsync();

			LocalizationManager.Current.PropertyChanged += this.LocalizationManagerEventHandler;

			if (App.Current is not null)
			{
				this.countDownTimer = App.Current.Dispatcher.CreateTimer();
				this.countDownTimer.Interval = TimeSpan.FromMilliseconds(500);
				this.countDownTimer.Tick += this.CountDownEventHandler;
			}
		}

		/// <inheritdoc/>
		public override async Task OnDisposeAsync()
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

			await base.OnDisposeAsync();
		}

		private void CountDownEventHandler(object? sender, EventArgs e)
		{
			this.countDownTimer?.Stop();
			this.OnBackgroundColorChanged();
		}

		private void OnBackgroundColorChanged()
		{
			this.OnPropertyChanged(nameof(this.IconBackgroundColor));

			foreach (UriSchemaIcon Icon in this.Icons)
				Icon.BackgroundColorChanged();
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
			this.OnBackgroundColorChanged();

			if (this.CanOpen(ScannedText))
			{
				return this.TrySetResultAndClosePage(ScannedText!.Trim());
			}
			this.countDownTimer?.Start();
			this.OnBackgroundColorChanged();

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
						await base.GoBack();
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
		/// Recognized URI-schema icons.
		/// </summary>
		[ObservableProperty]
		[NotifyPropertyChangedFor(nameof(MultipleIcons))]
		[NotifyPropertyChangedFor(nameof(SingleIcon))]
		private ObservableCollection<UriSchemaIcon> icons = [];

		/// <summary>
		/// If the view shows more than one icon.
		/// </summary>
		public bool MultipleIcons => this.Icons.Count > 1;

		/// <summary>
		/// If only one icon is shown.
		/// </summary>
		public bool SingleIcon => this.Icons.Count == 1;

		/// <summary>
		/// Background color of displayed icon.
		/// </summary>
		public Color IconBackgroundColor
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
					return this.IsPermittedUrl(Url) ? Colors.White : Colors.LightSalmon;
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

		/// <inheritdoc/>
		public override Task GoBack()
		{
			return this.TrySetResultAndClosePage(null);
		}
	}
}
