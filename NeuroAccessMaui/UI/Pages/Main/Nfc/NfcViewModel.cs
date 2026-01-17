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
using System.Linq;
using System.Net;
using System.Text;

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
		private string scanStateText = string.Empty;

		[ObservableProperty]
		private bool hasScanStateText;

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
		private string lastExtractedUriScheme = string.Empty;

		[ObservableProperty]
		private string lastExtractedUriHost = string.Empty;

		[ObservableProperty]
		private ObservableCollection<string> lastExtractedUriWarnings = new();

		[ObservableProperty]
		private bool hasLastExtractedUriWarnings;

		[ObservableProperty]
		private bool isLastExtractedUriDomainTrusted;

		[ObservableProperty]
		private bool canTrustLastExtractedUriDomain;

		[ObservableProperty]
		private ObservableCollection<NfcNdefRecordSnapshot> lastNdefRecords = new();

		[ObservableProperty]
		private bool hasNdefRecords;

		[ObservableProperty]
		private NfcNdefRecordSnapshot? selectedLastNdefRecord;

		[ObservableProperty]
		private bool hasSelectedLastNdefRecord;

		[ObservableProperty]
		private bool safeScanEnabled;

		[ObservableProperty]
		private bool hasSafeScanLinkPreview;

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
		private async Task ExportHistoryJsonAsync()
		{
			try
			{
				IReadOnlyList<NfcScanHistoryRecord> Records = await this.nfcScanHistoryService.GetRecentAsync(1000, CancellationToken.None);
				if (Records.Count == 0)
					return;

				string FileName = $"nfc-history-{DateTime.UtcNow:yyyyMMddTHHmmssZ}.json";
				string Path = System.IO.Path.Combine(FileSystem.CacheDirectory, FileName);

				JsonSerializerOptions Options = new()
				{
					WriteIndented = true
				};

				string Json = JsonSerializer.Serialize(Records, Options);
				await File.WriteAllTextAsync(Path, Json);

				await Share.Default.RequestAsync(new ShareFileRequest
				{
					Title = ServiceRef.Localizer[nameof(AppResources.NfcHistoryExportAllTitle)],
					File = new ShareFile(Path)
				});
			}
			catch (Exception Ex)
			{
				ServiceRef.LogService.LogException(Ex);
			}
		}

		[RelayCommand(AllowConcurrentExecutions = false)]
		private async Task ExportHistoryCsvAsync()
		{
			try
			{
				IReadOnlyList<NfcScanHistoryRecord> Records = await this.nfcScanHistoryService.GetRecentAsync(1000, CancellationToken.None);
				if (Records.Count == 0)
					return;

				string FileName = $"nfc-history-{DateTime.UtcNow:yyyyMMddTHHmmssZ}.csv";
				string Path = System.IO.Path.Combine(FileSystem.CacheDirectory, FileName);

				StringBuilder Csv = new();
				Csv.AppendLine("DetectedAtUtc,TagId,TagType,Technologies,ExtractedUri,NdefSummary,NdefRecordCount");

				foreach (NfcScanHistoryRecord Record in Records)
				{
					int Count = Record.NdefRecords?.Length ?? 0;
					Csv.Append(this.EscapeCsv(Record.DetectedAtUtc.ToString("O")));
					Csv.Append(',');
					Csv.Append(this.EscapeCsv(Record.TagId.ToString()));
					Csv.Append(',');
					Csv.Append(this.EscapeCsv(Record.TagType));
					Csv.Append(',');
					Csv.Append(this.EscapeCsv(Record.InterfacesSummary));
					Csv.Append(',');
					Csv.Append(this.EscapeCsv(Record.ExtractedUri));
					Csv.Append(',');
					Csv.Append(this.EscapeCsv(Record.NdefSummary));
					Csv.Append(',');
					Csv.Append(Count.ToString());
					Csv.AppendLine();
				}

				await File.WriteAllTextAsync(Path, Csv.ToString(), Encoding.UTF8);

				await Share.Default.RequestAsync(new ShareFileRequest
				{
					Title = ServiceRef.Localizer[nameof(AppResources.NfcHistoryExportAllTitle)],
					File = new ShareFile(Path)
				});
			}
			catch (Exception Ex)
			{
				ServiceRef.LogService.LogException(Ex);
			}
		}

		private string EscapeCsv(string? Value)
		{
			string Text = Value ?? string.Empty;
			bool MustQuote = Text.Contains(',') || Text.Contains('"') || Text.Contains('\n') || Text.Contains('\r');
			if (!MustQuote)
				return Text;

			string Escaped = Text.Replace("\"", "\"\"");
			return "\"" + Escaped + "\"";
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
				IReadOnlyList<NfcScanHistoryRecord> Records = await this.nfcScanHistoryService.GetRecentAsync(100, CancellationToken.None);
				MainThread.BeginInvokeOnMainThread(() =>
				{
					this.HistoryItems.Clear();
					foreach (NfcScanHistoryRecord Record in Records)
					{
						this.HistoryItems.Add(NfcScanHistoryListItem.FromRecord(Record));
					}
					this.HasHistoryItems = this.HistoryItems.Count > 0;
					this.SelectedHistoryItem = null;
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
				this.ScanStateText = string.Empty;
				this.HasScanStateText = false;
				this.LastTagIdHex = string.Empty;
				this.LastTagType = string.Empty;
				this.LastDetectedAtUtc = null;
				this.LastInterfacesSummary = string.Empty;
				this.LastNdefSummary = string.Empty;
				this.HasNdefSummary = false;
				this.LastExtractedUri = string.Empty;
				this.HasExtractedUri = false;
				this.HasSafeScanLinkPreview = false;
				this.LastExtractedUriScheme = string.Empty;
				this.LastExtractedUriHost = string.Empty;
				this.LastExtractedUriWarnings.Clear();
				this.HasLastExtractedUriWarnings = false;
				this.IsLastExtractedUriDomainTrusted = false;
				this.CanTrustLastExtractedUriDomain = false;
				this.LastNdefRecords.Clear();
				this.HasNdefRecords = false;
				this.SelectedLastNdefRecord = null;
				this.HasSelectedLastNdefRecord = false;
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
			this.HasSafeScanLinkPreview = this.SafeScanEnabled && this.HasExtractedUri;
			this.UpdateUriPreview(this.LastExtractedUri);

			this.LastNdefRecords.Clear();
			if (Snapshot.NdefRecords is not null)
			{
				foreach (NfcNdefRecordSnapshot Record in Snapshot.NdefRecords)
					this.LastNdefRecords.Add(Record);
			}
			this.HasNdefRecords = this.LastNdefRecords.Count > 0;
			this.SelectedLastNdefRecord = null;
			this.HasSelectedLastNdefRecord = false;
			this.UpdateScanState();

			MainThread.BeginInvokeOnMainThread(async () =>
			{
				await this.UpdateTrustedDomainStateAsync();
			});
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

			if (value is null)
				return;

			MainThread.BeginInvokeOnMainThread(async () =>
			{
				try
				{
					NeuroAccessMaui.UI.Popups.Nfc.NfcHistoryDetailsPopup Popup = new(value);
					await ServiceRef.PopupService.PushAsync(Popup);
				}
				catch (Exception Ex)
				{
					ServiceRef.LogService.LogException(Ex);
				}
				finally
				{
					this.SelectedHistoryItem = null;
				}
			});
		}

		partial void OnSafeScanEnabledChanged(bool value)
		{
			MainThread.BeginInvokeOnMainThread(async () =>
			{
				try
				{
					await RuntimeSettings.SetAsync("NFC.SafeScan.Enabled", value);
					this.HasSafeScanLinkPreview = this.SafeScanEnabled && this.HasExtractedUri;
					this.UpdateUriPreview(this.LastExtractedUri);
					await this.UpdateTrustedDomainStateAsync();
				}
				catch (Exception Ex)
				{
					ServiceRef.LogService.LogException(Ex);
				}
			});
		}

		partial void OnSelectedLastNdefRecordChanged(NfcNdefRecordSnapshot? value)
		{
			this.HasSelectedLastNdefRecord = value is not null;
		}

		private void UpdateScanState()
		{
			if (!this.HasNdefSummary && !this.HasNdefRecords)
			{
				this.ScanStateText = ServiceRef.Localizer[nameof(AppResources.NfcScanStateNoNdef)];
				this.HasScanStateText = true;
				return;
			}

			if (this.HasNdefRecords)
			{
				this.ScanStateText = ServiceRef.Localizer[nameof(AppResources.NfcScanStateValidNdef)];
				this.HasScanStateText = true;
				return;
			}

			this.ScanStateText = ServiceRef.Localizer[nameof(AppResources.NfcScanStateNdefPresent)];
			this.HasScanStateText = true;
		}

		private void UpdateUriPreview(string UriText)
		{
			this.LastExtractedUriScheme = string.Empty;
			this.LastExtractedUriHost = string.Empty;
			this.LastExtractedUriWarnings.Clear();
			this.HasLastExtractedUriWarnings = false;
			this.IsLastExtractedUriDomainTrusted = false;
			this.CanTrustLastExtractedUriDomain = false;

			string Candidate = UriText?.Trim() ?? string.Empty;
			if (string.IsNullOrWhiteSpace(Candidate))
				return;

			if (!Uri.TryCreate(Candidate, UriKind.Absolute, out Uri? Parsed))
				return;

			this.LastExtractedUriScheme = Parsed.Scheme ?? string.Empty;
			this.LastExtractedUriHost = Parsed.Host ?? string.Empty;

			string HostLower = this.LastExtractedUriHost.Trim().ToLowerInvariant();
			if (!string.IsNullOrWhiteSpace(HostLower) && this.SafeScanEnabled)
				this.CanTrustLastExtractedUriDomain = true;

			if (!string.Equals(Parsed.Scheme, "https", StringComparison.OrdinalIgnoreCase) &&
				!string.Equals(Parsed.Scheme, "http", StringComparison.OrdinalIgnoreCase))
			{
				this.LastExtractedUriWarnings.Add(ServiceRef.Localizer[nameof(AppResources.NfcUriWarningNonHttp)]);
			}

			if (!string.IsNullOrWhiteSpace(HostLower) && HostLower.Contains("xn--", StringComparison.OrdinalIgnoreCase))
			{
				this.LastExtractedUriWarnings.Add(ServiceRef.Localizer[nameof(AppResources.NfcUriWarningPunycode)]);
			}

			if (!string.IsNullOrWhiteSpace(HostLower) && IPAddress.TryParse(HostLower, out _))
			{
				this.LastExtractedUriWarnings.Add(ServiceRef.Localizer[nameof(AppResources.NfcUriWarningIpHost)]);
			}

			if (this.IsKnownShortenerHost(HostLower))
			{
				this.LastExtractedUriWarnings.Add(ServiceRef.Localizer[nameof(AppResources.NfcUriWarningShortener)]);
			}

			this.HasLastExtractedUriWarnings = this.LastExtractedUriWarnings.Count > 0;
		}

		private bool IsKnownShortenerHost(string HostLower)
		{
			if (string.IsNullOrWhiteSpace(HostLower))
				return false;

			return HostLower == "t.co" ||
				HostLower == "bit.ly" ||
				HostLower == "tinyurl.com" ||
				HostLower == "goo.gl" ||
				HostLower == "ow.ly" ||
				HostLower == "is.gd";
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

				if (!await this.ConfirmOpenLastUriAsync())
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
		private async Task TrustLastExtractedUriDomainAsync()
		{
			try
			{
				string Host = this.LastExtractedUriHost?.Trim().ToLowerInvariant() ?? string.Empty;
				if (string.IsNullOrWhiteSpace(Host))
					return;

				if (!this.SafeScanEnabled)
					return;

				if (!await ServiceRef.UiService.DisplayAlert(
					ServiceRef.Localizer[nameof(AppResources.Question)],
					string.Format(ServiceRef.Localizer[nameof(AppResources.NfcTrustDomainQuestion)], Host),
					ServiceRef.Localizer[nameof(AppResources.Yes)],
					ServiceRef.Localizer[nameof(AppResources.Cancel)]))
				{
					return;
				}

				HashSet<string> Trusted = await this.GetTrustedDomainsAsync();
				Trusted.Add(Host);
				await this.SetTrustedDomainsAsync(Trusted);
				await this.UpdateTrustedDomainStateAsync();
			}
			catch (Exception Ex)
			{
				ServiceRef.LogService.LogException(Ex);
			}
		}

		[RelayCommand(AllowConcurrentExecutions = false)]
		private async Task CopySelectedLastNdefRecordAsync()
		{
			try
			{
				if (this.SelectedLastNdefRecord is null)
					return;

				string Candidate = this.SelectedLastNdefRecord.DisplayValue ?? string.Empty;
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
		private async Task ShareSelectedLastNdefRecordJsonAsync()
		{
			try
			{
				if (this.SelectedLastNdefRecord is null)
					return;

				string FileName = $"nfc-ndef-record-{DateTime.UtcNow:yyyyMMddTHHmmssZ}.json";
				string Path = System.IO.Path.Combine(FileSystem.CacheDirectory, FileName);

				JsonSerializerOptions Options = new()
				{
					WriteIndented = true
				};

				string Json = JsonSerializer.Serialize(this.SelectedLastNdefRecord, Options);
				await File.WriteAllTextAsync(Path, Json);

				await Share.Default.RequestAsync(new ShareFileRequest
				{
					Title = ServiceRef.Localizer[nameof(AppResources.NfcRecordExportTitle)],
					File = new ShareFile(Path)
				});
			}
			catch (Exception Ex)
			{
				ServiceRef.LogService.LogException(Ex);
			}
		}

		private async Task<bool> ConfirmOpenLastUriAsync()
		{
			try
			{
				if (!this.SafeScanEnabled)
					return true;

				if (this.IsLastExtractedUriDomainTrusted)
					return true;

				string Message = ServiceRef.Localizer[nameof(AppResources.NfcOpenUnsafeLinkPrompt)];
				if (this.HasLastExtractedUriWarnings)
				{
					string WarningList = string.Join("\n- ", this.LastExtractedUriWarnings);
					Message = Message + "\n\n- " + WarningList;
				}

				return await ServiceRef.UiService.DisplayAlert(
					ServiceRef.Localizer[nameof(AppResources.NfcPotentiallyUnsafe)],
					Message,
					ServiceRef.Localizer[nameof(AppResources.Open)],
					ServiceRef.Localizer[nameof(AppResources.Cancel)]);
			}
			catch
			{
				return true;
			}
		}

		private async Task<HashSet<string>> GetTrustedDomainsAsync()
		{
			string Json = await RuntimeSettings.GetAsync("NFC.SafeScan.TrustedDomains", "[]");
			try
			{
				string[]? Items = JsonSerializer.Deserialize<string[]>(Json);
				if (Items is null)
					return new HashSet<string>(StringComparer.OrdinalIgnoreCase);

				IEnumerable<string> Cleaned = Items
					.Where(x => !string.IsNullOrWhiteSpace(x))
					.Select(x => x.Trim().ToLowerInvariant());

				return new HashSet<string>(Cleaned, StringComparer.OrdinalIgnoreCase);
			}
			catch
			{
				return new HashSet<string>(StringComparer.OrdinalIgnoreCase);
			}
		}

		private async Task SetTrustedDomainsAsync(HashSet<string> Trusted)
		{
			string[] Array = Trusted
				.Where(x => !string.IsNullOrWhiteSpace(x))
				.Select(x => x.Trim().ToLowerInvariant())
				.Distinct(StringComparer.OrdinalIgnoreCase)
				.OrderBy(x => x, StringComparer.OrdinalIgnoreCase)
				.ToArray();

			string Json = JsonSerializer.Serialize(Array);
			await RuntimeSettings.SetAsync("NFC.SafeScan.TrustedDomains", Json);
		}

		private async Task UpdateTrustedDomainStateAsync()
		{
			try
			{
				if (!this.SafeScanEnabled)
				{
					this.IsLastExtractedUriDomainTrusted = false;
					return;
				}

				string Host = this.LastExtractedUriHost?.Trim().ToLowerInvariant() ?? string.Empty;
				if (string.IsNullOrWhiteSpace(Host))
				{
					this.IsLastExtractedUriDomainTrusted = false;
					return;
				}

				HashSet<string> Trusted = await this.GetTrustedDomainsAsync();
				this.IsLastExtractedUriDomainTrusted = Trusted.Contains(Host);
			}
			catch (Exception Ex)
			{
				ServiceRef.LogService.LogException(Ex);
				this.IsLastExtractedUriDomainTrusted = false;
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
