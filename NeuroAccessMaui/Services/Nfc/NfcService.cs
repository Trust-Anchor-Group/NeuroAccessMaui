using System.Globalization;
using System.Text;
using System.Xml;
using NeuroAccess.Nfc;
using NeuroAccess.Nfc.Records;
using NeuroAccess.Nfc.TravelDocuments;
using NeuroAccess.Nfc.TravelDocuments.DataObjects;
using NeuroAccess.Nfc.TravelDocuments.ISO19794;
using NeuroAccessMaui.Resources.Languages;
using NeuroAccessMaui.Services.Authentication;
using NeuroAccessMaui.Services.UI;
using NeuroAccessMaui.UI.Pages;
using Waher.Content.Xml;
using Waher.Events;
using Waher.Networking.Sniffers;
using Waher.Runtime.Inventory;
using Waher.Runtime.Settings;
using Waher.Security;

namespace NeuroAccessMaui.Services.Nfc
{
	/// <summary>
	/// Near-Field Communication (NFC) Service.
	/// </summary>
	[Singleton]
	public class NfcService : INfcService
	{
		private readonly IAuthenticationService authenticationService = ServiceRef.Provider.GetRequiredService<IAuthenticationService>();

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

						IsoDep.SetTimeout(300000);   // Electronic documents may introduce latency to stall spamming. Max timeout = 5 minutes.

						string Mrz = await RuntimeSettings.GetAsync("NFC.LastMrz", string.Empty);

