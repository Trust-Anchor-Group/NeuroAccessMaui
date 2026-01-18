using System;
using System.IO;
using System.Text.Json;
using System.Threading.Tasks;
using System.Xml;
using CommunityToolkit.Mvvm.ComponentModel;
using CommunityToolkit.Mvvm.Input;
using Microsoft.Maui.ApplicationModel.DataTransfer;
using Microsoft.Maui.Storage;
using NeuroAccessMaui.Resources.Languages;
using NeuroAccessMaui.Services;
using NeuroAccessMaui.UI.Pages.Main.Nfc;

namespace NeuroAccessMaui.UI.Popups.Nfc
{
	/// <summary>
	/// View model for <see cref="NfcHistoryDetailsPopup"/>.
	/// </summary>
	public partial class NfcHistoryDetailsPopupViewModel : BasePopupViewModel
	{
		/// <summary>
		/// Initializes a new instance of the <see cref="NfcHistoryDetailsPopupViewModel"/> class.
		/// </summary>
		/// <param name="Item">History item to show.</param>
		/// <exception cref="ArgumentNullException">If <paramref name="Item"/> is null.</exception>
		public NfcHistoryDetailsPopupViewModel(NfcScanHistoryListItem Item)
		{
			this.Item = Item ?? throw new ArgumentNullException(nameof(Item));
			this.HasUri = !string.IsNullOrWhiteSpace(this.Item.ExtractedUri);
			this.HasNdefRecords = this.Item.NdefRecords is not null && this.Item.NdefRecords.Count > 0;
		}

		/// <summary>
		/// Gets the history item being displayed.
		/// </summary>
		[ObservableProperty]
		private NfcScanHistoryListItem item;

		/// <summary>
		/// Gets a value indicating whether the item has a usable URI.
		/// </summary>
		[ObservableProperty]
		private bool hasUri;

		/// <summary>
		/// Gets a value indicating whether the item has decoded NDEF records.
		/// </summary>
		[ObservableProperty]
		private bool hasNdefRecords;

		[RelayCommand(AllowConcurrentExecutions = false)]
		private async Task CloseAsync()
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

		[RelayCommand(AllowConcurrentExecutions = false)]
		private async Task CopyUriAsync()
		{
			try
			{
				await this.CopyValueAsync(this.Item.ExtractedUri);
			}
			catch (Exception Ex)
			{
				ServiceRef.LogService.LogException(Ex);
			}
		}

		/// <summary>
		/// Copies a value to the clipboard.
		/// </summary>
		/// <param name="Value">Value to copy.</param>
		[RelayCommand(AllowConcurrentExecutions = false)]
		private async Task CopyValueAsync(string? Value)
		{
			try
			{
				string Candidate = Value?.Trim() ?? string.Empty;
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
		private async Task OpenUriAsync()
		{
			try
			{
				string Candidate = this.Item.ExtractedUri?.Trim() ?? string.Empty;
				if (string.IsNullOrWhiteSpace(Candidate))
					return;

				if (!await ServiceRef.AuthenticationService.AuthenticateUserAsync(AuthenticationPurpose.NfcTagDetected))
					return;

				await App.OpenUrlAsync(Candidate);
			}
			catch (Exception Ex)
			{
				ServiceRef.LogService.LogException(Ex);
			}
		}

		[RelayCommand(AllowConcurrentExecutions = false)]
		private async Task ExportJsonAsync()
		{
			try
			{
				JsonSerializerOptions Options = new()
				{
					WriteIndented = true
				};

				string FileName = $"nfc-scan-{DateTime.UtcNow:yyyyMMddTHHmmssZ}.json";
				string Path = System.IO.Path.Combine(FileSystem.CacheDirectory, FileName);

				string Json = JsonSerializer.Serialize(this.Item, Options);
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
		private async Task ExportXmlAsync()
		{
			try
			{
				string FileName = $"nfc-scan-{DateTime.UtcNow:yyyyMMddTHHmmssZ}.xml";
				string Path = System.IO.Path.Combine(FileSystem.CacheDirectory, FileName);

				XmlWriterSettings Settings = new()
				{
					Indent = true,
					NewLineHandling = NewLineHandling.Entitize,
					OmitXmlDeclaration = false
				};

				await using (FileStream Stream = File.Create(Path))
				using (XmlWriter Writer = XmlWriter.Create(Stream, Settings))
				{
					Writer.WriteStartDocument();
					Writer.WriteStartElement("NfcScan");
					Writer.WriteAttributeString("ExportedAtUtc", DateTime.UtcNow.ToString("O"));
					this.WriteHistoryItemXml(Writer, this.Item);
					Writer.WriteEndElement();
					Writer.WriteEndDocument();
					Writer.Flush();
					await Stream.FlushAsync();
				}

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

		private void WriteHistoryItemXml(XmlWriter Writer, NfcScanHistoryListItem Item)
		{
			Writer.WriteStartElement("Item");
			Writer.WriteAttributeString("DetectedAtUtc", Item.DetectedAtUtc.ToString("O"));
			Writer.WriteAttributeString("TagIdHex", Item.TagIdHex ?? string.Empty);
			Writer.WriteAttributeString("TagType", Item.TagType ?? string.Empty);
			Writer.WriteAttributeString("ObjectId", Item.ObjectId ?? string.Empty);
			Writer.WriteAttributeString("InterfacesSummary", Item.InterfacesSummary ?? string.Empty);

			if (!string.IsNullOrWhiteSpace(Item.NdefSummary))
				Writer.WriteElementString("NdefSummary", Item.NdefSummary);

			if (!string.IsNullOrWhiteSpace(Item.ExtractedUri))
				Writer.WriteElementString("ExtractedUri", Item.ExtractedUri);

			if (Item.NdefRecords is not null && Item.NdefRecords.Count > 0)
			{
				Writer.WriteStartElement("NdefRecords");
				foreach (Services.Nfc.Ui.NfcNdefRecordSnapshot Record in Item.NdefRecords)
				{
					Writer.WriteStartElement("Record");
					Writer.WriteAttributeString("Index", Record.Index.ToString());
					Writer.WriteAttributeString("RecordType", Record.RecordType);
					Writer.WriteAttributeString("RecordTnf", Record.RecordTnf);
					Writer.WriteAttributeString("WellKnownType", Record.WellKnownType ?? string.Empty);
					Writer.WriteAttributeString("ContentType", Record.ContentType ?? string.Empty);
					Writer.WriteAttributeString("ExternalType", Record.ExternalType ?? string.Empty);
					Writer.WriteAttributeString("IsPayloadDerived", Record.IsPayloadDerived ? "true" : "false");
					Writer.WriteAttributeString("PayloadSizeBytes", Record.PayloadSizeBytes.ToString());

					if (!string.IsNullOrWhiteSpace(Record.Uri))
						Writer.WriteElementString("Uri", Record.Uri);
					if (!string.IsNullOrWhiteSpace(Record.Text))
						Writer.WriteElementString("Text", Record.Text);
					if (!string.IsNullOrWhiteSpace(Record.DisplayValue))
						Writer.WriteElementString("DisplayValue", Record.DisplayValue);
					if (!string.IsNullOrWhiteSpace(Record.PayloadBase64))
						Writer.WriteElementString("PayloadBase64", Record.PayloadBase64);
					if (!string.IsNullOrWhiteSpace(Record.PayloadHex))
						Writer.WriteElementString("PayloadHex", Record.PayloadHex);

					Writer.WriteEndElement();
				}
				Writer.WriteEndElement();
			}

			Writer.WriteEndElement();
		}
	}
}
