using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.Threading;
using System.Threading.Tasks;
using CommunityToolkit.Mvvm.ComponentModel;
using CommunityToolkit.Mvvm.Input;
using Microsoft.Maui.ApplicationModel.DataTransfer;
using Microsoft.Maui.Storage;
using Microsoft.Extensions.DependencyInjection;
using NeuroAccessMaui.Resources.Languages;
using NeuroAccessMaui.Services;
using NeuroAccessMaui.Services.Authentication;
using NeuroAccessMaui.Services.Nfc;
using NeuroAccessMaui.Services.Nfc.Ui;
using NeuroAccessMaui.UI;
using Waher.Runtime.Settings;
using System.IO;
using System.Text.Json;

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
		private readonly INfcScanHistoryService nfcScanHistoryService;
		private readonly IAuthenticationService authenticationService;

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
			this.nfcScanHistoryService = ServiceRef.Provider.GetRequiredService<INfcScanHistoryService>();
			this.authenticationService = ServiceRef.Provider.GetRequiredService<IAuthenticationService>();

			this.SelectedTabKey = "Read";
			this.UpdateFromSnapshot(this.nfcTagSnapshotService.LastSnapshot);
		}

		/// <inheritdoc />
		public override async Task OnInitializeAsync()
		{
			await base.OnInitializeAsync();
			this.nfcTagSnapshotService.SnapshotChanged += this.NfcTagSnapshotService_SnapshotChanged;
			this.nfcScanHistoryService.HistoryChanged += this.NfcScanHistoryService_HistoryChanged;
			await this.LoadSafeScanSettingAsync();
		}

		/// <inheritdoc />
		public override async Task OnDisposeAsync()
		{
			this.nfcTagSnapshotService.SnapshotChanged -= this.NfcTagSnapshotService_SnapshotChanged;
			this.nfcScanHistoryService.HistoryChanged -= this.NfcScanHistoryService_HistoryChanged;

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

		/// <summary>
		/// Gets the style to use for the History tab button.
		/// </summary>
		public Style HistoryTabButtonStyle => this.SelectedTabKey == "History" ? AppStyles.FilledTextButton : this.GetOutlinedTextButtonStyle();

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
		private ObservableCollection<NfcNdefRecordSnapshot> lastNdefRecords = new();

		[ObservableProperty]
		private bool hasNdefRecords;

		[ObservableProperty]
		private bool safeScanEnabled;

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

		[RelayCommand]
		private async Task SelectHistoryTab()
		{
			this.SelectedTabKey = "History";
			await this.LoadHistoryAsync();
		}

		[ObservableProperty]
		private ObservableCollection<NfcScanHistoryListItem> historyItems = new();

		[ObservableProperty]
		private bool hasHistoryItems;

		/// <summary>
		/// Gets a value indicating whether the history list is empty.
		/// </summary>
		public bool HasNoHistoryItems => !this.HasHistoryItems;

		[ObservableProperty]
		private NfcScanHistoryListItem? selectedHistoryItem;

		[ObservableProperty]
		private bool hasSelectedHistoryUri;

		[ObservableProperty]
		private bool hasSelectedHistoryItem;

		[ObservableProperty]
		private bool hasSelectedHistoryNdefRecords;

		[RelayCommand(AllowConcurrentExecutions = false)]
		private async Task RefreshHistoryAsync()
		{
			await this.LoadHistoryAsync();
		}

		[RelayCommand(AllowConcurrentExecutions = false)]
		private async Task ClearHistoryAsync()
		{
			try
			{
				this.SelectedHistoryItem = null;

				if (!await ServiceRef.UiService.DisplayAlert(
					ServiceRef.Localizer[nameof(AppResources.Question)],
					ServiceRef.Localizer[nameof(AppResources.NfcHistoryClearQuestion)],
					ServiceRef.Localizer[nameof(AppResources.Yes)],
					ServiceRef.Localizer[nameof(AppResources.Cancel)]))
				{
					return;
				}

				if (!await this.authenticationService.AuthenticateUserAsync(AuthenticationPurpose.NfcTagDetected))
					return;

				await this.nfcScanHistoryService.ClearAsync(CancellationToken.None);
				await this.LoadHistoryAsync();
			}
			catch (Exception Ex)
			{
				ServiceRef.LogService.LogException(Ex);
			}
		}

		[RelayCommand(AllowConcurrentExecutions = false)]
		private async Task CopySelectedHistoryUriAsync()
		{
			try
			{
				string Candidate = this.SelectedHistoryItem?.ExtractedUri?.Trim() ?? string.Empty;
				if (string.IsNullOrWhiteSpace(Candidate))
					return;

				await Clipboard.Default.SetTextAsync(Candidate);
			}
			catch (Exception Ex)
			{
				ServiceRef.LogService.LogException(Ex);
			}
		}

		[RelayCommand(AllowConcurrentExecutions = false)]
		private async Task OpenSelectedHistoryUriAsync()
		{
			try
			{
				string Candidate = this.SelectedHistoryItem?.ExtractedUri?.Trim() ?? string.Empty;
				if (string.IsNullOrWhiteSpace(Candidate))
					return;

				if (!await this.authenticationService.AuthenticateUserAsync(AuthenticationPurpose.NfcTagDetected))
					return;

				await App.OpenUrlAsync(Candidate);
			}
			catch (Exception Ex)
			{
				ServiceRef.LogService.LogException(Ex);
			}
		}

		[RelayCommand(AllowConcurrentExecutions = false)]
		private async Task ExportSelectedHistoryJsonAsync()
		{
			try
			{
				if (this.SelectedHistoryItem is null)
					return;

				string FileName = $"nfc-scan-{DateTime.UtcNow:yyyyMMddTHHmmssZ}.json";
				string Path = System.IO.Path.Combine(FileSystem.CacheDirectory, FileName);

				JsonSerializerOptions Options = new()
				{
					WriteIndented = true
				};

				string Json = JsonSerializer.Serialize(this.SelectedHistoryItem, Options);
				await File.WriteAllTextAsync(Path, Json);

				await Share.Default.RequestAsync(new ShareFileRequest
				{
					Title = ServiceRef.Localizer[nameof(AppResources.NfcHistoryExportTitle)],
					File = new ShareFile(Path)
				});
			}
			catch (Exception Ex)
			{
				ServiceRef.LogService.LogException(Ex);
			}
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

		private void NfcScanHistoryService_HistoryChanged(object? Sender, EventArgs e)
		{
			if (this.SelectedTabKey != "History")
				return;

			MainThread.BeginInvokeOnMainThread(async () =>
			{
				await this.LoadHistoryAsync();
			});
		}

		private async Task LoadHistoryAsync()
		{
			try
			{
				string? SelectedObjectId = this.SelectedHistoryItem?.ObjectId;
				IReadOnlyList<NfcScanHistoryRecord> Records = await this.nfcScanHistoryService.GetRecentAsync(100, CancellationToken.None);
				MainThread.BeginInvokeOnMainThread(() =>
				{
					this.HistoryItems.Clear();
					foreach (NfcScanHistoryRecord Record in Records)
					{
						this.HistoryItems.Add(NfcScanHistoryListItem.FromRecord(Record));
					}
					this.HasHistoryItems = this.HistoryItems.Count > 0;

					if (this.HistoryItems.Count == 0)
					{
						this.SelectedHistoryItem = null;
					}
					else if (!string.IsNullOrEmpty(SelectedObjectId))
					{
						NfcScanHistoryListItem? StillSelected = this.HistoryItems.FirstOrDefault(x =>
							string.Equals(x.ObjectId, SelectedObjectId, StringComparison.OrdinalIgnoreCase));
						this.SelectedHistoryItem = StillSelected;
					}
				});
			}
			catch (Exception Ex)
			{
				ServiceRef.LogService.LogException(Ex);
			}
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
				this.LastNdefRecords.Clear();
				this.HasNdefRecords = false;
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

			this.LastNdefRecords.Clear();
			if (Snapshot.NdefRecords is not null)
			{
				foreach (NfcNdefRecordSnapshot Record in Snapshot.NdefRecords)
					this.LastNdefRecords.Add(Record);
			}
			this.HasNdefRecords = this.LastNdefRecords.Count > 0;
		}

		partial void OnSelectedTabKeyChanged(string value)
		{
			this.OnPropertyChanged(nameof(this.ReadTabButtonStyle));
			this.OnPropertyChanged(nameof(this.WriteTabButtonStyle));
			this.OnPropertyChanged(nameof(this.HistoryTabButtonStyle));
		}

		partial void OnHasHistoryItemsChanged(bool value)
		{
			this.OnPropertyChanged(nameof(this.HasNoHistoryItems));
		}

		partial void OnSelectedHistoryItemChanged(NfcScanHistoryListItem? value)
		{
			this.HasSelectedHistoryItem = value is not null;
			this.HasSelectedHistoryUri = !string.IsNullOrWhiteSpace(value?.ExtractedUri);
			this.HasSelectedHistoryNdefRecords = value?.NdefRecords is not null && value.NdefRecords.Count > 0;
		}

		partial void OnSafeScanEnabledChanged(bool value)
		{
			MainThread.BeginInvokeOnMainThread(async () =>
			{
				try
				{
					await RuntimeSettings.SetAsync("NFC.SafeScan.Enabled", value);
				}
				catch (Exception Ex)
				{
					ServiceRef.LogService.LogException(Ex);
				}
			});
		}

		private async Task LoadSafeScanSettingAsync()
		{
			try
			{
				bool Enabled = await RuntimeSettings.GetAsync("NFC.SafeScan.Enabled", false);
				this.SafeScanEnabled = Enabled;
			}
			catch (Exception Ex)
			{
				ServiceRef.LogService.LogException(Ex);
			}
		}

		[RelayCommand(AllowConcurrentExecutions = false)]
		private async Task CopyLastUriAsync()
		{
			try
			{
				if (string.IsNullOrWhiteSpace(this.LastExtractedUri))
					return;

				await Clipboard.Default.SetTextAsync(this.LastExtractedUri);
			}
			catch (Exception Ex)
			{
				ServiceRef.LogService.LogException(Ex);
			}
		}

		[RelayCommand(AllowConcurrentExecutions = false)]
		private async Task OpenLastUriAsync()
		{
			try
			{
				string Candidate = this.LastExtractedUri?.Trim() ?? string.Empty;
				if (string.IsNullOrWhiteSpace(Candidate))
					return;

				if (!await this.authenticationService.AuthenticateUserAsync(AuthenticationPurpose.NfcTagDetected))
					return;

				await App.OpenUrlAsync(Candidate);
			}
			catch (Exception Ex)
			{
				ServiceRef.LogService.LogException(Ex);
			}
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

	/// <summary>
	/// Represents a scan history item suitable for UI binding and export.
	/// </summary>
	public sealed class NfcScanHistoryListItem
	{
		/// <summary>
		/// Gets the record identifier.
		/// </summary>
		public string? ObjectId { get; init; }

		/// <summary>
		/// Gets the detection time in UTC.
		/// </summary>
		public DateTime DetectedAtUtc { get; init; }

		/// <summary>
		/// Gets the tag identifier (hex).
		/// </summary>
		public string TagIdHex { get; init; } = string.Empty;

		/// <summary>
		/// Gets the high-level tag type.
		/// </summary>
		public string TagType { get; init; } = string.Empty;

		/// <summary>
		/// Gets the detected interface summary.
		/// </summary>
		public string InterfacesSummary { get; init; } = string.Empty;

		/// <summary>
		/// Gets an optional NDEF summary.
		/// </summary>
		public string? NdefSummary { get; init; }

		/// <summary>
		/// Gets an optional extracted URI.
		/// </summary>
		public string? ExtractedUri { get; init; }

		/// <summary>
		/// Gets optional decoded record details.
		/// </summary>
		public IReadOnlyList<NfcNdefRecordSnapshot>? NdefRecords { get; init; }

		/// <summary>
		/// Creates an instance from a persisted history record.
		/// </summary>
		/// <param name="Record">Record to convert.</param>
		/// <returns>Converted list item.</returns>
		public static NfcScanHistoryListItem FromRecord(NfcScanHistoryRecord Record)
		{
			return new NfcScanHistoryListItem
			{
				ObjectId = Record.ObjectId,
				DetectedAtUtc = Record.DetectedAtUtc,
				TagIdHex = Record.TagId.ToString(),
				TagType = Record.TagType,
				InterfacesSummary = Record.InterfacesSummary,
				NdefSummary = Record.NdefSummary,
				ExtractedUri = Record.ExtractedUri,
				NdefRecords = Record.NdefRecords
			};
		}
	}
}