						if (!string.IsNullOrEmpty(Mrz) &&
							MrzExtensions.ParseMrz(Mrz, out DocumentInformation? DocInfo))
						{
							StringBuilder XmlBuilder = new();
							XmlWriter XmlOutput = XmlWriter.Create(XmlBuilder, XML.WriterSettings(false, true));
							XmlWriterSniffer InMemoryXmlWriterSniffer = new(XmlOutput, BinaryPresentationMethod.Base64, "NFC");
							ISniffer[] Sniffers = new ISniffer[] { InMemoryXmlWriterSniffer }.Join(
								ServiceRef.XmppService.RemoteSniffers);

							using TravelDocumentsClient Client = new(IsoDep, DocInfo, Sniffers);

							try
							{
								Client.Information("Starting readout.");

								Client.StateChanged += (_, e) =>
								{
									// TODO: Forward state-information to UI.
									return Task.CompletedTask;
								};

								// TODO: Seed PACE authentication with ID of PREVIEW application, so that
								// Neuron can cryptographically validate the readout is not a replay of a
								// previous readout.

								switch (await Client.Authenticate())
								{
									case AuthenticateResult.Success:
										// Authentication successful.
										break;

									case AuthenticateResult.AlreadyEncrypted:
										// Already authenticated with the document.

									case AuthenticateResult.UnableToInitializePace:
										// Unable to initialize PACE.
										// (Incompatibility, missing support; suggest sending log to support for troubleshooting if problem persists.)

									case AuthenticateResult.UnableToAuthenticatePace:
										// Unable to authenticate using the selected PACE protocol.
										// (Incompatibility, missing support; suggest sending log to support for troubleshooting if problem persists.)

									case AuthenticateResult.UnableToGetBacChallenge:
										// Unable to get BAC challenge. (Probably not a valid/working travel document.)

									case AuthenticateResult.BacNotImplemented:
										// Old Travel Document requiring BAC, which is not supported.

									default:
										// TODO: Forward failure to UI.
										return;
								}

								Client.AppInfoUpdated += (_, e) =>
								{
									// TODO: Forward Application-level information to UI.
									return Task.CompletedTask;
								};

								Client.SecurityInfoUpdated += (_, e) =>
								{
									// TODO: Forward Security information to UI.
									return Task.CompletedTask;
								};

								Client.MrzUpdated += (_, e) =>
								{
									// TODO: Forward MRZ information to UI.
									// TODO: Compare with OCR MRZ to ensure consistency.
									// TODO: Check ExpiryDate to ensure passport is not expired.
									return Task.CompletedTask;
								};

								Client.BiometricEncodingFaceUpdated += (_, e) =>
								{
									// TODO: Remove. Now being output to get binaries for JPEG 2000 decoding.
									if (Client.BiometricEncodingFace is not null)
									{
										Representation? Face = Client.BiometricEncodingFace[0].BiometricDataBlock?.Record?.Representations[0];

										if (Face is not null)
										{
											Client.Warning("Face Image (type: " + Face.ImageDataType.ToString() + "):\r\n\r\n" +
												Convert.ToBase64String(Face.ImageData, Base64FormattingOptions.InsertLineBreaks));
										}
									}

									// TODO: Forward Face Biometric information to UI.
									return Task.CompletedTask;
								};

								Client.BiometricEncodingFingersUpdated += (_, e) =>
								{
									// TODO: Remove. Now being output to get binaries for JPEG 2000 decoding.
									if (Client.BiometricEncodingFingers is not null)
									{
										Representation? Fingers = Client.BiometricEncodingFingers[0].BiometricDataBlock?.Record?.Representations[0];

										if (Fingers is not null)
										{
											Client.Warning("Fingers Image (type: " + Fingers.ImageDataType.ToString() + "):\r\n\r\n" +
												Convert.ToBase64String(Fingers.ImageData, Base64FormattingOptions.InsertLineBreaks));
										}
									}

									// TODO: Forward Fingers Biometric information to UI.
									return Task.CompletedTask;
								};

								Client.BiometricEncodingIrisesUpdated += (_, e) =>
								{
									// TODO: Remove. Now being output to get binaries for JPEG 2000 decoding.
									if (Client.BiometricEncodingIrises is not null)
									{
										Representation? Irises = Client.BiometricEncodingIrises[0].BiometricDataBlock?.Record?.Representations[0];

										if (Irises is not null)
										{
											Client.Warning("Irises Image (type: " + Irises.ImageDataType.ToString() + "):\r\n\r\n" +
												Convert.ToBase64String(Irises.ImageData, Base64FormattingOptions.InsertLineBreaks));
										}
									}

									// TODO: Forward Irises Biometric information to UI.
									return Task.CompletedTask;
								};

								Client.DisplayedSignaturesUpdated += (_, e) =>
								{
									// TODO: Remove. Now being output to get binaries for JPEG 2000 decoding.
									if (Client.DisplayedSignatures?.Signatures is not null)
									{
										DisplayedSignature? Signature = Client.DisplayedSignatures?.Signatures[0];

										if (Signature is not null)
										{
											Client.Warning("Signature Image (type: JPEG or JPEG2000):\r\n\r\n" +
												Convert.ToBase64String(Signature.ImageData, Base64FormattingOptions.InsertLineBreaks));
										}
									}

									// TODO: Forward Signature information to UI.
									return Task.CompletedTask;
								};

								Client.PersonalInformationUpdated += (_, e) =>
								{
									// TODO: Forward Personal Information to UI.
									return Task.CompletedTask;
								};

								switch (await Client.ReadTravelDocument(Constants.Domains.IdDomain))
								{
									case ReadTravelDocumentResult.Success:
										// Readout successful.
										break;

									case ReadTravelDocumentResult.Lds1ApplicationNotFound:
										// LDS1 eMRTD application was not found on chip. (Not an electronic passport.)

									case ReadTravelDocumentResult.UnableToReadEfCom:
										// Unable to read EF.COM. (Try again.)

									case ReadTravelDocumentResult.UnableToParseEfCom:
										// Unable to parse EF.COM. (Incompatibility, missing support; suggest sending log to support for troubleshooting if problem persists.)
										// EF.COM used to identify services available on the chip.

									case ReadTravelDocumentResult.UnableToReadEfSod:
										// Unable to read EF.SOD. (Try again.)

									case ReadTravelDocumentResult.UnableToParseEfSod:
										// Unable to parse EF.SOD. (Incompatibility, missing support; suggest sending log to support for troubleshooting if problem persists.)
										// EF.SOD used to identify issuers of documents.

									case ReadTravelDocumentResult.UnableToReadEfDg:
										// Unable to read EF.DGx. (Try again.)

									case ReadTravelDocumentResult.UnableToParseEfDg:
										// Unable to parse EF.DGx. (Incompatibility, missing support; suggest sending log to support for troubleshooting if problem persists.)
										// TODO: Forward failure to UI.

									case ReadTravelDocumentResult.DgHashDigestInvalid:
										// Hash Digest as reported by EF.SOD does not match the has digest of the data group read.
										// (Data has been corrupted, either in transit or on the passport.)

									case ReadTravelDocumentResult.NoCertificates:
										// No certificates to validate available in EF.SOD.
										// (Not a valid Travel Document)

									case ReadTravelDocumentResult.MultipleCertificates:
										// Multiple certificates to validate available in EF.SOD were provided. Only one allowed.
										// (Not a valid Travel Document)

									case ReadTravelDocumentResult.InvalidCertificate:
										// Certificate provided in EF.SOD is not a valid certificate.
										// (Not a valid Travel Document)

									default:
										return;
								}

								Client.Information("Readout completed.");

								await InMemoryXmlWriterSniffer.FlushAsync();
								string Xml = XmlBuilder.ToString();

								// TODO: XML needs to be attached to PREVIEW ID application as an attachment
								// named `NFC.xml` to prove that the readout was performed by this application.
							}
							catch (Exception ex)
							{
								// TODO: Forward error to UI.
								Client.Exception(ex);
							}
							finally
							{
								IsoDep.CloseIfOpen();
							}
						}
					}
					else if (Interface is INdefInterface Ndef)
					{
						bool CanMakeReadOnly = await Ndef.CanMakeReadOnly();
						bool IsWritable = await Ndef.IsWritable();
						INdefRecord[] Records = await Ndef.GetMessage();

						if (Records.Length == 0 && IsWritable)
						{
							await ProgramNfc(Items => Ndef.SetMessage(Items));
							// TODO: Make read-only if able
						}
						else
						{
							foreach (INdefRecord Record in Records)
							{
								if (Record is INdefUriRecord UriRecord)
								{
									if (!string.IsNullOrEmpty(Constants.UriSchemes.GetScheme(UriRecord.Uri)))
									{
										if (!await this.authenticationService.AuthenticateUserAsync(AuthenticationPurpose.NfcTagDetected))
											return;

										if (await App.OpenUrlAsync(UriRecord.Uri))
											return;
									}
								}
							}

							// TODO: Open NFC view
						}
					}
					else if (Interface is INdefFormatableInterface NdefFormatable)
					{
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
