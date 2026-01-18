using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.Threading;
using System.Threading.Tasks;
using CommunityToolkit.Mvvm.ComponentModel;
using CommunityToolkit.Mvvm.Input;
using EDaler.Uris;
using Microsoft.Maui.ApplicationModel.DataTransfer;
using Microsoft.Maui.Storage;
using Microsoft.Extensions.DependencyInjection;
using NeuroAccessMaui.Resources.Languages;
using NeuroAccessMaui.Services;
using NeuroAccessMaui.Services.Authentication;
using NeuroAccessMaui.Services.Nfc;
using NeuroAccessMaui.Services.Nfc.Ui;
using NeuroAccessMaui.UI;
using NeuroAccessMaui.UI.Pages.Contracts.MyContracts;
using NeuroAccessMaui.UI.Pages.Wallet;
using NeuroAccessMaui.UI.Pages.Wallet.MyTokens;
using NeuroAccessMaui.UI.Pages.Wallet.MyWallet.ObjectModels;
using NeuroAccessMaui.UI.Pages.Wallet.SendPayment;
using NeuroAccessMaui.UI.Popups.Nfc;
using Waher.Networking.XMPP.Contracts;
using Waher.Runtime.Settings;
using System.IO;
using System.Text.Json;
using System.Linq;
using System.Net;
using System.Text;
using System.Security.Cryptography;
using NeuroAccessMaui.Services.UI.Popups;
using NeuroAccessMaui.Services.UI;

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
		private DateTimeOffset? debugToolsLastTapAtUtc;
		private int debugToolsTapCount;

		private const string DebugToolsEnabledSettingsKey = "NFC.DebugTools.Enabled";
		private static readonly TimeSpan DebugToolsTapWindow = TimeSpan.FromSeconds(2);
		private const int DebugToolsTapThreshold = 7;

#if DEBUG
		private const bool DefaultDebugToolsEnabled = true;
#else
		private const bool DefaultDebugToolsEnabled = false;
