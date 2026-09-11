using System.Globalization;
using NeuroAccess.Nfc;
using NeuroAccess.Nfc.Records;
using NeuroAccessMaui.Resources.Languages;
using NeuroAccessMaui.Services.Authentication;
using NeuroAccessMaui.Services.TravelDocuments;
using NeuroAccessMaui.Services.UI;
using NeuroAccessMaui.UI.Pages;
using Waher.Events;
using Waher.Runtime.Inventory;
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
		private readonly INfcIsoDepSessionService nfcIsoDepSessionService = ServiceRef.Provider.GetRequiredService<INfcIsoDepSessionService>();
		private readonly ITravelDocumentEvidenceService travelDocumentEvidenceService = ServiceRef.Provider.GetRequiredService<ITravelDocumentEvidenceService>();
		private readonly ITravelDocumentReadoutService travelDocumentReadoutService = ServiceRef.Provider.GetRequiredService<ITravelDocumentReadoutService>();

		/// <summary>
		/// Near-Field Communication (NFC) Service.
		/// </summary>
		public NfcService()
			: base()
		{
		}

		private async Task ReadTravelDocumentAsync(IIsoDepInterface IsoDep)
		{
			TravelDocumentMrzEvidence? Evidence = await this.travelDocumentEvidenceService.TryLoadMrzAsync(CancellationToken.None);
			if (Evidence is null)
				return;

			if (string.IsNullOrWhiteSpace(Evidence.ApplicationIdentityId))
			{
				ServiceRef.LogService.LogWarning("Travel document NFC readout skipped because no application identity seed is available.");
				return;
			}

			TravelDocumentReadoutRequest Request = new TravelDocumentReadoutRequest(
				Evidence.DocumentInformation,
				Evidence.MrzText,
				Evidence.ApplicationIdentityId);
			TravelDocumentReadoutResult Result = await this.travelDocumentReadoutService.ReadAsync(IsoDep, Request, CancellationToken.None);

			if (Result.IsSuccess)
			{
				await this.travelDocumentEvidenceService.SaveReadoutXmlAsync(Result.Xml, CancellationToken.None);
				return;
			}

			ServiceRef.LogService.LogWarning("Travel document NFC readout did not complete.",
				new KeyValuePair<string, object?>("Status", Result.Status),
				new KeyValuePair<string, object?>("AuthenticationResult", Result.AuthenticationResult),
				new KeyValuePair<string, object?>("ReadResult", Result.ReadResult),
				new KeyValuePair<string, object?>("ErrorMessage", Result.ErrorMessage));
		}

		/// <summary>
		/// Method called when a new NFC Tag has been detected.
		/// </summary>
		/// <param name="Tag">NFC Tag</param>
		public async Task TagDetected(INfcTag Tag)
		{
			try
			{
				if (await this.nfcIsoDepSessionService.TryHandleTagAsync(Tag, CancellationToken.None))
					return;

				if (this.nfcIsoDepSessionService.SuppressGenericDispatch)
					return;

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
						await this.ReadTravelDocumentAsync(IsoDep);
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
			INavigationService Nav = ServiceRef.NavigationService;
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
