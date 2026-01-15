using System;
using System.Threading;
using System.Threading.Tasks;
using CommunityToolkit.Mvvm.ComponentModel;
using CommunityToolkit.Mvvm.Input;
using Microsoft.Extensions.DependencyInjection;
using NeuroAccessMaui.Resources.Languages;
using NeuroAccessMaui.Services;
using NeuroAccessMaui.Services.Nfc;
using NeuroAccessMaui.Services.Nfc.Ui;
using NeuroAccessMaui.UI;

namespace NeuroAccessMaui.UI.Pages.Main.Nfc
{
	/// <summary>
	/// View model for the NFC app page.
	/// </summary>
	public partial class NfcViewModel : BaseViewModel, IDisposable
	{
		private readonly INfcTagSnapshotService nfcTagSnapshotService;
		private readonly INfcWriteService nfcWriteService;
		private readonly INfcScanService nfcScanService;

		private CancellationTokenSource? writeCancellationTokenSource;
		private CancellationTokenSource? readCancellationTokenSource;

		/// <summary>
		/// Initializes a new instance of the <see cref="NfcViewModel"/> class.
		/// </summary>
		public NfcViewModel()
		{
			this.nfcTagSnapshotService = ServiceRef.Provider.GetRequiredService<INfcTagSnapshotService>();
			this.nfcWriteService = ServiceRef.Provider.GetRequiredService<INfcWriteService>();
			this.nfcScanService = ServiceRef.Provider.GetRequiredService<INfcScanService>();

			this.SelectedTabKey = "Read";
			this.UpdateFromSnapshot(this.nfcTagSnapshotService.LastSnapshot);
		}

		/// <inheritdoc />
		public override async Task OnInitializeAsync()
		{
			await base.OnInitializeAsync();
			this.nfcTagSnapshotService.SnapshotChanged += this.NfcTagSnapshotService_SnapshotChanged;
		}

		/// <inheritdoc />
		public override async Task OnDisposeAsync()
		{
			this.nfcTagSnapshotService.SnapshotChanged -= this.NfcTagSnapshotService_SnapshotChanged;

			if (this.readCancellationTokenSource is not null)
			{
				this.readCancellationTokenSource.Cancel();
				this.readCancellationTokenSource.Dispose();
				this.readCancellationTokenSource = null;
			}

			if (this.writeCancellationTokenSource is not null)
			{
				this.writeCancellationTokenSource.Cancel();
				this.writeCancellationTokenSource.Dispose();
				this.writeCancellationTokenSource = null;
			}

			await base.OnDisposeAsync();
		}

		[ObservableProperty]
		private string selectedTabKey = "Read";

		/// <summary>
		/// Gets the style to use for the Read tab button.
		/// </summary>
		public Style ReadTabButtonStyle => this.SelectedTabKey == "Read" ? AppStyles.FilledTextButton : this.GetOutlinedTextButtonStyle();

		/// <summary>
		/// Gets the style to use for the Write tab button.
		/// </summary>
		public Style WriteTabButtonStyle => this.SelectedTabKey == "Write" ? AppStyles.FilledTextButton : this.GetOutlinedTextButtonStyle();

		[ObservableProperty]
		private string lastTagStatusText = string.Empty;

		[ObservableProperty]
		private string lastTagIdHex = string.Empty;

		[ObservableProperty]
		private string lastTagType = string.Empty;

		[ObservableProperty]
		private DateTimeOffset? lastDetectedAtUtc;

		[ObservableProperty]
		private string lastInterfacesSummary = string.Empty;

		[ObservableProperty]
		private string lastNdefSummary = string.Empty;

		[ObservableProperty]
		private string lastExtractedUri = string.Empty;

		[ObservableProperty]
		private bool hasNdefSummary;

		[ObservableProperty]
		private bool hasExtractedUri;

		[ObservableProperty]
		private string uriToWrite = string.Empty;

		[ObservableProperty]
		private string writeStatusText = string.Empty;

		[RelayCommand]
		private void SelectReadTab()
		{
			this.SelectedTabKey = "Read";
		}

		[RelayCommand]
		private void SelectWriteTab()
		{
			this.SelectedTabKey = "Write";
		}

		[RelayCommand(AllowConcurrentExecutions = false)]
		private async Task StartReadScan()
		{
			try
			{
				if (this.readCancellationTokenSource is not null)
				{
					this.readCancellationTokenSource.Cancel();
					this.readCancellationTokenSource.Dispose();
				}

				this.readCancellationTokenSource = new CancellationTokenSource();
				string Prompt = ServiceRef.Localizer["NfcTapTag"];
				string? Uri = await this.nfcScanService.ScanUriAsync(Prompt, null, this.readCancellationTokenSource.Token);

				if (!string.IsNullOrWhiteSpace(Uri))
				{
					NfcTagSnapshot Snapshot = new(
						"N/A",
						DateTimeOffset.UtcNow,
						"NDEF",
						"CoreNFC",
						"URI",
						Uri);
					this.nfcTagSnapshotService.Publish(Snapshot);
				}
			}
			catch (OperationCanceledException)
			{
			}
			catch (Exception Ex)
			{
				ServiceRef.LogService.LogException(Ex);
				this.LastTagStatusText = ServiceRef.Localizer[nameof(AppResources.ErrorTitle)];
			}
		}