#endif

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
			this.BuildWritePayloadTypeOptions();
			this.UpdateWriteComposerState();
			this.UpdateFromSnapshot(this.nfcTagSnapshotService.LastSnapshot);
		}

		/// <inheritdoc />
		public override async Task OnInitializeAsync()
		{
			await base.OnInitializeAsync();
			this.nfcTagSnapshotService.SnapshotChanged += this.NfcTagSnapshotService_SnapshotChanged;
			this.nfcScanHistoryService.HistoryChanged += this.NfcScanHistoryService_HistoryChanged;
			await this.LoadSafeScanSettingAsync();
			await this.LoadDebugToolsSettingAsync();
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
		private ObservableCollection<NfcScanHint> scanHints = new();

		[ObservableProperty]
		private bool hasScanHints;

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
		private bool isDebugToolsEnabled;

		[ObservableProperty]
		private ObservableCollection<string> trustedDomainHosts = new();

		[ObservableProperty]
		private bool hasTrustedDomainHosts;

		/// <summary>
		/// Gets a value indicating whether there are no trusted domains.
		/// </summary>
		public bool HasNoTrustedDomainHosts => !this.HasTrustedDomainHosts;

		[ObservableProperty]
		private string trustedDomainToAdd = string.Empty;

		[ObservableProperty]
		private bool hasSafeScanLinkPreview;

		[ObservableProperty]
		private bool hasNdefSummary;

		[ObservableProperty]
		private bool hasExtractedUri;

		[ObservableProperty]
		private ObservableCollection<NfcWritePayloadTypeOption> writePayloadTypeOptions = new();

		[ObservableProperty]
		private NfcWritePayloadTypeOption? selectedWritePayload;

		[ObservableProperty]
		private bool isWritePayloadUri;

		[ObservableProperty]
		private bool isWritePayloadText;

		[ObservableProperty]
		private string uriToWrite = string.Empty;

		[ObservableProperty]
		private string textToWrite = string.Empty;

		[ObservableProperty]
		private string writeSizeText = string.Empty;

		[ObservableProperty]
		private bool hasWriteSizeText;

		[ObservableProperty]
		private bool verifyAfterWrite;

		private bool isVerificationPending;
		private DateTimeOffset? verificationStartedAtUtc;
		private string verificationExpectedTagIdHex = string.Empty;
		private string verificationExpectedValue = string.Empty;
		private NfcWritePayloadKind verificationExpectedKind = NfcWritePayloadKind.Uri;

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

				if (this.SafeScanEnabled)
				{
					string? KnownAppScheme = NeuroAccessMaui.Constants.UriSchemes.GetScheme(Candidate);
					if (string.IsNullOrWhiteSpace(KnownAppScheme))
					{
						List<string> Warnings = new();
						string HostLower = string.Empty;

						if (Uri.TryCreate(Candidate, UriKind.Absolute, out Uri? Parsed))
						{
							HostLower = (Parsed.Host ?? string.Empty).Trim().ToLowerInvariant();

							if (!string.Equals(Parsed.Scheme, "https", StringComparison.OrdinalIgnoreCase) &&
								!string.Equals(Parsed.Scheme, "http", StringComparison.OrdinalIgnoreCase))
							{
								Warnings.Add(string.Format(ServiceRef.Localizer[nameof(AppResources.NfcHintUnknownScheme)], Parsed.Scheme));
							}
						}

						bool IsTrusted = false;
						if (!string.IsNullOrWhiteSpace(HostLower))
						{
							HashSet<string> Trusted = await this.GetTrustedDomainsAsync();
							IsTrusted = Trusted.Contains(HostLower);
						}

						if (!IsTrusted)
						{
							UI.Popups.Nfc.NfcOpenLinkDecisionPopupViewModel ViewModel = new(
								Candidate,
								Warnings,
								!string.IsNullOrWhiteSpace(HostLower));
							UI.Popups.Nfc.NfcOpenLinkDecisionPopup Popup = new(ViewModel);

							Services.UI.Popups.PopupOptions Options = Services.UI.Popups.PopupOptions.CreateModal();
							await ServiceRef.PopupService.PushAsync(Popup, Options);

							UI.Popups.Nfc.NfcOpenLinkDecision Decision = await ViewModel.Result;
							switch (Decision)
							{
								case UI.Popups.Nfc.NfcOpenLinkDecision.OpenOnce:
									break;
								case UI.Popups.Nfc.NfcOpenLinkDecision.TrustAndOpen:
									if (!string.IsNullOrWhiteSpace(HostLower))
										await this.TrustDomainInternalAsync(HostLower);
									break;
								case UI.Popups.Nfc.NfcOpenLinkDecision.CopyLink:
									await Clipboard.Default.SetTextAsync(Candidate);
									return;
								case UI.Popups.Nfc.NfcOpenLinkDecision.Cancel:
								case UI.Popups.Nfc.NfcOpenLinkDecision.None:
								default:
									return;
							}
						}
					}
				}

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

		[RelayCommand(AllowConcurrentExecutions = false)]
		private async Task ExportHistoryXmlAsync()
		{
			try
			{
				IReadOnlyList<NfcScanHistoryRecord> Records = await this.nfcScanHistoryService.GetRecentAsync(1000, CancellationToken.None);
				if (Records.Count == 0)
					return;

				string FileName = $"nfc-history-{DateTime.UtcNow:yyyyMMddTHHmmssZ}.xml";
				string Path = System.IO.Path.Combine(FileSystem.CacheDirectory, FileName);

				System.Xml.XmlWriterSettings Settings = new()
				{
					Indent = true,
					NewLineHandling = System.Xml.NewLineHandling.Entitize,
					OmitXmlDeclaration = false
				};

				await using (FileStream Stream = File.Create(Path))
				using (System.Xml.XmlWriter Writer = System.Xml.XmlWriter.Create(Stream, Settings))
				{
					Writer.WriteStartDocument();
					Writer.WriteStartElement("NfcHistory");
					Writer.WriteAttributeString("ExportedAtUtc", DateTime.UtcNow.ToString("O"));

					foreach (NfcScanHistoryRecord Record in Records)
						this.WriteHistoryRecordXml(Writer, Record);

					Writer.WriteEndElement();
					Writer.WriteEndDocument();
					Writer.Flush();
					await Stream.FlushAsync();
				}

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

		private void WriteHistoryRecordXml(System.Xml.XmlWriter Writer, NfcScanHistoryRecord Record)
		{
			Writer.WriteStartElement("Scan");
			Writer.WriteAttributeString("ObjectId", Record.ObjectId ?? string.Empty);
			Writer.WriteAttributeString("DetectedAtUtc", Record.DetectedAtUtc.ToString("O"));
			Writer.WriteAttributeString("TagId", Record.TagId.ToString());
			Writer.WriteAttributeString("TagType", Record.TagType ?? string.Empty);
			Writer.WriteAttributeString("InterfacesSummary", Record.InterfacesSummary ?? string.Empty);

			if (!string.IsNullOrWhiteSpace(Record.NdefSummary))
				Writer.WriteElementString("NdefSummary", Record.NdefSummary);

			if (!string.IsNullOrWhiteSpace(Record.ExtractedUri))
				Writer.WriteElementString("ExtractedUri", Record.ExtractedUri);

			if (Record.NdefRecords is not null && Record.NdefRecords.Length > 0)
			{
				Writer.WriteStartElement("NdefRecords");
				foreach (Services.Nfc.Ui.NfcNdefRecordSnapshot NdefRecord in Record.NdefRecords)
				{
					Writer.WriteStartElement("Record");
					Writer.WriteAttributeString("Index", NdefRecord.Index.ToString());
					Writer.WriteAttributeString("RecordType", NdefRecord.RecordType);
					Writer.WriteAttributeString("RecordTnf", NdefRecord.RecordTnf);
					Writer.WriteAttributeString("WellKnownType", NdefRecord.WellKnownType ?? string.Empty);
					Writer.WriteAttributeString("ContentType", NdefRecord.ContentType ?? string.Empty);
					Writer.WriteAttributeString("ExternalType", NdefRecord.ExternalType ?? string.Empty);
					Writer.WriteAttributeString("IsPayloadDerived", NdefRecord.IsPayloadDerived ? "true" : "false");
					Writer.WriteAttributeString("PayloadSizeBytes", NdefRecord.PayloadSizeBytes.ToString());

					if (!string.IsNullOrWhiteSpace(NdefRecord.Uri))
						Writer.WriteElementString("Uri", NdefRecord.Uri);
					if (!string.IsNullOrWhiteSpace(NdefRecord.Text))
						Writer.WriteElementString("Text", NdefRecord.Text);
					if (!string.IsNullOrWhiteSpace(NdefRecord.DisplayValue))
						Writer.WriteElementString("DisplayValue", NdefRecord.DisplayValue);
					if (!string.IsNullOrWhiteSpace(NdefRecord.PayloadBase64))
						Writer.WriteElementString("PayloadBase64", NdefRecord.PayloadBase64);
					if (!string.IsNullOrWhiteSpace(NdefRecord.PayloadHex))
						Writer.WriteElementString("PayloadHex", NdefRecord.PayloadHex);

					Writer.WriteEndElement();
				}
				Writer.WriteEndElement();
			}

			Writer.WriteEndElement();
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
				if (this.SelectedWritePayload is null)
				{
					this.WriteStatusText = ServiceRef.Localizer[nameof(AppResources.ErrorTitle)];
					return;
				}

				if (this.SelectedWritePayload.Kind == NfcWritePayloadKind.Uri)
				{
					if (!Uri.TryCreate(this.UriToWrite?.Trim(), UriKind.Absolute, out _))
					{
						this.WriteStatusText = ServiceRef.Localizer[nameof(AppResources.NfcWriteInvalidUri)];
						return;
					}
				}
				else
				{
					if (string.IsNullOrWhiteSpace(this.TextToWrite))
					{
						this.WriteStatusText = ServiceRef.Localizer[nameof(AppResources.NfcWriteInvalidText)];
						return;
					}
				}

				if (this.writeCancellationTokenSource is not null)
				{
					this.writeCancellationTokenSource.Cancel();
					this.writeCancellationTokenSource.Dispose();
				}

				this.writeCancellationTokenSource = new CancellationTokenSource();
				this.WriteStatusText = ServiceRef.Localizer[nameof(AppResources.NfcWritePending)];

				this.isVerificationPending = false;
				this.verificationStartedAtUtc = null;
				this.verificationExpectedTagIdHex = string.Empty;
				this.verificationExpectedValue = string.Empty;

				bool Ok;
				if (this.SelectedWritePayload.Kind == NfcWritePayloadKind.Uri)
					Ok = await this.nfcWriteService.WriteUriAsync(this.UriToWrite.Trim(), this.writeCancellationTokenSource.Token);
				else
					Ok = await this.nfcWriteService.WriteTextAsync(this.TextToWrite.Trim(), this.writeCancellationTokenSource.Token);

				if (!Ok)
				{
					this.WriteStatusText = ServiceRef.Localizer[nameof(AppResources.NfcWriteFailed)];
					return;
				}

				if (this.VerifyAfterWrite)
				{
					this.isVerificationPending = true;
					this.verificationStartedAtUtc = DateTimeOffset.UtcNow;
					this.verificationExpectedTagIdHex = this.LastTagIdHex ?? string.Empty;
					this.verificationExpectedKind = this.SelectedWritePayload.Kind;
					this.verificationExpectedValue = this.SelectedWritePayload.Kind == NfcWritePayloadKind.Uri ? this.UriToWrite.Trim() : this.TextToWrite.Trim();
					this.WriteStatusText = ServiceRef.Localizer[nameof(AppResources.NfcWriteVerificationPending)];
				}
				else
				{
					this.WriteStatusText = ServiceRef.Localizer[nameof(AppResources.NfcWriteSucceeded)];
				}
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
				this.nfcWriteService.CancelPendingWrite();
			}
			catch
			{
			}
		}

		private void NfcTagSnapshotService_SnapshotChanged(object? Sender, EventArgs e)
		{
			NfcTagSnapshot? Snapshot = this.nfcTagSnapshotService.LastSnapshot;
			this.UpdateFromSnapshot(Snapshot);
			this.TryCompleteWriteVerification(Snapshot);
		}

		private void BuildWritePayloadTypeOptions()
		{
			this.WritePayloadTypeOptions.Clear();
			this.WritePayloadTypeOptions.Add(new NfcWritePayloadTypeOption(NfcWritePayloadKind.Uri, ServiceRef.Localizer[nameof(AppResources.NfcWritePayloadTypeUri)]));
			this.WritePayloadTypeOptions.Add(new NfcWritePayloadTypeOption(NfcWritePayloadKind.Text, ServiceRef.Localizer[nameof(AppResources.NfcWritePayloadTypeText)]));

			this.SelectedWritePayload = this.WritePayloadTypeOptions.FirstOrDefault();
		}

		private void UpdateWriteComposerState()
		{
			NfcWritePayloadKind Kind = this.SelectedWritePayload?.Kind ?? NfcWritePayloadKind.Uri;
			this.IsWritePayloadUri = Kind == NfcWritePayloadKind.Uri;
			this.IsWritePayloadText = Kind == NfcWritePayloadKind.Text;
			this.UpdateWriteSizeEstimate();
		}

		private void UpdateWriteSizeEstimate()
		{
			int PayloadBytes = 0;
			int EstimatedRecordBytes = 0;

			NfcWritePayloadKind Kind = this.SelectedWritePayload?.Kind ?? NfcWritePayloadKind.Uri;
			if (Kind == NfcWritePayloadKind.Uri)
			{
				string Candidate = this.UriToWrite?.Trim() ?? string.Empty;
				if (!string.IsNullOrWhiteSpace(Candidate))
				{
					PayloadBytes = Encoding.UTF8.GetByteCount(Candidate);
					int NdefPayloadBytes = 1 + PayloadBytes;
					EstimatedRecordBytes = 4 + NdefPayloadBytes;
				}
			}
			else
			{
				string Candidate = this.TextToWrite?.Trim() ?? string.Empty;
				if (!string.IsNullOrWhiteSpace(Candidate))
				{
					PayloadBytes = Encoding.UTF8.GetByteCount(Candidate);
					int NdefPayloadBytes = 1 + 2 + PayloadBytes;
					EstimatedRecordBytes = 4 + NdefPayloadBytes;
				}
			}

			if (EstimatedRecordBytes > 0)
			{
				this.WriteSizeText = ServiceRef.Localizer[nameof(AppResources.NfcWriteSizeSummary), PayloadBytes, EstimatedRecordBytes];
				this.HasWriteSizeText = true;
			}
			else
			{
				this.WriteSizeText = string.Empty;
				this.HasWriteSizeText = false;
			}
		}

		private void TryCompleteWriteVerification(NfcTagSnapshot? Snapshot)
		{
			if (!this.isVerificationPending)
				return;

			if (Snapshot is null)
				return;

			if (this.verificationStartedAtUtc.HasValue && (DateTimeOffset.UtcNow - this.verificationStartedAtUtc.Value) > TimeSpan.FromMinutes(2))
			{
				this.isVerificationPending = false;
				return;
			}

			string ExpectedTagIdHex = this.verificationExpectedTagIdHex?.Trim() ?? string.Empty;
			if (!string.IsNullOrWhiteSpace(ExpectedTagIdHex) &&
				!string.Equals(ExpectedTagIdHex, Snapshot.TagIdHex ?? string.Empty, StringComparison.OrdinalIgnoreCase))
			{
				return;
			}

			bool Matches;
			if (this.verificationExpectedKind == NfcWritePayloadKind.Uri)
			{
				string Candidate = Snapshot.ExtractedUri?.Trim() ?? string.Empty;
				Matches = string.Equals(Candidate, this.verificationExpectedValue, StringComparison.OrdinalIgnoreCase);
			}
			else
			{
				string Expected = this.verificationExpectedValue;
				IReadOnlyList<NfcNdefRecordSnapshot>? Records = Snapshot.NdefRecords;
				Matches = Records is not null && Records.Any(r =>
					string.Equals(r.WellKnownType, "T", StringComparison.OrdinalIgnoreCase) &&
					string.Equals((r.Text ?? string.Empty).Trim(), Expected, StringComparison.Ordinal));
			}

			this.isVerificationPending = false;
			this.verificationStartedAtUtc = null;
			this.WriteStatusText = Matches
				? ServiceRef.Localizer[nameof(AppResources.NfcWriteVerificationSucceeded)]
				: ServiceRef.Localizer[nameof(AppResources.NfcWriteVerificationFailed)];
		}

		partial void OnSelectedWritePayloadChanged(NfcWritePayloadTypeOption? value)
		{
			this.UpdateWriteComposerState();
		}

		partial void OnUriToWriteChanged(string value)
		{
			this.UpdateWriteSizeEstimate();
		}

		partial void OnTextToWriteChanged(string value)
		{
			this.UpdateWriteSizeEstimate();
		}

		/// <summary>
		/// Enumerates supported NFC write payload kinds.
		/// </summary>
		public enum NfcWritePayloadKind
		{
			/// <summary>
			/// A URI record.
			/// </summary>
			Uri,

			/// <summary>
			/// A text record.
			/// </summary>
			Text
		}

		/// <summary>
		/// Represents a selectable payload type option in the NFC write composer.
		/// </summary>
		public sealed class NfcWritePayloadTypeOption
		{
			/// <summary>
			/// Initializes a new instance of the <see cref="NfcWritePayloadTypeOption"/> class.
			/// </summary>
			/// <param name="Kind">Payload kind.</param>
			/// <param name="Label">User-visible label.</param>
			public NfcWritePayloadTypeOption(NfcWritePayloadKind Kind, string Label)
			{
				this.Kind = Kind;
				this.Label = Label ?? string.Empty;
			}

			/// <summary>
			/// Gets the payload kind.
			/// </summary>
			public NfcWritePayloadKind Kind { get; }

			/// <summary>
			/// Gets the user-visible label.
			/// </summary>
			public string Label { get; }

			/// <inheritdoc/>
			public override string ToString()
			{
				return this.Label;
			}
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
				HashSet<string> TrustedDomains = await this.GetTrustedDomainsAsync();
				MainThread.BeginInvokeOnMainThread(() =>
				{
					this.HistoryItems.Clear();
					foreach (NfcScanHistoryRecord Record in Records)
					{
						this.HistoryItems.Add(NfcScanHistoryListItem.FromRecord(Record, this.SafeScanEnabled, TrustedDomains));
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
				this.IsLastExtractedUriDomainTrusted = false;
				this.CanTrustLastExtractedUriDomain = false;
				this.ScanHints.Clear();
				this.HasScanHints = false;
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
			this.UpdateScanHints();

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

		partial void OnHasTrustedDomainHostsChanged(bool value)
		{
			this.OnPropertyChanged(nameof(this.HasNoTrustedDomainHosts));
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
					await this.RefreshTrustedDomainHostsAsync();
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

		private void UpdateScanHints()
		{
			this.ScanHints.Clear();
			this.HasScanHints = false;

			bool HasNdef = this.HasNdefSummary || this.HasNdefRecords;
			bool IsIsoDep =
				string.Equals(this.LastTagType?.Trim(), "IsoDep", StringComparison.OrdinalIgnoreCase) ||
				(!string.IsNullOrWhiteSpace(this.LastInterfacesSummary) && this.LastInterfacesSummary.Contains("IsoDep", StringComparison.OrdinalIgnoreCase));

			if (!HasNdef && IsIsoDep)
			{
				this.ScanHints.Add(new NfcScanHint(
					NfcScanHintSeverity.Warning,
					ServiceRef.Localizer[nameof(AppResources.NfcHintIsoDepNoNdef)]));
			}

			string Candidate = this.LastExtractedUri?.Trim() ?? string.Empty;
			if (!string.IsNullOrWhiteSpace(Candidate) && Uri.TryCreate(Candidate, UriKind.Absolute, out Uri? Parsed))
			{
				string Scheme = Parsed.Scheme ?? string.Empty;
				string? KnownAppScheme = NeuroAccessMaui.Constants.UriSchemes.GetScheme(Candidate);

				if (!string.IsNullOrWhiteSpace(KnownAppScheme))
				{
					this.ScanHints.Add(new NfcScanHint(
						NfcScanHintSeverity.Info,
						string.Format(ServiceRef.Localizer[nameof(AppResources.NfcHintKnownAppLink)], KnownAppScheme)));
				}
				else if (!string.Equals(Scheme, "https", StringComparison.OrdinalIgnoreCase) &&
					!string.Equals(Scheme, "http", StringComparison.OrdinalIgnoreCase))
				{
					this.ScanHints.Add(new NfcScanHint(
						NfcScanHintSeverity.Warning,
						string.Format(ServiceRef.Localizer[nameof(AppResources.NfcHintUnknownScheme)], Scheme)));
				}
			}

			this.HasScanHints = this.ScanHints.Count > 0;
		}

		private void UpdateUriPreview(string UriText)
		{
			this.LastExtractedUriScheme = string.Empty;
			this.LastExtractedUriHost = string.Empty;
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

		}

		private async Task LoadSafeScanSettingAsync()
		{
			try
			{
				bool Enabled = await RuntimeSettings.GetAsync("NFC.SafeScan.Enabled", false);
				this.SafeScanEnabled = Enabled;
				await this.RefreshTrustedDomainHostsAsync();
			}
			catch (Exception Ex)
			{
				ServiceRef.LogService.LogException(Ex);
			}
		}

		private async Task LoadDebugToolsSettingAsync()
		{
			try
			{
				bool Enabled = await RuntimeSettings.GetAsync(DebugToolsEnabledSettingsKey, DefaultDebugToolsEnabled);
				this.IsDebugToolsEnabled = Enabled;
			}
			catch (Exception Ex)
			{
				ServiceRef.LogService.LogException(Ex);
			}
		}

		[RelayCommand(AllowConcurrentExecutions = false)]
		private async Task TitleTappedAsync()
		{
			try
			{
				DateTimeOffset NowUtc = DateTimeOffset.UtcNow;
				if (this.debugToolsLastTapAtUtc is null || (NowUtc - this.debugToolsLastTapAtUtc.Value) > DebugToolsTapWindow)
					this.debugToolsTapCount = 0;

				this.debugToolsTapCount++;
				this.debugToolsLastTapAtUtc = NowUtc;

				if (this.debugToolsTapCount < DebugToolsTapThreshold)
					return;

				this.debugToolsTapCount = 0;

				bool NewValue = !this.IsDebugToolsEnabled;
				await RuntimeSettings.SetAsync(DebugToolsEnabledSettingsKey, NewValue);
				this.IsDebugToolsEnabled = NewValue;

				string Message = NewValue
					? ServiceRef.Localizer[nameof(AppResources.NfcDebugToolsEnabled)]
					: ServiceRef.Localizer[nameof(AppResources.NfcDebugToolsDisabled)];

				await ServiceRef.UiService.DisplayAlert(
					ServiceRef.Localizer[nameof(AppResources.NFC)],
					Message,
					ServiceRef.Localizer[nameof(AppResources.Ok)]);
			}
			catch (Exception Ex)
			{
				ServiceRef.LogService.LogException(Ex);
			}
		}

		[RelayCommand]
		private void FakeScanUri()
		{
			try
			{
				if (!this.IsDebugToolsEnabled)
					return;

				string Timestamp = DateTimeOffset.UtcNow.ToString("O");
				string Uri = "https://example.org/neuroaccess/nfc-test?ts=" + WebUtility.UrlEncode(Timestamp);
				string TagIdHex = this.CreateFakeTagIdHex();

				List<NfcNdefRecordSnapshot> Records = new()
				{
					new NfcNdefRecordSnapshot(
						0,
						"URI",
						"WellKnown",
						"U",
						null,
						null,
						Uri,
						null,
						Encoding.UTF8.GetBytes(Uri),
						IsPayloadDerived: true),
					new NfcNdefRecordSnapshot(
						1,
						"Text",
						"WellKnown",
						"T",
						null,
						null,
						null,
						"Hello from fake NFC",
						Encoding.UTF8.GetBytes("Hello from fake NFC"),
						IsPayloadDerived: true),
				};

				NfcTagSnapshot Snapshot = new(
					TagIdHex,
					DateTimeOffset.UtcNow,
					"NDEF",
					"NfcA, NDEF",
					"URI + Text",
					Uri,
					Records);

				MainThread.BeginInvokeOnMainThread(() => this.nfcTagSnapshotService.Publish(Snapshot));
			}
			catch (Exception Ex)
			{
				ServiceRef.LogService.LogException(Ex);
			}
		}

		[RelayCommand]
		private void FakeScanUnknownScheme()
		{
			try
			{
				if (!this.IsDebugToolsEnabled)
					return;

				string Timestamp = DateTimeOffset.UtcNow.ToString("O");
				string Uri = "ftp://example.org/neuroaccess/nfc-test?ts=" + WebUtility.UrlEncode(Timestamp);
				string TagIdHex = this.CreateFakeTagIdHex();

				List<NfcNdefRecordSnapshot> Records = new()
				{
					new NfcNdefRecordSnapshot(
						0,
						"URI",
						"WellKnown",
						"U",
						null,
						null,
						Uri,
						null,
						Encoding.UTF8.GetBytes(Uri),
						IsPayloadDerived: true),
				};

				NfcTagSnapshot Snapshot = new(
					TagIdHex,
					DateTimeOffset.UtcNow,
					"NDEF",
					"NfcA, NDEF",
					"URI",
					Uri,
					Records);

				MainThread.BeginInvokeOnMainThread(() => this.nfcTagSnapshotService.Publish(Snapshot));
			}
			catch (Exception Ex)
			{
				ServiceRef.LogService.LogException(Ex);
			}
		}

		[RelayCommand]
		private void FakeScanKnownAppScheme()
		{
			try
			{
				if (!this.IsDebugToolsEnabled)
					return;

				string Timestamp = DateTimeOffset.UtcNow.ToString("O");
				string Uri = "iotsc:example@id.tagroot.io?ts=" + WebUtility.UrlEncode(Timestamp);
				string TagIdHex = this.CreateFakeTagIdHex();

				List<NfcNdefRecordSnapshot> Records = new()
				{
					new NfcNdefRecordSnapshot(
						0,
						"URI",
						"WellKnown",
						"U",
						null,
						null,
						Uri,
						null,
						Encoding.UTF8.GetBytes(Uri),
						IsPayloadDerived: true),
				};

				NfcTagSnapshot Snapshot = new(
					TagIdHex,
					DateTimeOffset.UtcNow,
					"NDEF",
					"NfcA, NDEF",
					"URI",
					Uri,
					Records);

				MainThread.BeginInvokeOnMainThread(() => this.nfcTagSnapshotService.Publish(Snapshot));
			}
			catch (Exception Ex)
			{
				ServiceRef.LogService.LogException(Ex);
			}
		}

		[RelayCommand]
		private void FakeScanNoNdef()
		{
			try
			{
				if (!this.IsDebugToolsEnabled)
					return;

				NfcTagSnapshot Snapshot = new(
					this.CreateFakeTagIdHex(),
					DateTimeOffset.UtcNow,
					"IsoDep",
					"IsoDep, NfcA",
					null,
					null,
					null);

				MainThread.BeginInvokeOnMainThread(() => this.nfcTagSnapshotService.Publish(Snapshot));
			}
			catch (Exception Ex)
			{
				ServiceRef.LogService.LogException(Ex);
			}
		}

		private string CreateFakeTagIdHex()
		{
			Span<byte> Data = stackalloc byte[7];
			RandomNumberGenerator.Fill(Data);
			return Convert.ToHexString(Data);
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

				await this.TrustDomainInternalAsync(Host);
			}
			catch (Exception Ex)
			{
				ServiceRef.LogService.LogException(Ex);
			}
		}

		private async Task TrustDomainInternalAsync(string Host)
		{
			if (string.IsNullOrWhiteSpace(Host))
				return;

			HashSet<string> Trusted = await this.GetTrustedDomainsAsync();
			Trusted.Add(Host.Trim().ToLowerInvariant());
			await this.SetTrustedDomainsAsync(Trusted);
			await this.RefreshTrustedDomainHostsAsync();
			await this.UpdateTrustedDomainStateAsync();
		}

		[RelayCommand(AllowConcurrentExecutions = false)]
		private async Task AddTrustedDomainAsync()
		{
			try
			{
				if (!this.SafeScanEnabled)
					return;

				string Input = this.TrustedDomainToAdd?.Trim() ?? string.Empty;
				if (string.IsNullOrWhiteSpace(Input))
					return;

				if (!this.TryNormalizeTrustedDomainInput(Input, out string Host))
					return;

				await this.TrustDomainInternalAsync(Host);
				this.TrustedDomainToAdd = string.Empty;
			}
			catch (Exception Ex)
			{
				ServiceRef.LogService.LogException(Ex);
			}
		}

		[RelayCommand(AllowConcurrentExecutions = false)]
		private async Task RemoveTrustedDomainAsync(string Domain)
		{
			try
			{
				if (!this.SafeScanEnabled)
					return;

				string Host = Domain?.Trim().ToLowerInvariant() ?? string.Empty;
				if (string.IsNullOrWhiteSpace(Host))
					return;

				HashSet<string> Trusted = await this.GetTrustedDomainsAsync();
				if (!Trusted.Remove(Host))
					return;

				await this.SetTrustedDomainsAsync(Trusted);
				await this.RefreshTrustedDomainHostsAsync();
				await this.UpdateTrustedDomainStateAsync();
			}
			catch (Exception Ex)
			{
				ServiceRef.LogService.LogException(Ex);
			}
		}

		private bool TryNormalizeTrustedDomainInput(string Input, out string Host)
		{
			Host = string.Empty;

			string Candidate = Input?.Trim() ?? string.Empty;
			if (string.IsNullOrWhiteSpace(Candidate))
				return false;

			if (Candidate.Contains("://", StringComparison.OrdinalIgnoreCase))
			{
				if (!Uri.TryCreate(Candidate, UriKind.Absolute, out Uri? Parsed))
					return false;

				Host = (Parsed.Host ?? string.Empty).Trim().ToLowerInvariant();
				return !string.IsNullOrWhiteSpace(Host);
			}

			if (Candidate.Contains(':'))
			{
				int ColonIndex = Candidate.IndexOf(':');
				int SlashIndex = Candidate.IndexOf('/');
				if (ColonIndex >= 0 && (SlashIndex < 0 || ColonIndex < SlashIndex))
				{
					// Looks like scheme-like input (e.g. xmpp: or iotid:). Not a domain allowlist entry.
					return false;
				}
			}

			string WithScheme = "https://" + Candidate;
			if (!Uri.TryCreate(WithScheme, UriKind.Absolute, out Uri? ParsedWithScheme))
				return false;

			Host = (ParsedWithScheme.Host ?? string.Empty).Trim().ToLowerInvariant();
			return !string.IsNullOrWhiteSpace(Host);
		}

		private async Task RefreshTrustedDomainHostsAsync()
		{
			try
			{
				if (!this.SafeScanEnabled)
				{
					MainThread.BeginInvokeOnMainThread(() =>
					{
						this.TrustedDomainHosts.Clear();
						this.HasTrustedDomainHosts = false;
					});
					return;
				}

				HashSet<string> Trusted = await this.GetTrustedDomainsAsync();
				string[] Sorted = Trusted
					.Where(x => !string.IsNullOrWhiteSpace(x))
					.Select(x => x.Trim().ToLowerInvariant())
					.Distinct(StringComparer.OrdinalIgnoreCase)
					.OrderBy(x => x, StringComparer.OrdinalIgnoreCase)
					.ToArray();

				MainThread.BeginInvokeOnMainThread(() =>
				{
					this.TrustedDomainHosts.Clear();
					foreach (string Item in Sorted)
						this.TrustedDomainHosts.Add(Item);
					this.HasTrustedDomainHosts = this.TrustedDomainHosts.Count > 0;
				});
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

		/// <summary>
		/// Opens a popup with detailed information about a decoded NDEF record.
		/// </summary>
		/// <param name="Record">Record to display.</param>
		[RelayCommand(AllowConcurrentExecutions = false)]
		private async Task OpenNdefRecordDetailsAsync(NfcNdefRecordSnapshot? Record)
		{
			try
			{
				if (Record is null)
					return;

				NfcNdefRecordDetailsPopup Popup = new(Record);
				PopupOptions Options = PopupOptions.CreateModal();
				await ServiceRef.PopupService.PushAsync(Popup, Options);
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

				string Candidate = this.LastExtractedUri?.Trim() ?? string.Empty;
				if (string.IsNullOrWhiteSpace(Candidate))
					return false;

				string? KnownAppScheme = NeuroAccessMaui.Constants.UriSchemes.GetScheme(Candidate);
				if (!string.IsNullOrWhiteSpace(KnownAppScheme))
					return true;

				if (this.IsLastExtractedUriDomainTrusted)
					return true;

				string Host = this.LastExtractedUriHost?.Trim().ToLowerInvariant() ?? string.Empty;
				bool CanTrustDomain = this.SafeScanEnabled && !string.IsNullOrWhiteSpace(Host);

				List<string> Warnings = new();
				if (Uri.TryCreate(Candidate, UriKind.Absolute, out Uri? Parsed) &&
					!string.Equals(Parsed.Scheme, "https", StringComparison.OrdinalIgnoreCase) &&
					!string.Equals(Parsed.Scheme, "http", StringComparison.OrdinalIgnoreCase))
				{
					Warnings.Add(string.Format(ServiceRef.Localizer[nameof(AppResources.NfcHintUnknownScheme)], Parsed.Scheme));
				}

				UI.Popups.Nfc.NfcOpenLinkDecisionPopupViewModel ViewModel = new(
					Candidate,
					Warnings,
					CanTrustDomain);
				UI.Popups.Nfc.NfcOpenLinkDecisionPopup Popup = new(ViewModel);

				Services.UI.Popups.PopupOptions Options = Services.UI.Popups.PopupOptions.CreateModal();
				await ServiceRef.PopupService.PushAsync(Popup, Options);

				UI.Popups.Nfc.NfcOpenLinkDecision Decision = await ViewModel.Result;
				switch (Decision)
				{
					case UI.Popups.Nfc.NfcOpenLinkDecision.OpenOnce:
						return true;
					case UI.Popups.Nfc.NfcOpenLinkDecision.TrustAndOpen:
						if (!string.IsNullOrWhiteSpace(Host))
							await this.TrustDomainInternalAsync(Host);
						return true;
					case UI.Popups.Nfc.NfcOpenLinkDecision.CopyLink:
					case UI.Popups.Nfc.NfcOpenLinkDecision.Cancel:
					case UI.Popups.Nfc.NfcOpenLinkDecision.None:
					default:
						return false;
				}
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

		/// <summary>
		/// Opens the NFC safety settings popup.
		/// </summary>
		[RelayCommand(AllowConcurrentExecutions = false)]
		private async Task OpenSafetySettingsAsync()
		{
			try
			{
				NfcSafetySettingsPopup Popup = new(this);
				PopupOptions Options = PopupOptions.CreateModal();
				await ServiceRef.PopupService.PushAsync(Popup, Options);
			}
			catch (Exception Ex)
			{
				ServiceRef.LogService.LogException(Ex);
			}
		}

		/// <summary>
		/// Closes the NFC safety settings popup.
		/// </summary>
		[RelayCommand(AllowConcurrentExecutions = false)]
		private async Task CloseSafetySettingsPopupAsync()
		{
			try
			{
				await ServiceRef.PopupService.PopAsync();
			}
			catch (Exception Ex)
			{
				ServiceRef.LogService.LogException(Ex);
			}
		}

		/// <summary>
		/// Inserts an eDaler URI into the NFC write composer.
		/// </summary>
		[RelayCommand(AllowConcurrentExecutions = false)]
		private async Task InsertEdalerUriAsync()
		{
			try
			{
				EDaler.Balance CurrentBalance = await ServiceRef.XmppService.GetEDalerBalance();
				string Candidate = "edaler:cu=" + (CurrentBalance.Currency ?? string.Empty);
				if (!EDalerUri.TryParse(Candidate, out EDalerUri Parsed))
					return;

				TaskCompletionSource<string?> UriToSend = new();
				TaskCompletionSource<string?> MessageToSend = new();
				EDalerUriNavigationArgs Args = new(Parsed, string.Empty, UriToSend, MessageToSend);
				await ServiceRef.NavigationService.GoToAsync(nameof(SendPaymentPage), Args, BackMethod.Pop);

				string? Uri = await UriToSend.Task;
				if (string.IsNullOrWhiteSpace(Uri))
					return;

				this.SelectedWritePayload = this.WritePayloadTypeOptions.FirstOrDefault(x => x.Kind == NfcWritePayloadKind.Uri) ?? this.SelectedWritePayload;
				this.UriToWrite = Uri;
			}
			catch (Exception Ex)
			{
				ServiceRef.LogService.LogException(Ex);
			}
		}

		/// <summary>
		/// Inserts a contract payload into the NFC write composer.
		/// </summary>
		[RelayCommand(AllowConcurrentExecutions = false)]
		private async Task InsertContractPayloadAsync()
		{
			try
			{
				TaskCompletionSource<Contract?> SelectedContract = new();
				MyContractsNavigationArgs Args = new(ContractsListMode.Contracts, SelectedContract);
				await ServiceRef.NavigationService.GoToAsync(nameof(MyContractsPage), Args, Services.UI.BackMethod.Pop);

				Contract? Contract = await SelectedContract.Task;
				if (Contract is null)
					return;

				StringBuilder Markdown = new();
				Markdown.Append("```");
				Markdown.AppendLine(NeuroAccessMaui.Constants.UriSchemes.IotSc);
				Contract.Serialize(Markdown, true, true, true, true, true, true, true);
				Markdown.AppendLine();
				Markdown.AppendLine("```");

				this.SelectedWritePayload = this.WritePayloadTypeOptions.FirstOrDefault(x => x.Kind == NfcWritePayloadKind.Text) ?? this.SelectedWritePayload;
				this.TextToWrite = Markdown.ToString();
			}
			catch (Exception Ex)
			{
				ServiceRef.LogService.LogException(Ex);
			}
		}

		/// <summary>
		/// Inserts a token payload into the NFC write composer.
		/// </summary>
		[RelayCommand(AllowConcurrentExecutions = false)]
		private async Task InsertTokenPayloadAsync()
		{
			try
			{
				MyTokensNavigationArgs Args = new();
				await ServiceRef.NavigationService.GoToAsync(nameof(MyTokensPage), Args, BackMethod.Pop);

				TokenItem? Selected = await Args.TokenItemProvider.Task;
				if (Selected is null)
					return;

				StringBuilder Markdown = new();
				Markdown.AppendLine("```nfeat");
				Selected.Token.Serialize(Markdown);
				Markdown.AppendLine();
				Markdown.AppendLine("```");

				this.SelectedWritePayload = this.WritePayloadTypeOptions.FirstOrDefault(x => x.Kind == NfcWritePayloadKind.Text) ?? this.SelectedWritePayload;
				this.TextToWrite = Markdown.ToString();
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
		public static NfcScanHistoryListItem FromRecord(NfcScanHistoryRecord Record, bool SafeScanEnabled, ISet<string>? TrustedDomains)
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
