using NeuroAccess.Nfc;
using NeuroAccess.Nfc.Extensions;
using NeuroAccess.Nfc.Records;
using NeuroAccessMaui.UI.Pages;
using NeuroAccessMaui.Resources.Languages;
using NeuroAccessMaui.Services.UI;
using Waher.Runtime.Inventory;
using Waher.Runtime.Settings;
using Waher.Security;
using System.Globalization;
using NeuroAccessMaui.Services.Authentication;
using NeuroAccessMaui.Services.Nfc.Ui;
using System.Text;

namespace NeuroAccessMaui.Services.Nfc
{
	/// <summary>
	/// Near-Field Communication (NFC) Service.
	/// </summary>
	[Singleton]
	public class NfcService : INfcService
	{
		private readonly IAuthenticationService authenticationService = ServiceRef.Provider.GetRequiredService<IAuthenticationService>();
		private readonly INfcScanService nfcScanService = ServiceRef.Provider.GetRequiredService<INfcScanService>();
		private readonly INfcTagSnapshotService nfcTagSnapshotService = ServiceRef.Provider.GetRequiredService<INfcTagSnapshotService>();
		private readonly INfcWriteService nfcWriteService = ServiceRef.Provider.GetRequiredService<INfcWriteService>();

		/// <summary>
		/// Near-Field Communication (NFC) Service.
		/// </summary>
		public NfcService()
			: base()
		{
		}

