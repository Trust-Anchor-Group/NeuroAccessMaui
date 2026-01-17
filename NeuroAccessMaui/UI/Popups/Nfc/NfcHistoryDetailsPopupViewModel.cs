using System;
using System.IO;
using System.Text.Json;
using System.Threading.Tasks;
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
				string Candidate = this.Item.ExtractedUri?.Trim() ?? string.Empty;
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
	}
}
