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
	public partial class ScanQrCodeViewModel : BaseViewModel, IAsyncDisposable
	{
		private readonly ScanQrCodeNavigationArgs? navigationArgs;
		private bool disposed;
		private Task? disposalTask;

		/// <summary>
		/// The view model to bind to when scanning a QR code.
		/// </summary>
		public ScanQrCodeViewModel()
		{
			this.navigationArgs = ServiceRef.NavigationService.PopLatestArgs<ScanQrCodeNavigationArgs>();
			// Default: automatic scan => enable detecting
			this.isAutomaticScan = true; // backing field to avoid recursion
			this.isDetecting = true;
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


		[RelayCommand(CanExecute = nameof(CanSwitchMode))]
		private void SwitchMode()
		{
			this.IsAutomaticScan = !this.IsAutomaticScan;
		}

		[RelayCommand(CanExecute = nameof(CanSwitchCamera))]
		private void SwitchCamera()
		{
			int Index = this.cameras.FindIndex(Camera => Camera.Id == this.SelectedCamera?.Id);
			this.ResetScannerSession();
			this.SelectedCamera = this.cameras[(Index + 1) % this.cameras.Count];
		}

		[RelayCommand(CanExecute = nameof(CanUseTorch))]
		private void SwitchTorch()
		{
			this.IsTorchOn = !this.IsTorchOn;
		}

		private static bool CanPickPhoto() => false; // Disabled for now

		[RelayCommand(CanExecute = nameof(CanPickPhoto))]
		private async Task PickPhoto()
		{
#if ANDROID
			// Platform-specific implementation previously in page; keep placeholder for future
			await Task.CompletedTask;
#else
			await Task.CompletedTask;
#endif
		}


		/// <inheritdoc />
		public override async Task OnInitializeAsync()
		{
			await base.OnInitializeAsync();
			if (this.disposed)
				return;

			LocalizationManager.Current.PropertyChanged += this.LocalizationManagerEventHandler;

		}

		/// <inheritdoc/>
		public override Task OnDisposeAsync() => MainThread.InvokeOnMainThreadAsync(() => this.disposalTask ??= this.DisposeCoreAsync());

		/// <summary>Cancels scanner work and releases subscriptions through the view-model lifecycle.</summary>
		/// <returns>A task representing completion of view-model cleanup.</returns>
		public virtual async ValueTask DisposeAsync()
		{
			await this.OnDisposeAsync().ConfigureAwait(false);
			GC.SuppressFinalize(this);
		}

		private async Task DisposeCoreAsync()
		{
			this.disposed = true;
			LocalizationManager.Current.PropertyChanged -= this.LocalizationManagerEventHandler;

			this.EndScannerSession();
			this.AttachCamera(null);

			if (this.navigationArgs?.QrCodeScanned is TaskCompletionSource<string> TaskSource)
				TaskSource.TrySetResult(string.Empty);

			await base.OnDisposeAsync();
		}

		private void OnBackgroundColorChanged()
		{
			this.OnPropertyChanged(nameof(this.IconBackgroundColor));

			foreach (UriSchemaIcon Icon in this.Icons)
				Icon.BackgroundColorChanged();
		}

		private void LocalizationManagerEventHandler(object? Sender, PropertyChangedEventArgs e)
		{
			MainThread.BeginInvokeOnMainThread(() =>
			{
				this.OnPropertyChanged(nameof(this.LocalizedQrPageTitle));
				this.OnPropertyChanged(nameof(this.FeedbackDescription));
				this.OnPropertyChanged(nameof(this.TorchDescription));
			});
		}

		/// <summary>Changes between camera scanning and manual entry.</summary>
		/// <param name="IsAutomaticScan">Whether to use the camera.</param>
		/// <returns>A completed task after updating the mode.</returns>
		public Task DoSwitchMode(bool IsAutomaticScan)
		{
			this.IsAutomaticScan = IsAutomaticScan;
			return Task.CompletedTask;
		}

		/// <summary>Applies the existing acceptance rules and presents scan feedback.</summary>
		/// <param name="ScannedText">The decoded value.</param>
		/// <returns>A task representing feedback and result delivery.</returns>
		public Task SetScannedText(string? ScannedText) => this.ProcessCandidateAsync(ScannedText);

		/// <summary>
		/// Tries to set the Scan QR Code result and close the scan page.
		/// </summary>
		/// <param name="Url">The URL to set.</param>
		/// <returns>A task representing navigation and result delivery.</returns>
		private async Task TrySetResultAndClosePageAsync(string? Url)
		{
			if (this.navigationArgs?.QrCodeScanned is not null)
			{
				TaskCompletionSource<string?> TaskSource = this.navigationArgs.QrCodeScanned;
				this.navigationArgs.QrCodeScanned = null;
				this.State = QrScannerState.Closing;
				this.EndScannerSession();

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

		partial void OnIsAutomaticScanChanged(bool value)
		{
			this.ResetScannerSession();
		}

		/// <summary>
		/// If camera should be detecting (bound to view control)
		/// </summary>
		[ObservableProperty]
		private bool isDetecting;

		/// <summary>
		/// Torch state
		/// </summary>
		[ObservableProperty]
		[NotifyPropertyChangedFor(nameof(TorchDescription))]
		private bool isTorchOn;

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

				if (this.State == QrScannerState.Rejected &&
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
		public bool CanOpenManual => this.CanSwitchMode && this.CanOpen(this.ManualText);

		#endregion

		[RelayCommand(CanExecute = nameof(CanOpenManual))]
		private Task OpenUrlAsync()
		{
			return this.ProcessCandidateAsync(this.ManualText);
		}

		/// <inheritdoc/>
		public override Task GoBack()
		{
			return this.TrySetResultAndClosePageAsync(null);
		}
	}
}