		[RelayCommand]
		private void Clear()
		{
			this.nfcTagSnapshotService.Clear();
		}

		[RelayCommand]
		private void Refresh()
		{
			this.UpdateFromSnapshot(this.nfcTagSnapshotService.LastSnapshot);
		}

		[RelayCommand]
		private async Task StartWrite()
		{
			try
			{
				if (string.IsNullOrWhiteSpace(this.UriToWrite))
				{
					this.WriteStatusText = ServiceRef.Localizer[nameof(AppResources.ErrorTitle)];
					return;
				}

				if (this.writeCancellationTokenSource is not null)
				{
					this.writeCancellationTokenSource.Cancel();
					this.writeCancellationTokenSource.Dispose();
				}

				this.writeCancellationTokenSource = new CancellationTokenSource();
				this.WriteStatusText = ServiceRef.Localizer["NfcWritePending"];

				bool Ok = await this.nfcWriteService.WriteUriAsync(this.UriToWrite.Trim(), this.writeCancellationTokenSource.Token);
				this.WriteStatusText = Ok ? ServiceRef.Localizer["NfcWriteSucceeded"] : ServiceRef.Localizer["NfcWriteFailed"];
			}
			catch (OperationCanceledException)
			{
				this.WriteStatusText = ServiceRef.Localizer[nameof(AppResources.Cancel)];
			}
			catch (Exception Ex)
			{
				ServiceRef.LogService.LogException(Ex);
				this.WriteStatusText = ServiceRef.Localizer[nameof(AppResources.ErrorTitle)];
			}
		}

		[RelayCommand]
		private void CancelWrite()
		{
			try
			{
				this.writeCancellationTokenSource?.Cancel();
			}
			catch
			{
			}
		}

		private void NfcTagSnapshotService_SnapshotChanged(object? Sender, EventArgs e)
		{
			this.UpdateFromSnapshot(this.nfcTagSnapshotService.LastSnapshot);
		}

		private void UpdateFromSnapshot(NfcTagSnapshot? Snapshot)
		{
			if (Snapshot is null)
			{
				this.LastTagStatusText = ServiceRef.Localizer["NfcNoTagYet"];
				this.LastTagIdHex = string.Empty;
				this.LastTagType = string.Empty;
				this.LastDetectedAtUtc = null;
				this.LastInterfacesSummary = string.Empty;
				this.LastNdefSummary = string.Empty;
				this.HasNdefSummary = false;
				this.LastExtractedUri = string.Empty;
				this.HasExtractedUri = false;
				return;
			}

			this.LastTagStatusText = ServiceRef.Localizer["NfcLastTag"];
			this.LastTagIdHex = Snapshot.TagIdHex;
			this.LastTagType = Snapshot.TagType;
			this.LastDetectedAtUtc = Snapshot.DetectedAtUtc;
			this.LastInterfacesSummary = Snapshot.InterfacesSummary;

			this.LastNdefSummary = Snapshot.NdefSummary ?? string.Empty;
			this.HasNdefSummary = !string.IsNullOrWhiteSpace(this.LastNdefSummary);

			this.LastExtractedUri = Snapshot.ExtractedUri ?? string.Empty;
			this.HasExtractedUri = !string.IsNullOrWhiteSpace(this.LastExtractedUri);
		}

		partial void OnSelectedTabKeyChanged(string value)
		{
			this.OnPropertyChanged(nameof(this.ReadTabButtonStyle));
			this.OnPropertyChanged(nameof(this.WriteTabButtonStyle));
		}

		private Style GetOutlinedTextButtonStyle()
		{
			Style? Style = AppStyles.TryGetResource<Style>("OutlinedTextButton");
			return Style ?? AppStyles.FilledTextButton;
		}

		private int disposed;

		public void Dispose()
		{
			Dispose(disposing: true);
			GC.SuppressFinalize(this);
		}

		protected virtual void Dispose(bool disposing)
		{
			if (Interlocked.Exchange(ref disposed, 1) == 1)
				return;

			if (!disposing)
			{
				// Only unmanaged/native cleanup here.
				// You currently have none, so do nothing.
				return;
			}

			// Managed cleanup:
			this.nfcTagSnapshotService.SnapshotChanged -= this.NfcTagSnapshotService_SnapshotChanged;

			CancelAndDispose(ref this.readCancellationTokenSource);
			CancelAndDispose(ref this.writeCancellationTokenSource);
		}

		private static void CancelAndDispose(ref CancellationTokenSource? cts)
		{
			var old = Interlocked.Exchange(ref cts, null);
			if (old is null) return;

			try { old.Cancel(); } catch { /* ignore */ }
			old.Dispose();
		}
	}
}
