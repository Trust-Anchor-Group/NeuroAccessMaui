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
using NeuroAccess.Nfc.Extensions.PACE;
using System.Diagnostics.CodeAnalysis;
using Waher.Networking.XMPP.Provisioning.Events;

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

						string Mrz = await RuntimeSettings.GetAsync("NFC.LastMrz", string.Empty);

						if (!string.IsNullOrEmpty(Mrz) &&
							TravelDocuments.ParseMrz(Mrz, out DocumentInformation? DocInfo))
						{
							try
							{
								if (DocInfo is null)
								{
									IsoDep.Error("Unable to parse MRZ information.");
									return;
								}

								// §4.2 1. https://www2023.icao.int/publications/Documents/9303_p11_cons_en.pdf

								byte[]? Data = await IsoDep.DownloadFile(TravelDocuments.ElementaryFiles.CardAccess);

								if (Data is not null &&
									TravelDocuments.TryDecodeDER(Data, out object? CardAccess) &&
									TryFindPaceProtocol(IsoDep, CardAccess, out IPaceProtocol? Protocol))
								{
									// Optional: Read EF.DIR	§4.2 2. https://www2023.icao.int/publications/Documents/9303_p11_cons_en.pdf

									// PACE
									// §4.2 3. https://www2023.icao.int/publications/Documents/9303_p11_cons_en.pdf

									if (IsoDep.HasSniffers)
										IsoDep.Information("PACE protocol " + Protocol.GetType().Name.Replace('_', '-') + " selected.");

									if (!await TravelDocuments.InitializePACE(IsoDep, Protocol))
									{
										IsoDep.Error("Unable to initialize PACE protocol.");
										return;
									}
									else if (Protocol is PaceEecProtocol EecProtocol)
										IsoDep.Information("PACE protocol initialized (" + EecProtocol.Curve?.CurveName + ").");
									else
										IsoDep.Information("PACE protocol initialized.");

									byte[]? Nonce = await IsoDep.GetPaceNonce();

									if (Nonce is null)
									{
										IsoDep.Error("Unable to get PACE nonce.");
										return;
									}

									byte[] LocalPublicKey = Protocol.CreateNewEphemeralKey();
									byte[]? RemotePublicKey = await IsoDep.GetPaceRemotePublicKey(LocalPublicKey);

									if (RemotePublicKey is null)
									{
										IsoDep.Error("Unable to get PACE remote public key.");
										return;
									}

									// TODO
								}
								else
								{
									// BAC
									// §4.2 4. https://www2023.icao.int/publications/Documents/9303_p11_cons_en.pdf

									IsoDep.Information("Attempting legacy BAC protocol.");

									// §4.3, §D.3, https://www.icao.int/publications/Documents/9303_p11_cons_en.pdf

									byte[]? Challenge = await IsoDep.GetBacChallenge();

									if (Challenge is null)
									{
										IsoDep.Error("Unable to get BAC challenge.");
										return;
									}

									byte[] ChallengeResponse = DocInfo.CalcChallengeResponse3DES(Challenge);
									byte[]? Response = await IsoDep.ExternalBacAuthenticate(ChallengeResponse);

									// TODO
								}
							}
							catch (Exception ex)
							{
								IsoDep.Exception(ex);
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

		public static bool TryFindPaceProtocol(IIsoDepInterface IsoDep, object? CardAccess,
			[NotNullWhen(true)] out IPaceProtocol? Protocol)
		{
			/*
			 * Contents of EF.CardAccess:
			 * 
			 * SecurityInfos ::= SET of SecurityInfo 
			 * 
			 * SecurityInfo ::= SEQUENCE 
			 * {
			 *		protocol		OBJECT IDENTIFIER, 
			 *		requiredData	ANY DEFINED BY protocol, 
			 *		optionalData	ANY DEFINED BY protocol OPTIONAL 
			 * }
			*/

			Protocol = null;

			if (CardAccess is not Array SecurityInfos)
				return false;

			IPaceProtocol? Best = null;
			IPaceProtocol? Current;

			foreach (object Item in SecurityInfos)
			{
				if (Item is not Array SecurityInfo ||
					SecurityInfo.Length == 0 ||
					SecurityInfo.GetValue(0) is not string Oid)
				{
					continue;
				}

				Current = Types.FindBest<IPaceProtocol, string>(Oid);
				if (Current is null)
				{
					if (IsoDep.HasSniffers)
						IsoDep.Information("OID " + Oid + " lacks implemented support.");

					continue;
				}

				if (IsoDep.HasSniffers)
					IsoDep.Information("OID " + Oid + " (" + Current.GetType().Name.Replace('_', '-') + ") supported.");

				if (!Current.Configure(SecurityInfo))
					continue;

				if (Best is null ||
					Current.SecurityStrength > Best.SecurityStrength ||
					(Current.SecurityStrength == Best.SecurityStrength &&
					Current.ChipAuthenticationMapping && !Best.ChipAuthenticationMapping))
				{
					Best = Current;
				}
			}

			Protocol = Best;

			return Protocol is not null;
		}

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