		/// <summary>
		/// Method called when a new NFC Tag has been detected.
		/// </summary>
		/// <param name="Tag">NFC Tag</param>
		public async Task TagDetected(INfcTag Tag)
		{
			try
			{
				string TagId = Hashes.BinaryToString(Tag.ID).ToUpper(CultureInfo.InvariantCulture);
				this.PublishSnapshot(Tag, TagId);
				NfcTagReference TagReference = await NfcTagReference.FindByTagId(TagId);

				foreach (INfcInterface Interface in Tag.Interfaces)
				{
					// Some NFC devices allow all interfaces to be open, others not. So when browsing interfaces we must assure only
					// one interface is open at a time.
					foreach (INfcInterface Interface2 in Tag.Interfaces)
					{
						if (Interface2 == Interface)
							await Interface2.OpenIfClosed();
						else
							Interface2.CloseIfOpen();
					}

					if (Interface is IIsoDepInterface IsoDep)
					{
						// ISO 14443-4

						string Mrz = await RuntimeSettings.GetAsync("NFC.LastMrz", string.Empty);
						bool ScanRequested = await RuntimeSettings.GetAsync("NFC.Passport.ScanRequested", false);

						if (!string.IsNullOrEmpty(Mrz) &&
							BasicAccessControl.ParseMrz(Mrz, out DocumentInformation? DocInfo))
						{
							// §4.3, §D.3, https://www.icao.int/publications/Documents/9303_p11_cons_en.pdf

							byte[]? Challenge = await IsoDep.GetChallenge();
							if (Challenge is not null && DocInfo is not null)
							{
								byte[] ChallengeResponse = DocInfo.CalcChallengeResponse(Challenge);
								byte[]? Response = await IsoDep.ExternalAuthenticate(ChallengeResponse);
								if (Response is not null)
								{
									await RuntimeSettings.SetAsync("NFC.Passport.BacOk", true);
									await RuntimeSettings.SetAsync("NFC.Passport.LastBacAt", DateTime.UtcNow.ToString("O"));
								}
								else
								{
									await RuntimeSettings.SetAsync("NFC.Passport.BacOk", false);
								}
							}
						}
						else if (ScanRequested)
						{
							// A user-initiated passport scan is active, but we cannot attempt BAC due to missing/invalid MRZ.
							await RuntimeSettings.SetAsync("NFC.Passport.BacOk", false);
						}
					}
					else if (Interface is INdefInterface Ndef)
					{
						bool CanMakeReadOnly = await Ndef.CanMakeReadOnly();
						bool IsWritable = await Ndef.IsWritable();
						INdefRecord[] Records = await Ndef.GetMessage();

						// User-initiated write takes precedence over auto-engraving or auto-open.
						if (this.nfcWriteService.HasPendingWrite)
						{
							bool Handled = await this.nfcWriteService.TryHandleWriteAsync(Items => Ndef.SetMessage(Items));
							if (Handled)
								return;
						}

						if (Records.Length == 0 && IsWritable)
						{
							await ProgramNfc(Items => Ndef.SetMessage(Items));
							// TODO: Make read-only if able
						}
						else
						{
							List<string> RecordTypes = [];
							string? ExtractedUri = null;
							List<NfcNdefRecordSnapshot> NdefRecords = [];
							bool ScanHandled = false;

							for (int i = 0; i < Records.Length; i++)
							{
								INdefRecord Record = Records[i];
								string RecordTnf = Record.Type.ToString();

								if (Record is INdefUriRecord UriRecord)
								{
									RecordTypes.Add("URI");
									ExtractedUri ??= UriRecord.Uri;
									NdefRecords.Add(new NfcNdefRecordSnapshot(
										i,
										"URI",
										RecordTnf,
										WellKnownType: "U",
										ContentType: null,
										ExternalType: null,
										Uri: UriRecord.Uri,
										Text: null,
										Payload: Encoding.UTF8.GetBytes(UriRecord.Uri ?? string.Empty),
										IsPayloadDerived: true));

									if (!ScanHandled)
										ScanHandled = await this.TryHandleNfcUriAsync(UriRecord.Uri);
								}
								else if (Record is INdefWellKnownTypeRecord WellKnown &&
									string.Equals(WellKnown.WellKnownType, "T", StringComparison.OrdinalIgnoreCase))
								{
									string? Text = TryDecodeNdefTextRecord(WellKnown.Data);
									RecordTypes.Add("Text");

									if (!string.IsNullOrEmpty(Text) && System.Uri.TryCreate(Text, UriKind.Absolute, out _))
										ExtractedUri ??= Text;

									NdefRecords.Add(new NfcNdefRecordSnapshot(
										i,
										"Text",
										RecordTnf,
										WellKnownType: "T",
										ContentType: null,
										ExternalType: null,
										Uri: null,
										Text: Text,
										Payload: WellKnown.Data,
										IsPayloadDerived: false));

									if (!string.IsNullOrEmpty(Text) && !ScanHandled)
										ScanHandled = await this.TryHandleNfcUriAsync(Text);
								}
								else if (Record is INdefMimeTypeRecord Mime)
								{
									RecordTypes.Add("MIME");
									NdefRecords.Add(new NfcNdefRecordSnapshot(
										i,
										"MIME",
										RecordTnf,
										WellKnownType: null,
										ContentType: Mime.ContentType,
										ExternalType: null,
										Uri: null,
										Text: TryDecodeUtf8(Mime.Data),
										Payload: Mime.Data,
										IsPayloadDerived: false));
								}
								else if (Record is INdefExternalTypeRecord External)
								{
									RecordTypes.Add("External");
									NdefRecords.Add(new NfcNdefRecordSnapshot(
										i,
										"External",
										RecordTnf,
										WellKnownType: null,
										ContentType: null,
										ExternalType: External.ExternalType,
										Uri: null,
										Text: TryDecodeUtf8(External.Data),
										Payload: External.Data,
										IsPayloadDerived: false));
								}
								else if (Record is INdefWellKnownTypeRecord OtherWellKnown)
								{
									RecordTypes.Add("WellKnown");
									NdefRecords.Add(new NfcNdefRecordSnapshot(
										i,
										"WellKnown",
										RecordTnf,
										WellKnownType: OtherWellKnown.WellKnownType,
										ContentType: null,
										ExternalType: null,
										Uri: null,
										Text: TryDecodeUtf8(OtherWellKnown.Data),
										Payload: OtherWellKnown.Data,
										IsPayloadDerived: false));
								}
								else
								{
									RecordTypes.Add(Record.GetType().Name);
									NdefRecords.Add(new NfcNdefRecordSnapshot(
										i,
										Record.GetType().Name,
										RecordTnf,
										WellKnownType: null,
										ContentType: null,
										ExternalType: null,
										Uri: null,
										Text: null,
										Payload: null,
										IsPayloadDerived: false));
								}
							}

							string? NdefSummary = RecordTypes.Count > 0 ? string.Join(", ", RecordTypes) : "";
							this.nfcTagSnapshotService.UpdateNdefDetails(TagId, NdefSummary, ExtractedUri, NdefRecords);

							if (ScanHandled)
								return;

							// TODO: Open NFC view
						}
					}
					else if (Interface is INdefFormatableInterface NdefFormatable)
					{
						// User-initiated write takes precedence.
						if (this.nfcWriteService.HasPendingWrite)
						{
							bool Handled = await this.nfcWriteService.TryHandleWriteAsync(Items => NdefFormatable.Format(false, Items));
							if (Handled)
								return;
						}

						await ProgramNfc(Items => NdefFormatable.Format(false, Items));
						// TODO: Make read-only if able
					}
					else if (Interface is INfcAInterface NfcA)
					{
						byte[] Atqa = await NfcA.GetAtqa();
						short Sqk = await NfcA.GetSqk();

						// TODO
					}
					else if (Interface is INfcBInterface NfcB)
					{
						byte[] ApplicationData = await NfcB.GetApplicationData();
						byte[] ProtocolInfo = await NfcB.GetProtocolInfo();

						// TODO
					}
					else if (Interface is INfcFInterface NfcF)
					{
						byte[] Manufacturer = await NfcF.GetManufacturer();
						byte[] SystemCode = await NfcF.GetSystemCode();

						// TODO
					}
					else if (Interface is INfcVInterface NfcV)
					{
						sbyte DsfId = await NfcV.GetDsfId();
						short ResponseFlags = await NfcV.GetResponseFlags();

						// TODO
					}
					else if (Interface is INfcBarcodeInterface Barcode)
					{
						byte[] Data = await Barcode.ReadAllData();

						// TODO
					}
					else if (Interface is IMifareUltralightInterface MifareUltralight)
					{
						byte[] Data = await MifareUltralight.ReadAllData();

						// TODO
					}
					else if (Interface is IMifareClassicInterface MifareClassic)
					{
						byte[] Data = await MifareClassic.ReadAllData();

						// TODO
					}
				}
			}
			catch (Exception ex)
			{
				await ServiceRef.UiService.DisplayException(ex);
			}
		}

