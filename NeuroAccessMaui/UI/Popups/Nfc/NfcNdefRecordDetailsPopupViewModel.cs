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
using NeuroAccessMaui.Services.Nfc.Ui;

namespace NeuroAccessMaui.UI.Popups.Nfc
{
	/// <summary>
	/// View model for <see cref="NfcNdefRecordDetailsPopup"/>.
	/// </summary>
	public partial class NfcNdefRecordDetailsPopupViewModel : BasePopupViewModel
	{
		/// <summary>
		/// Initializes a new instance of the <see cref="NfcNdefRecordDetailsPopupViewModel"/> class.
		/// </summary>
		/// <param name="Record">Record to display.</param>
		/// <exception cref="ArgumentNullException">If <paramref name="Record"/> is null.</exception>
		public NfcNdefRecordDetailsPopupViewModel(NfcNdefRecordSnapshot Record)
		{
			this.Record = Record ?? throw new ArgumentNullException(nameof(Record));
		}

		/// <summary>
		/// Gets the record being displayed.
		/// </summary>
		[ObservableProperty]
		private NfcNdefRecordSnapshot record;

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
		private async Task ExportJsonAsync()
		{
			try
			{
				string FileName = $"nfc-ndef-record-{DateTime.UtcNow:yyyyMMddTHHmmssZ}.json";
				string Path = System.IO.Path.Combine(FileSystem.CacheDirectory, FileName);

				JsonSerializerOptions Options = new()
				{
					WriteIndented = true
				};

				string Json = JsonSerializer.Serialize(this.Record, Options);
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
	}
}