		private void PublishSnapshot(INfcTag Tag, string TagId)
		{
			try
			{
				List<string> InterfaceNames = [];
				bool HasNdef = false;
				bool HasIsoDep = false;

				foreach (INfcInterface Interface in Tag.Interfaces)
				{
					if (Interface is IIsoDepInterface)
						InterfaceNames.Add("ISO-DEP (ISO 14443-4)");
					else if (Interface is INdefInterface)
						InterfaceNames.Add("NDEF");
					else if (Interface is INdefFormatableInterface)
						InterfaceNames.Add("NDEF (formatable)");
					else if (Interface is INfcAInterface)
						InterfaceNames.Add("NFC-A (ISO 14443-3A)");
					else if (Interface is INfcBInterface)
						InterfaceNames.Add("NFC-B (ISO 14443-3B)");
					else if (Interface is INfcFInterface)
						InterfaceNames.Add("NFC-F (FeliCa)");
					else if (Interface is INfcVInterface)
						InterfaceNames.Add("NFC-V (ISO 15693)");
					else
						InterfaceNames.Add(Interface.GetType().Name);

					HasNdef |= Interface is INdefInterface || Interface is INdefFormatableInterface;
					HasIsoDep |= Interface is IIsoDepInterface;
				}

				string TagType = HasIsoDep ? "ISO-DEP" : (HasNdef ? "NDEF" : "Unknown");
				string InterfacesSummary = string.Join(", ", InterfaceNames);

				NfcTagSnapshot Snapshot = new(
					TagId,
					DateTimeOffset.UtcNow,
					TagType,
					InterfacesSummary,
					NdefSummary: null,
					ExtractedUri: null);

				this.nfcTagSnapshotService.Publish(Snapshot);
			}
			catch
			{
			}
		}

		private async Task<bool> TryHandleNfcUriAsync(string Uri)
		{
			string Candidate = Uri?.Trim() ?? string.Empty;
			if (string.IsNullOrEmpty(Candidate))
				return false;

			if (string.IsNullOrEmpty(Constants.UriSchemes.GetScheme(Candidate)))
				return false;

			// If a user-initiated scan is active (e.g., from the QR scan page), prefer completing that flow
			// rather than navigating immediately.
			if (this.nfcScanService.TryHandleDetectedUri(Candidate))
				return true;

			bool SafeScanEnabled = await RuntimeSettings.GetAsync("NFC.SafeScan.Enabled", false);
			if (SafeScanEnabled)
			{
				if (!System.Uri.TryCreate(Candidate, UriKind.Absolute, out System.Uri? Parsed))
					return false;

				string Host = Parsed.Host?.Trim().ToLowerInvariant() ?? string.Empty;
				if (string.IsNullOrWhiteSpace(Host))
					return false;

				string TrustedJson = await RuntimeSettings.GetAsync("NFC.SafeScan.TrustedDomains", "[]");
				try
				{
					string[]? Trusted = System.Text.Json.JsonSerializer.Deserialize<string[]>(TrustedJson);
					if (Trusted is null || Array.IndexOf(Trusted, Host) < 0)
						return false;
				}
				catch
				{
					return false;
				}
			}

			// Default behavior: authenticate and open.
			MainThread.BeginInvokeOnMainThread(async () =>
			{
				try
				{
					if (!await this.authenticationService.AuthenticateUserAsync(AuthenticationPurpose.NfcTagDetected))
						return;

					await App.OpenUrlAsync(Candidate);
				}
				catch (Exception Ex)
				{
					ServiceRef.LogService.LogException(Ex);
				}
			});

			return true;
		}

		private static string? TryDecodeUtf8(byte[] Data)
		{
			if (Data is null || Data.Length == 0)
				return null;

			try
			{
				string Text = Encoding.UTF8.GetString(Data).Trim();
				return string.IsNullOrEmpty(Text) ? null : Text;
			}
			catch
			{
				return null;
			}
		}

		private static string? TryDecodeNdefTextRecord(byte[] Data)
		{
			if (Data is null || Data.Length < 2)
				return null;

			byte Status = Data[0];
			int LanguageCodeLength = Status & 0x3F;
			bool IsUtf16 = (Status & 0x80) != 0;

			int TextIndex = 1 + LanguageCodeLength;
			if (TextIndex >= Data.Length)
				return null;

			System.Text.Encoding Encoding = IsUtf16 ? System.Text.Encoding.Unicode : System.Text.Encoding.UTF8;
			return Encoding.GetString(Data, TextIndex, Data.Length - TextIndex).Trim();
		}

		public delegate Task<bool> WriteItems(object[] Items);

		/// <summary>
		/// Programs an NFC tag.
		/// </summary>
		/// <param name="Callback">Callback method that performs actual writing.</param>
		/// <returns>If process was successful or not.</returns>
		public static async Task<bool> ProgramNfc(WriteItems Callback)
		{
			INavigationService Nav = App.Instantiate<INavigationService>();
			if (Nav.CurrentPage is BaseContentPage ContentPage &&
				ContentPage.ViewModel<BaseViewModel>() is ILinkableView LinkableView &&
				LinkableView.IsLinkable)
			{
				string? Link = LinkableView.Link;
				string Title = await LinkableView.Title;

				List<object> Items = [];

				if (LinkableView.EncodeAppLinks)
					Items.Add(Title);

				if (!string.IsNullOrEmpty(Link))
					Items.Add(new Uri(Link));

				if (LinkableView.EncodeAppLinks)
				{
					Items.Add(new Uri(Constants.References.AndroidApp));
					Items.Add(new Uri(Constants.References.IPhoneApp));
				}

				if (LinkableView.HasMedia)
					Items.Add(new KeyValuePair<byte[], string>(LinkableView.Media!, LinkableView.MediaContentType!));

				if (!await ServiceRef.Provider.GetRequiredService<IAuthenticationService>().AuthenticateUserAsync(AuthenticationPurpose.NfcTagDetected))
					return false;

				bool Ok = await Callback([.. Items]);

				if (!Ok && Items[^1] is KeyValuePair<byte[], string>)
				{
					Items.RemoveAt(Items.Count - 1);
					Ok = await Callback([.. Items]);
				}

				if (!Ok)
				{
					while (Items.Count > 2)
						Items.RemoveAt(2);

					Ok = await Callback([.. Items]);

					if (!Ok)
					{
						Items.RemoveAt(0);
						Ok = await Callback([.. Items]);
					}
				}

				if (Ok)
				{
					await ServiceRef.UiService.DisplayAlert(
						ServiceRef.Localizer[nameof(AppResources.SuccessTitle)],
						ServiceRef.Localizer[nameof(AppResources.TagEngraved), Title]);

					return true;
				}
				else
				{
					await ServiceRef.UiService.DisplayAlert(
						ServiceRef.Localizer[nameof(AppResources.ErrorTitle)],
						ServiceRef.Localizer[nameof(AppResources.TagNotEngraved), Title]);

					return false;
				}
			}
			else
				return false;
		}
	}
}
