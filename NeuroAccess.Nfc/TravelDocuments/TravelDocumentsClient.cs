using System;
using System.Collections.Generic;
using System.Diagnostics.CodeAnalysis;
using System.Globalization;
using System.IO;
using System.Reflection;
using System.Security.Cryptography;
using System.Threading.Tasks;
using System.Xml;
using NeuroAccess.Nfc.TravelDocuments.Certificates;
using NeuroAccess.Nfc.TravelDocuments.DataObjects;
using NeuroAccess.Nfc.TravelDocuments.Events;
using NeuroAccess.Nfc.TravelDocuments.PACE;
using NeuroAccess.Nfc.TravelDocuments.RevocationLists;
using NeuroAccess.Nfc.TravelDocuments.Security;
using NeuroAccess.Nfc.TravelDocuments.Security.Properties.CertificateExtensions;
using NeuroAccess.Nfc.TravelDocuments.Security.Properties.Keys;
using Waher.Content;
using Waher.Events;
using Waher.Networking;
using Waher.Networking.Sniffers;
using Waher.Runtime.Collections;
using Waher.Runtime.Inventory;
using Waher.Security;
using Waher.Security.EllipticCurves;

namespace NeuroAccess.Nfc.TravelDocuments
{
	/// <summary>
	/// Travel Documents Client, implementing ICAO 9303 to communicate with machine-readable
	/// travel documents, such as passports, visas, and identity cards.
	/// </summary>
	/// <param name="TagInterface">NFC Interface</param>
	/// <param name="DocumentInformation">Document Information parsed from the MRZ.</param>
	/// <param name="LocalKeySeed">Local Key Seed value, permitting the association of the interaction
	/// with an external object.</param>
	/// <param name="Sniffers">Optional sniffers.</param>
	public sealed class TravelDocumentsClient(IIsoDepInterface TagInterface,
		DocumentInformation DocumentInformation, byte[]? LocalKeySeed, params ISniffer[] Sniffers)
		: CommunicationLayer(false, Sniffers), IDisposable
	{
		private static readonly Dictionary<ushort, IDataObject> dataObjects = GetDataObjects();
		private ApplicationLevelInformation? appInfo;
		private DocumentSecurityObject? securityinfo;
		private MrzDataObject? mrz;
		private BiometricInformationTemplate[]? biometricEncodingFace;
		private BiometricInformationTemplate[]? biometricEncodingFingers;
		private BiometricInformationTemplate[]? biometricEncodingIrises;
		private DisplayedSignatures? displayedSignatures;
		private AdditionalPersonalDetails? personalInformation;
		private readonly IIsoDepInterface tagInterface = TagInterface;
		private readonly DocumentInformation documentInformation = DocumentInformation;
		private readonly byte[]? localKeySeed = LocalKeySeed;
		private TravelDocumentsState state = TravelDocumentsState.Detected;
		private IPaceProtocol? protocol;
		private CMac? cMac = null;
		private byte[]? ks_Enc = null;
		private byte[]? ks_Mac = null;
		private byte[]? sendSequenceCounter = null;
		private byte[]? zeroIv = null;
		private bool encrypted = false;
		private bool enhancedSecurity = false;
		private bool permitPlatformDependentValidation = true;
		private bool disposed = false;

		/// <summary>
		/// Disposes of the client and clears any keys.
		/// </summary>
		public void Dispose()
		{
			if (!this.disposed)
			{
				this.disposed = true;

				if (this.ks_Enc is not null)
				{
					Array.Clear(this.ks_Enc, 0, this.ks_Enc.Length);
					this.ks_Enc = null;
				}

				if (this.ks_Mac is not null)
				{
					Array.Clear(this.ks_Mac, 0, this.ks_Mac.Length);
					this.ks_Mac = null;
					this.cMac = null;
				}

				this.encrypted = false;

				this.tagInterface.CloseIfOpen();
			}
		}

		/// <summary>
		/// Application-level information, if available.
		/// </summary>
		public ApplicationLevelInformation? AppInfo
		{
			get => this.appInfo;
			set => this.appInfo = value;
		}

		/// <summary>
		/// If platform-dependent signature validation operations are permitted.
		/// </summary>
		public bool PermitPlatformDependentValidation
		{
			get => this.permitPlatformDependentValidation;
			set => this.permitPlatformDependentValidation = value;
		}

		/// <summary>
		/// Event raised when <see cref="AppInfo"/> is updated.
		/// </summary>
		public event EventHandlerAsync? AppInfoUpdated;

		/// <summary>
		/// Security information, if available.
		/// </summary>
		public DocumentSecurityObject? SecurityInfo => this.securityinfo;

		/// <summary>
		/// Event raised when <see cref="SecurityInfo"/> is updated.
		/// </summary>
		public event EventHandlerAsync? SecurityInfoUpdated;

		/// <summary>
		/// MRZ information from DG1, if available.
		/// </summary>
		public MrzDataObject? Mrz => this.mrz;

		/// <summary>
		/// Event raised when <see cref="Mrz"/> is updated.
		/// </summary>
		public event EventHandlerAsync? MrzUpdated;

		/// <summary>
		/// Biometric Encoding of Face in DG2, if available.
		/// </summary>
		public BiometricInformationTemplate[]? BiometricEncodingFace => this.biometricEncodingFace;

		/// <summary>
		/// Event raised when <see cref="BiometricEncodingFace"/> is updated.
		/// </summary>
		public event EventHandlerAsync? BiometricEncodingFaceUpdated;

		/// <summary>
		/// Biometric Encoding of Fingers in DG3, if available.
		/// </summary>
		public BiometricInformationTemplate[]? BiometricEncodingFingers => this.biometricEncodingFingers;

		/// <summary>
		/// Event raised when <see cref="BiometricEncodingFingers"/> is updated.
		/// </summary>
		public event EventHandlerAsync? BiometricEncodingFingersUpdated;

		/// <summary>
		/// Biometric Encoding of Irises in DG4, if available.
		/// </summary>
		public BiometricInformationTemplate[]? BiometricEncodingIrises => this.biometricEncodingIrises;

		/// <summary>
		/// Event raised when <see cref="BiometricEncodingIrises"/> is updated.
		/// </summary>
		public event EventHandlerAsync? BiometricEncodingIrisesUpdated;

		/// <summary>
		/// Displayed Signatures from DG7, if available.
		/// </summary>
		public DisplayedSignatures? DisplayedSignatures => this.displayedSignatures;

		/// <summary>
		/// Event raised when <see cref="DisplayedSignatures"/> is updated.
		/// </summary>
		public event EventHandlerAsync? DisplayedSignaturesUpdated;

		/// <summary>
		/// Additional Personal Information from DG11, if available.
		/// </summary>
		public AdditionalPersonalDetails? PersonalInformation => this.personalInformation;

		/// <summary>
		/// Event raised when <see cref="PersonalInformation"/> is updated.
		/// </summary>
		public event EventHandlerAsync? PersonalInformationUpdated;

		/// <summary>
		/// Current state of client.
		/// </summary>
		public TravelDocumentsState State => this.state;

		private Task SetState(TravelDocumentsState NewState)
		{
			return this.SetState(NewState, null);
		}

		private async Task SetState(TravelDocumentsState NewState, object? AssociatedData)
		{
			this.state = NewState;
			await this.StateChanged.Raise(this, new TravelDocumentsStateEventArgs(this, NewState, AssociatedData));
		}

		/// <summary>
		/// Event raised when the state of the client changes.
		/// </summary>
		public event EventHandlerAsync<TravelDocumentsStateEventArgs>? StateChanged;

		/// <summary>
		/// Selects the master application
		/// </summary>
		/// <returns>If application was selected.</returns>
		public async Task<bool> SelectMaster()
		{
			// Ref §3.6.1.1, ISOC 9303-10: https://www2023.icao.int/publications/Documents/9303_p10_cons_en.pdf

			await this.SetState(TravelDocumentsState.SelectingMaster);

			if (this.HasSniffers)
				this.Information("SelectMaster()");

			byte[] Command =
			[
				ISO_7816.Classes.Basic,
				ISO_7816.Instructions.Select,
				0x00,	// P1 (Select master)
				0x0c,	// P2 (No File Control Information returned)
				0x02,	// Length of data
				0x3f,
				0x00
			];

			byte[] Response = await this.ExecuteCommand(Command);

			return this.CheckResponse(Response);
		}

		/// <summary>
		/// Selects an application
		/// </summary>
		/// <param name="ApplicationId">Application ID (AID)</param>
		/// <returns>If application was selected.</returns>
		public async Task<bool> SelectApplication(byte[] ApplicationId)
		{
			// Ref §3.6.1.2, ISOC 9303-10: https://www2023.icao.int/publications/Documents/9303_p10_cons_en.pdf

			await this.SetState(TravelDocumentsState.SelectingApplication, ApplicationId);

			if (this.HasSniffers)
				this.Information("SelectApplication(" + Hashes.BinaryToString(ApplicationId) + ")");

			byte[] Command =
				CONCAT(
					[
						ISO_7816.Classes.Basic,
						ISO_7816.Instructions.Select,
						0x04,	// P1 (Select by Application ID)
						0x0c,	// P2 (No File Control Information returned)
						(byte)ApplicationId.Length	// Length of data
					],
					ApplicationId);

			byte[] Response = await this.ExecuteCommand(Command);

			return this.CheckResponse(Response);
		}

		/// <summary>
		/// Selects a file
		/// </summary>
		/// <param name="FileId">File to select.</param>
		/// <returns>If file was selected.</returns>
		public async Task<bool> SelectFile(ushort FileId)
		{
			// Ref §3.6.2, ISOC 9303-10: https://www2023.icao.int/publications/Documents/9303_p10_cons_en.pdf

			await this.SetState(TravelDocumentsState.SelectingFile, FileId);

			if (this.HasSniffers)
				this.Information("SelectFile(" + FileId.ToString("X4", CultureInfo.InvariantCulture) + ")");

			byte[] Command =
			[
				ISO_7816.Classes.Basic,
				ISO_7816.Instructions.Select,
				0x02,	// P1 (Select by File ID)
				0x0c,	// P2 (No File Control Information returned)
				0x02,	// Length of data
				(byte)(FileId >> 8),
				(byte)FileId
			];

			byte[] Response = await this.ExecuteCommand(Command);

			return this.CheckResponse(Response);
		}

		private async Task<byte[]> ExecuteCommand(byte[] Command)
		{
			byte[] Response = await this.ExecuteCommandSingle(Command);
			return await this.GetRemainingResponseData(Response);
		}

		private async Task<byte[]> ExecuteCommandSingle(byte[] Command)
		{
			if (!this.encrypted)
				return await this.tagInterface.ExecuteCommand(Command, this);

			// Ref §9.8.4, ISOC 9303-11: https://www2023.icao.int/publications/Documents/9303_p11_cons_en.pdf

			if (this.HasSniffers)
				this.Information("Encrypting APDU: " + Hashes.BinaryToString(Command));

			if (Command.Length < 5)
				throw new ArgumentException("Command too short.", nameof(Command));

			int BlockSize = this.protocol!.BlockLength;
			byte INS = Command[1];
			byte P1 = Command[2];
			byte P2 = Command[3];
			byte Lc;
			byte Le;

			if (Command.Length == 5)
			{
				Lc = 0;
				Le = Command[4];
			}
			else
			{
				Lc = Command[4];
				if (5 + Lc > Command.Length)
					throw new ArgumentException("Command data length exceeds command length.", nameof(Command));

				Le = Lc + 5 < Command.Length ? Command[Lc + 5] : (byte)0;
			}

			byte[] Header =
			[
				ISO_7816.Classes.SecureMessaging,
				INS,
				P1,
				P2
			];
			byte[] HeaderPadding = new byte[BlockSize - 4];
			HeaderPadding[0] = 0x80;

			int PaddedDataLen = (Lc + BlockSize - 1) & ~(BlockSize - 1);

			if (PaddedDataLen + 17 > byte.MaxValue)
				throw new ArgumentException("Command data too long.", nameof(Command));

			byte[] PaddedData = new byte[PaddedDataLen];

			if (Lc > 0)
				Buffer.BlockCopy(Command, 5, PaddedData, 0, Lc);

			if (Lc < PaddedDataLen)
				PaddedData[Lc] = 0x80;

			this.IncrementCounter();

			if (this.HasSniffers)
				this.Information("Send Sequence Number: " + Hashes.BinaryToString(this.sendSequenceCounter));

			byte[] IV = this.protocol.Encrypt(this.ks_Enc!, this.zeroIv!, this.sendSequenceCounter!);

			if (this.HasSniffers)
			{
				this.Information("IV: " + Hashes.BinaryToString(IV));
				this.Information("Padded data to encrypt: " + Hashes.BinaryToString(PaddedData));
			}

			byte[] EncryptedData = this.protocol.Encrypt(this.ks_Enc!, IV, PaddedData);

			if (this.HasSniffers)
				this.Information("Encrypted data: " + Hashes.BinaryToString(EncryptedData));

			byte[] Footer =
			[
				0x97,
				1,
				Le
			];

			byte[] FooterPadding = new byte[BlockSize - 3];
			FooterPadding[0] = 0x80;

			byte[] EncryptedDataHeader = PaddedDataLen == 0 ? [] :
			[
				(INS & 1) == 0 ? (byte)0x87 : (byte)0x85,
				(byte)(PaddedDataLen + 1),
				1
			];

			int AssociatedDataPadLen = (EncryptedDataHeader.Length + Footer.Length) % BlockSize;    // Len(SSC+Header+HeaderPading+EncryptedData)=0 mod BlockSize
			byte[] AssociatedDataPadding;

			if (AssociatedDataPadLen == 0)
				AssociatedDataPadding = [];
			else
			{
				AssociatedDataPadding = new byte[BlockSize - AssociatedDataPadLen];
				AssociatedDataPadding[0] = 0x80;
			}

			byte[] AssociatedData = CONCAT(
				this.sendSequenceCounter!,
				Header,
				HeaderPadding,
				EncryptedDataHeader,
				EncryptedData,
				Footer,
				AssociatedDataPadding);

			if (this.HasSniffers)
				this.Information("Associated data to sign: " + Hashes.BinaryToString(AssociatedData));

			byte[] Signature = this.cMac!.Sign(AssociatedData, 8);

			byte[] EncryptedCommand = CONCAT(
				Header,
				[(byte)(EncryptedDataHeader.Length + EncryptedData.Length + Footer.Length + 10)],
				EncryptedDataHeader,
				EncryptedData,
				Footer,
				[
					0x8e,
					0x08
				],
				Signature,
				[
					0	// Standard length
				]);

			byte[] Response = await this.tagInterface.ExecuteCommand(EncryptedCommand, this);
			int c = Response.Length - 2;

			if (c <= 0)
				return Response ?? [];

			byte[]? EncryptedResponseData = null;
			byte[]? ResponseSignature = null;
			int i = 0;
			byte SW1 = Response[c];
			byte SW2 = Response[c + 1];
			int StartOfSignature = 0;

			while (i < c)
			{
				switch (Response[i++])
				{
					case 0x87:
						if (i >= c)
						{
							this.UnexpectedEndOfResponse();
							return Response;
						}

						int L = Response[i++];

						switch (L)
						{
							case 0x81:
								if (i >= c)
								{
									this.UnexpectedEndOfResponse();
									return Response;
								}

								L = Response[i++];
								break;

							case 0x82:
								if (i + 1 >= c)
								{
									this.UnexpectedEndOfResponse();
									return Response;
								}

								L = Response[i++];
								L <<= 8;
								L |= Response[i++];
								break;

							default:
								L &= 0x7f;
								break;
						}

						if (i >= c)
						{
							this.UnexpectedEndOfResponse();
							return Response;
						}

						if (L == 0)
						{
							this.Error("Expected length of DO'87' block.");
							return Response;
						}

						byte PaddingByte = Response[i++];

						if (PaddingByte != 1 && PaddingByte != 2)
						{
							this.Error("Expected 01 or 02 as padding byte in DO'87' block.");
							return Response;
						}

						L--;
						if (i + L > c)
						{
							this.UnexpectedEndOfResponse();
							return Response;
						}

						EncryptedResponseData = new byte[L];
						Buffer.BlockCopy(Response, i, EncryptedResponseData, 0, L);
						i += L;

						if (PaddingByte == 2)
						{
							if (i < c && Response[i] == 0x80)
							{
								i++;

								while (i < c && Response[i] == 0x00)
									i++;
							}
						}
						break;

					case 0x99:
						if (i >= c)
						{
							this.UnexpectedEndOfResponse();
							return Response;
						}

						L = Response[i++];

						if (L != 2)
						{
							this.Error("Expected DO'99' block to have a length of 02.");
							return Response;
						}

						if (i + L > c)
						{
							this.UnexpectedEndOfResponse();
							return Response;
						}

						SW1 = Response[i++];
						SW2 = Response[i++];
						break;

					case 0x8e:
						StartOfSignature = i - 1;

						if (i >= c)
						{
							this.UnexpectedEndOfResponse();
							return Response;
						}

						L = Response[i++];

						if (L != 8)
						{
							this.Error("Expected DO'8E' block to have a length of 08.");
							return Response;
						}

						if (i + L > c)
						{
							this.UnexpectedEndOfResponse();
							return Response;
						}

						ResponseSignature = new byte[L];
						Buffer.BlockCopy(Response, i, ResponseSignature, 0, L);
						i += L;
						break;

					default:
						this.Error("Unexpected DO block: " + Response[i - 1].ToString("X2", CultureInfo.InvariantCulture));
						return Response;
				}
			}

			if (ResponseSignature is null)
			{
				this.Error("Missing DO'8E' block with response signature.");
				return Response;
			}

			int ResponsePadLength = StartOfSignature % BlockSize;
			byte[] ResponsePadding;

			if (ResponsePadLength == 0)
				ResponsePadding = [];
			else
			{
				ResponsePadding = new byte[BlockSize - ResponsePadLength];
				ResponsePadding[0] = 0x80;
			}

			this.IncrementCounter();

			AssociatedData = new byte[StartOfSignature];
			Buffer.BlockCopy(Response, 0, AssociatedData, 0, StartOfSignature);

			AssociatedData = CONCAT(
				this.sendSequenceCounter!,
				AssociatedData,
				ResponsePadding);

			if (this.HasSniffers)
				this.Information("Associated data to verify: " + Hashes.BinaryToString(AssociatedData));

			if (!this.cMac.Verify(AssociatedData, ResponseSignature))
			{
				this.Error("Invalid response signature.");
				return Response;
			}

			if (EncryptedResponseData is null)
				Response = [SW1, SW2];
			else
			{
				IV = this.protocol.Encrypt(this.ks_Enc!, this.zeroIv!, this.sendSequenceCounter!);
				Response = this.protocol.Decrypt(this.ks_Enc!, IV, EncryptedResponseData);

				if (IsPadded(Response, out int NrBytesPadding))
					Array.Resize(ref Response, Response.Length - NrBytesPadding);

				Response = CONCAT(Response, [SW1, SW2]);
			}

			if (this.HasSniffers)
				this.Information("Decrypted response: " + Hashes.BinaryToString(Response));

			return Response;
		}

		private async Task<byte[]> GetRemainingResponseData(byte[] Response)
		{
			while (Response.Length >= 2 &&
				Response[^2] == (byte)Iso7816StatusCategory.DataStillAvailable)
			{
				byte Le = Response[^1];
				byte[] GetResponseCommand =
				[
					ISO_7816.Classes.Basic,
					ISO_7816.Instructions.GetResponse,
					0x00,
					0x00,
					Le
				];

				byte[] NextResponse = await this.ExecuteCommandSingle(GetResponseCommand);
				byte[] CombinedResponse = new byte[Response.Length + NextResponse.Length - 2];

				Buffer.BlockCopy(Response, 0, CombinedResponse, 0, Response.Length - 2);
				Buffer.BlockCopy(NextResponse, 0, CombinedResponse, Response.Length - 2, NextResponse.Length);
				Response = CombinedResponse;
			}

			return Response;
		}

		private static bool IsPadded(byte[] Data, out int NrBytesPadding)
		{
			NrBytesPadding = 0;

			if (Data is null)
				return false;

			int c = Data.Length;
			if (c == 0)
				return false;

			while (c > 0 && Data[--c] == 0)
				;

			if (Data[c] != 0x80)
				return false;

			NrBytesPadding = Data.Length - c;

			return true;
		}

		private void UnexpectedEndOfResponse()
		{
			this.Error("Unexpected end of encrypted response.");
		}

		private void IncrementCounter()
		{
			if (this.sendSequenceCounter is null)
				throw new InvalidOperationException("Send Sequence Counter is not initialized.");

			int i = this.sendSequenceCounter.Length;

			while (++this.sendSequenceCounter[--i] == 0 && i >= 0)
				;
		}

		/// <summary>
		/// Processes basic status word response codes.
		/// </summary>
		/// <param name="CheckResponse">Response received.</param>
		/// <returns>If processing can continue.</returns>
		private bool CheckResponse(byte[] CheckResponse)
		{
			if (CheckResponse is null || CheckResponse.Length < 2)
				return false;

			byte SW1 = CheckResponse[^2];
			byte SW2 = CheckResponse[^1];

			switch ((Iso7816StatusCategory)SW1)
			{
				case Iso7816StatusCategory.Ok:
					return true;

				case Iso7816StatusCategory.DataStillAvailable:
					this.Information(SW2.ToString(CultureInfo.InvariantCulture) + " bytes still available");
					return true;

				case Iso7816StatusCategory.WarningUnchanged:
					switch (SW2)
					{
						case 0:
							this.Warning("Warning, state unchanged. No information given.");
							break;

						default:
							this.Warning("Warning " + SW2.ToString("X2", CultureInfo.InvariantCulture) + " triggered by card. State unchanged.");
							break;

						case 0x81:
							this.Warning("Part of returned data may be corrupted");
							break;

						case 0x82:
							this.Warning("End of file or record reached before reading Ne bytes.");
							break;

						case 0x83:
							this.Warning("Selected file deactivated.");
							break;

						case 0x84:
							this.Warning("File control information not formatted correctly.");
							break;

						case 0x85:
							this.Warning("Selected file in termination state.");
							break;

						case 0x86:
							this.Warning("No input data available from a sensor on the card.");
							break;
					}
					return true;

				case Iso7816StatusCategory.WarningChanged:
					switch (SW2)
					{
						case 0:
							this.Warning("Warning, state changed. No information given.");
							break;

						default:
							this.Warning("Warning " + SW2.ToString("X2", CultureInfo.InvariantCulture) + " triggered by card. State changed.");
							break;

						case 0x81:
							this.Warning("File filled up by the last write.");
							break;
					}
					return true;

				case Iso7816StatusCategory.ErrorUnchanged:
					switch (SW2)
					{
						case 0:
							this.Error("Error, state unchanged. No information given.");
							break;

						default:
							this.Error("Error " + SW2.ToString("X2", CultureInfo.InvariantCulture) + " triggered by card. State unchanged.");
							break;

						case 0x01:
							this.Error("Immediate response required by the card.");
							break;
					}
					return false;

				case Iso7816StatusCategory.ErrorChanged:
					switch (SW2)
					{
						case 0:
							this.Error("Error, state changed. No information given.");
							break;

						default:
							this.Error("Error " + SW2.ToString("X2", CultureInfo.InvariantCulture) + " triggered by card. State changed.");
							break;

						case 0x81:
							this.Error("Memory failure.");
							break;
					}
					return false;

				case Iso7816StatusCategory.SecurityIssue:
					this.Error("Security issue detected.");
					return false;

				case Iso7816StatusCategory.WrongLength:
					this.Error("Wrong length.");
					return false;

				case Iso7816StatusCategory.FunctionNotSupported:
					switch (SW2)
					{
						case 0:
							this.Error("Function Not Supported. No information given.");
							break;

						default:
							this.Error("Function Not Supported " + SW2.ToString("X2", CultureInfo.InvariantCulture) + " triggered by card.");
							break;

						case 0x81:
							this.Error("Logical channel not supported.");
							break;

						case 0x82:
							this.Error("Secure messaging not supported.");
							break;

						case 0x83:
							this.Error("Last command of the chain expected.");
							break;

						case 0x84:
							this.Error("Command chaining not supported.");
							break;
					}
					return false;

				case Iso7816StatusCategory.NotAllowed:
					switch (SW2)
					{
						case 0:
							this.Error("Not Allowed. No information given.");
							break;

						default:
							this.Error("Not Allowed " + SW2.ToString("X2", CultureInfo.InvariantCulture) + " triggered by card.");
							break;

						case 0x81:
							this.Error("Command incompatible with file structure.");
							break;

						case 0x82:
							this.Error("Security status not satisfied.");
							break;

						case 0x83:
							this.Error("Authentication method blocked.");
							break;

						case 0x84:
							this.Error("Reference data not usable.");
							break;

						case 0x85:
							this.Error("Conditions of use not satisfied.");
							break;

						case 0x86:
							this.Error("Command not allowed (no current EF).");
							break;

						case 0x87:
							this.Error("Expected secure messaging data objects missing.");
							break;

						case 0x88:
							this.Error("Incorrect secure messaging data objects.");
							break;
					}
					return false;

				case Iso7816StatusCategory.WrongParameters:
					switch (SW2)
					{
						case 0:
							this.Error("Wrong Parameters. No information given.");
							break;

						default:
							this.Error("Wrong Parameters " + SW2.ToString("X2", CultureInfo.InvariantCulture) + " triggered by card.");
							break;

						case 0x80:
							this.Error("Incorrect parameters in the command data field.");
							break;

						case 0x81:
							this.Error("Function not supported.");
							break;

						case 0x82:
							this.Error("File or application not found.");
							break;

						case 0x83:
							this.Error("Record not found.");
							break;

						case 0x84:
							this.Error("Not enough memory space in the file.");
							break;

						case 0x85:
							this.Error("Nc inconsistent with TLV structure.");
							break;

						case 0x86:
							this.Error("Incorrect parameters P1-P2.");
							break;

						case 0x87:
							this.Error("Nc inconsistent with parameters P1-P2.");
							break;

						case 0x88:
							this.Error("Referenced data or reference data not found (exact meaning depending on the command).");
							break;

						case 0x89:
							this.Error("File already exists.");
							break;

						case 0x8A:
							this.Error("DF name already exists.");
							break;
					}
					return false;

				case Iso7816StatusCategory.WrongLeField:
					this.Error("Le field incorrect. Should be " + SW2.ToString("X2", CultureInfo.InvariantCulture));
					return false;

				default:
					this.Error("Unexpected response received. SW1=" + SW1.ToString("X2", CultureInfo.InvariantCulture) +
						", SW2=" + SW2.ToString("X2", CultureInfo.InvariantCulture));
					return false;
			}
		}

		/// <summary>
		/// Reads binary information from the currently selected file.
		/// </summary>
		/// <returns>Read data, or null if an error occurred.</returns>
		public Task<KeyValuePair<byte[]?, bool>> ReadBinary(uint Offset)
		{
			return this.ReadBinary(Offset, 0);
		}

		/// <summary>
		/// Reads binary information from the currently selected file.
		/// </summary>
		/// <param name="NrBytes">Number of bytes to read. 0=256 bytes.</param>
		/// <returns>Read data, or null if an error occurred.</returns>
		public async Task<KeyValuePair<byte[]?, bool>> ReadBinary(uint Offset, byte NrBytes)
		{
			// Ref §3.6.3, ISOC 9303-10: https://www2023.icao.int/publications/Documents/9303_p10_cons_en.pdf

			await this.SetState(TravelDocumentsState.ReadingBinary, Offset);

			if (this.HasSniffers)
			{
				this.Information("ReadBinary(" + Offset.ToString(CultureInfo.InvariantCulture) + "," +
					NrBytes.ToString(CultureInfo.InvariantCulture) + ")");
			}

			byte[] Command;

			if (Offset <= short.MaxValue)
			{
				Command =
				[
					ISO_7816.Classes.Basic,
					ISO_7816.Instructions.ReadBinary,
					(byte)(Offset >> 8),	// P1
					(byte)Offset,			// P2
					NrBytes                 // Le
				];
			}
			else if (Offset < 0x1000000)
			{
				Command =
				[
					ISO_7816.Classes.Basic,
					ISO_7816.Instructions.ReadBinary + 1,
					0,						// P1
					0,						// P2
					5,						// Lc=5 bytes
					0x54,					// DO'54'
					3,						// Length of DO'54' value
					(byte)(Offset >> 16),
					(byte)(Offset >> 8),
					(byte)Offset,
					NrBytes                 // Le
				];
			}
			else
			{
				Command =
				[
					ISO_7816.Classes.Basic,
					ISO_7816.Instructions.ReadBinary + 1,
					0,						// P1
					0,						// P2
					6,						// Lc=6 bytes
					0x54,					// DO'54'
					4,						// Length of DO'54' value
					(byte)(Offset >> 24),
					(byte)(Offset >> 16),
					(byte)(Offset >> 8),
					(byte)Offset,
					NrBytes                 // Le
				];
			}

			byte[] Response = await this.ExecuteCommand(Command);
			int c = Response.Length;

			if (!this.CheckResponse(Response))
			{
				if (Response is not null &&
					c >= 2 &&
					Response[^2] == (byte)Iso7816StatusCategory.WrongLeField)
				{
					Command[^1] = Response[^1];
					Response = await this.ExecuteCommand(Command);

					if (!this.CheckResponse(Response))
						return new KeyValuePair<byte[]?, bool>(null, false);
				}
				else
					return new KeyValuePair<byte[]?, bool>(null, false);
			}

			c = Response.Length;
			bool More = Response[^2] == (byte)Iso7816StatusCategory.DataStillAvailable;
			byte[] Data = new byte[c - 2];
			Buffer.BlockCopy(Response, 0, Data, 0, c - 2);

			return new KeyValuePair<byte[]?, bool>(Data, More);
		}

		/// <summary>
		/// Selects and downloads a file from the travel document.
		/// </summary>
		/// <param name="FileId">File to download.</param>
		/// <param name="FileName">Name of file.</param>
		/// <returns>Downloaded file, or null if unable to download file.</returns>
		public async Task<byte[]?> DownloadFile(ushort FileId, string FileName)
		{
			await this.SetState(TravelDocumentsState.DownloadingFile, FileName);

			this.Information("Downloading " + FileName + "...");

			if (!await this.SelectFile(FileId))
				return null;

			using MemoryStream File = new();
			uint Offset = 0;
			int? ExpectedLength = null;
			int BytesDownloaded = 0;

			while (!ExpectedLength.HasValue || BytesDownloaded < ExpectedLength.Value)
			{
				KeyValuePair<byte[]?, bool> P = await this.ReadBinary(Offset);
				if (P.Key is null)
					return null;

				File.Write(P.Key, 0, P.Key.Length);
				BytesDownloaded += P.Key.Length;

				if (!ExpectedLength.HasValue)
				{
					ExpectedLength = GetExpectedLength(P.Key);
					if (ExpectedLength.HasValue)
						this.Information("Expected length of file: " + ExpectedLength.Value.ToString(CultureInfo.InvariantCulture));
				}

				if (!P.Value && !ExpectedLength.HasValue)
					break;

				Offset += (uint)P.Key.Length;
			}

			byte[] Downloaded = File.ToArray();
			int c = Downloaded.Length;

			if (ExpectedLength.HasValue && c > ExpectedLength.Value)
			{
				bool AllZeroes = true;
				int i;

				for (i = ExpectedLength.Value; i < c; i++)
				{
					if (Downloaded[i] != 0)
					{
						AllZeroes = false;
						break;
					}
				}

				if (AllZeroes)
				{
					Array.Resize(ref Downloaded, ExpectedLength.Value);
					this.Warning("Downloaded data exceeds expected length, but excess data is all zeroes. Truncating to expected length.");
				}
				else
					this.Warning("Downloaded data exceeds expected length, and excess data is not all zeroes, so it is not truncated.");
			}

			await this.SetState(TravelDocumentsState.DownloadedFile, FileName);
			return Downloaded;
		}

		private static int? GetExpectedLength(byte[] Bin)
		{
			if (Bin is null)
				return null;

			uint i = 0;
			uint c = (uint)Bin.Length;
			byte b;

			if (c == 0)
				return 0;

			b = Bin[i++];
			if ((b & 0x1f) == 0x1f)
			{
				do
				{
					if (i >= c)
						return null;

					b = Bin[i++];
				}
				while ((b & 0x80) != 0);
			}

			if (i >= c)
				return null;

			b = Bin[i++];

			if (b < 0x80)
				return (int)(i + b);

			b -= 0x80;

			if (b > 4)
				return null;    // Length too long to be valid.

			if (i + b > c)
				return null;    // Length exceeds available data.

			uint Length = 0;

			while (b > 0)
			{
				Length <<= 8;
				Length |= Bin[i++];
				b--;
			}

			Length += i;

			if (Length > int.MaxValue)
				return null;    // Length too long to be valid.

			return (int)Length;
		}

		/// <summary>
		/// Tries to fins a PACE protocol matching information available in the EF.CardAccess file.
		/// </summary>
		/// <param name="CardAccess">Contents of EF.CardAccess file.</param>
		/// <returns>if a protocol was found matching the contents of the EF.CardAccess file.</returns>
		public async Task<bool> TryFindPaceProtocol(object? CardAccess)
		{
			await this.SetState(TravelDocumentsState.FindingCipher);

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

			if (CardAccess is not Vector SecurityInfos)
				return false;

			ChunkedList<string> OidsFound = [];
			IPaceProtocol? Best = null;

			foreach (object Item in SecurityInfos)
			{
				if (Item is IPaceProtocol Current)
				{
					OidsFound.Add(Current.Oid);

					if (this.HasSniffers)
						this.Information("OID " + Current.Oid + " (" + Current.GetType().Name.Replace('_', '-') + ") supported.");

					if (Best is null ||
						Current.SecurityStrength > Best.SecurityStrength ||
						(Current.SecurityStrength == Best.SecurityStrength &&
						Current.ChipAuthenticationMapping && !Best.ChipAuthenticationMapping))
					{
						Best = Current;
					}
				}
				else if (Item is Vector SecurityInfo &&
					SecurityInfo.Length > 0 &&
					SecurityInfo.FirstElement is string Oid)
				{
					OidsFound.Add(Oid);

					if (this.HasSniffers)
						this.Information("OID " + Oid + " lacks implemented support.");
				}
				else
					continue;
			}

			if (Best is null && OidsFound.HasFirstItem)
			{
				// Notify operators & developers that ciphers have been detected that
				// require implementation.
				//
				// Note: Do not include sensitive personal information in the log entry.

				Log.Alert("No supported PACE protocol found. OIDs found: " +
					string.Join(", ", OidsFound),
					new KeyValuePair<string, object>("DocumentType", this.documentInformation.DocumentType ?? string.Empty),
					new KeyValuePair<string, object>("IssuingState", this.documentInformation.IssuingState ?? string.Empty),
					new KeyValuePair<string, object>("Nationality", this.documentInformation.Nationality ?? string.Empty));
			}

			this.protocol = Best;
			this.zeroIv = new byte[this.protocol?.BlockLength ?? 0];

			return Best is not null;
		}

		/// <summary>
		/// Initializes PACE authentication.
		/// </summary>
		/// <returns>If successful.</returns>
		private async Task<bool> InitializePACE()
		{
			await this.SetState(TravelDocumentsState.SelectingCipher, this.protocol!.GetType().Name);

			if (this.HasSniffers)
				this.Information("MSE:Set AT(" + this.protocol.Oid + ",MRZ)");

			string[] Parts = this.protocol!.Oid.Split('.');
			int i, c = Parts.Length - 1;
			byte[] PartBytes = new byte[c];

			for (i = 0; i < c; i++)
			{
				if (!byte.TryParse(Parts[i + 1], out PartBytes[i]))     // Skip first 0.
					return false;
			}

			byte[] ParameterIdEncoding;

			if (this.protocol.ParameterId.HasValue)
			{
				ParameterIdEncoding = this.protocol.ParameterId.Value.ToByteArray(true, true);

				ParameterIdEncoding = CONCAT(
					[
						0x84,
						(byte)ParameterIdEncoding.Length
					],
					ParameterIdEncoding);
			}
			else
				ParameterIdEncoding = [];

			byte[] Command = CONCAT(
				[
					ISO_7816.Classes.Basic,
					ISO_7816.Instructions.MessageSecurityEnvironment,
					0xC1,			// P1 - Set
					0xA4,			// P2 - PACE
					(byte)(5 + c + ParameterIdEncoding.Length),	// Lc
					0x80,			// Algorithm reference
					(byte)c			// OID Length (excluding first zero)
				],
				[
					PartBytes,
					[
						0x83,		// Key reference
						0x01,		// Key reference length
						0x01		// MRZ key reference (0x02 = CAN, 0x03 = PIN, 0x04 = PUK)
					],
					ParameterIdEncoding
				]);

			byte[] Response = await this.ExecuteCommand(Command);

			return this.CheckResponse(Response);
		}

		/// <summary>
		/// Concatenates a series of byte arrays.
		/// </summary>
		/// <param name="Bytes">First byte array</param>
		/// <param name="MoreBytes">following bytes arrays.</param>
		/// <returns>Concatenated byte array.</returns>
		public static byte[] CONCAT(byte[] Bytes, params byte[][] MoreBytes)
		{
			int c = Bytes.Length;
			int i = c;

			foreach (byte[] A in MoreBytes)
				c += A.Length;

			byte[] Result = new byte[c];

			Buffer.BlockCopy(Bytes, 0, Result, 0, i);

			foreach (byte[] A in MoreBytes)
			{
				Buffer.BlockCopy(A, 0, Result, i, c = A.Length);
				i += c;
			}

			return Result;
		}

		/// <summary>
		/// Performs byte-wise XOR operation on byte arrays of equal length.
		/// </summary>
		/// <param name="A">Array 1</param>
		/// <param name="B">Array 2</param>
		/// <returns>A XOR B</returns>
		public static byte[] XOR(byte[] A, byte[] B)
		{
			int i, c = A.Length;

			if (B.Length != c)
				throw new ArgumentException("Byte arrays must have the same length.");

			byte[] Result = new byte[c];
			for (i = 0; i < c; i++)
				Result[i] = (byte)(A[i] ^ B[i]);

			return Result;
		}

		/// <summary>
		/// Seed for computing cryptographic keys (§D.2)
		/// </summary>
		/// <param name="Info">Document Information</param>
		public static byte[] PACE_K(DocumentInformation Info)
		{
			byte[] Data = InternetContent.ISO_8859_1.GetBytes(Info.MRZ_Information!);
			return Hashes.ComputeSHA1Hash(Data);
		}

		/// <summary>
		/// Basic Key-Derivation Function
		/// </summary>
		/// <param name="KSeed">Seed value</param>
		/// <param name="Counter">Counter</param>
		/// <param name="AdjustParity">If parity in bytes should be adjusted (for 3DES only).</param>
		/// <param name="HashFunction">Hash function to use.</param>
		/// <param name="NrBytes">Maximum number of bytes to use for key.</param>
		/// <returns>Key</returns>
		public static byte[] KDF(byte[] KSeed, int Counter, bool AdjustParity,
			HashFunctionArray HashFunction, int NrBytes)
		{
			int c = KSeed.Length;
			byte[] D = new byte[c + 4];
			Buffer.BlockCopy(KSeed, 0, D, 0, c);
			int i;

			for (i = c + 3; i >= c; i--)
			{
				D[i] = (byte)Counter;
				Counter >>= 8;
			}

			byte[] H = HashFunction(D);

			if (H.Length > NrBytes)
				Array.Resize(ref H, NrBytes);

			if (AdjustParity)
				OddParity(H);

			return H;
		}

		private static void OddParity(byte[] H)
		{
			int i, j, c = H.Length;
			byte b;

			for (i = 0; i < c; i++)
			{
				b = H[i];
				j = 0;

				while (b != 0)
				{
					j += b & 1;
					b >>= 1;
				}

				if ((j & 1) == 0)
					H[i] ^= 1;
			}
		}
		/// <summary>
		/// Seed for computing cryptographic keys (§D.2)
		/// </summary>
		/// <param name="Info">Document Information</param>
		public static byte[] BAC_KSeed(DocumentInformation Info)
		{
			byte[] Data = InternetContent.ISO_8859_1.GetBytes(Info.MRZ_Information!);
			byte[] H = Hashes.ComputeSHA1Hash(Data);
			Array.Resize(ref H, 16);
			return H;
		}

		/// <summary>
		/// BAC Encryption Key (3DES). Ref: §D.1
		/// </summary>
		public static byte[] BAC_KEnc(DocumentInformation Info)
		{
			return BAC_KDF(Info, 1, true);  // KDF(K,1)
		}

		/// <summary>
		/// BAC MAC Key (3DES). Ref: §D.1
		/// </summary>
		public static byte[] BAC_KMac(DocumentInformation Info)
		{
			return BAC_KDF(Info, 2, true);   // KDF(K,2)
		}

		private static byte[] BAC_KDF(DocumentInformation Info, int Counter, bool AdjustParity)
		{
			byte[] KSeed = BAC_KSeed(Info);
			return KDF(KSeed, Counter, AdjustParity, Hashes.ComputeSHA1Hash, 16);
		}

		/// <summary>
		/// Get PACE Nonce
		/// </summary>
		/// <returns>Nonce</returns>
		private async Task<byte[]?> GetPaceEncryptedNonce()
		{
			await this.SetState(TravelDocumentsState.GettingNonce);

			this.Information("General Authenticate (Get Encrypted Nonce)");

			byte[] Command =
			[
				ISO_7816.Classes.Chaining,
				ISO_7816.Instructions.GeneralAuthenticate,
				0x00,		// P1
				0x00,		// P2
				0x02,		// Lc
				0x7c, 0x00,	// Absent
				0x00		// Le (Maximal response length: 256 bytes)
			];

			byte[] Response = await this.ExecuteCommand(Command);

			if (!this.CheckResponse(Response))
				return null;

			if (Response.Length < 6 ||
				Response[0] != 0x7c ||
				Response.Length != Response[1] + 4 ||
				Response[2] != 0x80 ||      // Encrypted nonce
				Response.Length != Response[3] + 6 ||
				Response[^2] != 0x90 ||
				Response[^1] != 0x00)
			{
				this.Error("Unexpected response received.");
				return null;
			}

			int c = Response[3];
			byte[] Nonce = new byte[c];

			Buffer.BlockCopy(Response, 4, Nonce, 0, c);

			return Nonce;
		}

		/// <summary>
		/// Get PACE Remote Public Key
		/// </summary>
		/// <param name="LocalPublicKey">Local Public Key</param>
		/// <returns>Remote Public Key</returns>
		private async Task<byte[]?> GetPaceRemotePublicKey(byte[] LocalPublicKey)
		{
			await this.SetState(TravelDocumentsState.GettingPublicKey);

			return DecodePublicKey(await this.GeneralAuthenticate(
				EncodePublicKey(LocalPublicKey),
				"Get Remote Public Key",
				false,  // More commands in chain expected
				0x81,   // Mapping Data
				0x82));  // Mapping Data response
		}

		/// <summary>
		/// Get PACE Remote Ephemeral Key
		/// </summary>
		/// <param name="LocalPublicEphemeralKey">Local Public Ephemeral Key</param>
		/// <returns>Remote Public Ephemeral Key</returns>
		private async Task<byte[]?> GetPaceRemotePublicEphemeralKey(byte[] LocalPublicEphemeralKey)
		{
			await this.SetState(TravelDocumentsState.GettingEphemeralPublicKey);

			return DecodePublicKey(await this.GeneralAuthenticate(
				EncodePublicKey(LocalPublicEphemeralKey),
				"Get Remote Ephemeral Public Key",
				false,  // More commands in chain expected
				0x83,   // Terminal's Ephemeral Public Key
				0x84));  // Chip's Ephemeral Public Key
		}

		/// <summary>
		/// Get PACE Remote Verification Token
		/// </summary>
		/// <param name="LocalVerificationToken">Local Verification Token</param>
		/// <returns>Remote Verification Token</returns>
		private async Task<byte[]?> GetPaceRemoteVerificationToken(
			byte[] LocalVerificationToken)
		{
			await this.SetState(TravelDocumentsState.GettingVerificationToken);

			return await this.GeneralAuthenticate(LocalVerificationToken,
				"Get Remote Verification Token",
				true,   // Last command in chain
				0x85,   // Terminal's Verification Token
				0x86);  // Chip's Verification Token
		}

		private static byte[] EncodePublicKey(byte[] LocalPublicKey)
		{
			int c = LocalPublicKey.Length;
			byte[] EncodedPublicKey = new byte[c + 1];

			EncodedPublicKey[0] = 4;    // X coordinate following by Y coordinate (default for EEC curves)
			Buffer.BlockCopy(LocalPublicKey, 0, EncodedPublicKey, 1, c);

			return EncodedPublicKey;
		}

		private static byte[]? DecodePublicKey(byte[]? Data)
		{
			int c;

			if (Data is null || (c = Data.Length) == 0 || Data[0] != 4)   // X coordinate following by Y coordinate (default for EEC curves)
				return null;

			byte[] DecodedPublicKey = new byte[c - 1];
			Buffer.BlockCopy(Data, 1, DecodedPublicKey, 0, c - 1);

			return DecodedPublicKey;
		}

		private async Task<byte[]?> GeneralAuthenticate(byte[] Data, string Comment, bool LastInChain,
			byte Command, byte ExpectedResponse)
		{
			this.Information("General Authenticate (" + Comment + ")");

			int c = Data.Length;

			byte[] Request = CONCAT(
				[
					LastInChain ? ISO_7816.Classes.Basic : ISO_7816.Classes.Chaining,
					ISO_7816.Instructions.GeneralAuthenticate,
					0x00,									// P1
					0x00,									// P2
					(byte)(c + 4)		// Lc
				],
				[
					[
						0x7c,			// Dynamic Authentication Data
						(byte)(c + 2),
						Command,
						(byte)c
					],
					Data,
					[ 0x00 ]	// Le (Maximal response length: 256 bytes)
				]);

			byte[] Response = await this.ExecuteCommand(Request);

			if (!this.CheckResponse(Response))
				return null;

			if (Response.Length < 6 ||
				Response[0] != 0x7c ||
				Response.Length != Response[1] + 4 ||
				Response[2] != ExpectedResponse ||
				Response.Length != Response[3] + 6 ||
				Response[^2] != 0x90 ||
				Response[^1] != 0x00)
			{
				this.Error("Unexpected response received.");
				return null;
			}

			c = Response[3];
			byte[] ResponseData = new byte[c];

			Buffer.BlockCopy(Response, 4, ResponseData, 0, c);

			return ResponseData;
		}

		/// <summary>
		/// Get Challenge (§7.1.5.4, §D.3)
		/// </summary>
		/// <returns>Challenge</returns>
		private async Task<byte[]?> GetBacChallenge()
		{
			await this.SetState(TravelDocumentsState.GettingChallenge);

			this.Information("GetChallenge");

			byte[] Command =
			[
				ISO_7816.Classes.Basic,
				ISO_7816.Instructions.GetChallenge,
				0x00,	// P1
				0x00,	// P2
				0x08	// Le
			];

			byte[] Response = await this.ExecuteCommand(Command);

			if (!this.CheckResponse(Response))
				return null;

			if (Response.Length != 10 || Response[8] != 0x90 || Response[9] != 0x00)
			{
				this.Error("Unexpected response received.");
				return null;
			}

			byte[] Challenge = new byte[8];
			Buffer.BlockCopy(Response, 0, Challenge, 0, 8);

			return Challenge;
		}

		/// <summary>
		/// Send Response to challenge (§7.1.5.4, §D.3)
		/// </summary>
		/// <param name="ChallengeResponse">ChallengeResponse.</param>
		/// <returns>E.IC and M.IC</returns>
		private async Task<KeyValuePair<byte[]?, byte[]?>> ExternalBacAuthenticate(byte[] ChallengeResponse)
		{
			await this.SetState(TravelDocumentsState.RespondingToChallenge);

			this.Information("ChallengeResponse");

			byte Lc = (byte)ChallengeResponse.Length;
			byte[] Command = CONCAT(
				[
					ISO_7816.Classes.Basic,
					ISO_7816.Instructions.ExternalAuthenticate,
					0x00,	// P1
					0x00,	// P2
					Lc
				],
				ChallengeResponse,
				[
					0x28	// Le
				]);

			byte[] Response = await this.ExecuteCommand(Command);

			if (!this.CheckResponse(Response))
				return new KeyValuePair<byte[]?, byte[]?>(null, null);

			if (Response.Length != 42 || Response[40] != 0x90 || Response[41] != 0x00)
			{
				this.Error("Unexpected response received.");
				return new KeyValuePair<byte[]?, byte[]?>(null, null);
			}

			byte[] EIC = new byte[32];
			byte[] MIC = new byte[8];

			Buffer.BlockCopy(Response, 0, EIC, 0, 32);
			Buffer.BlockCopy(Response, 32, MIC, 0, 8);

			return new KeyValuePair<byte[]?, byte[]?>(EIC, MIC);
		}

		/// <summary>
		/// Authenticates the client with the travel document chip using the document information
		/// provided in the constructor.
		/// </summary>
		/// <returns>Authentication result.</returns>
		public async Task<AuthenticateResult> Authenticate()
		{
			// §4.2 1. https://www2023.icao.int/publications/Documents/9303_p11_cons_en.pdf

			byte[]? Data = await this.TryDownloadCardAccessForAuthentication();

			if (Data is not null &&
				ASN1.TryDecodeDer(this, Data, out object? CardAccess) &&
				await this.TryFindPaceProtocol(CardAccess))
			{
				if (this.encrypted)
					return AuthenticateResult.AlreadyEncrypted;   // TODO: Renegotiate session keys, see §9.8.2, ICAO 9303-11.

				// PACE
				// §4.2 3. https://www2023.icao.int/publications/Documents/9303_p11_cons_en.pdf

				if (this.HasSniffers)
					this.Information("PACE protocol " + this.protocol!.GetType().Name.Replace('_', '-') + " selected.");

				if (!await this.InitializePACE())
				{
					this.Error("Unable to initialize PACE protocol.");
					return AuthenticateResult.UnableToInitializePace;
				}
				else if (this.protocol is PaceEcdhProtocol EecProtocol)
					this.Information("PACE protocol initialized (" + EecProtocol.Curve?.CurveName + ").");
				else
					this.Information("PACE protocol initialized.");

				if (!await this.protocol!.Authenticate(this))
				{
					this.Error("Authentication unsuccessful.");
					return AuthenticateResult.UnableToAuthenticatePace;
				}
			}
			else
			{
				// BAC
				// §4.2 4. https://www.icao.int/sites/default/files/publications/DocSeries/9303_p11_cons_en.pdf

				this.Information("Attempting legacy BAC protocol.");

				// §4.3, §D.3, https://www.icao.int/sites/default/files/publications/DocSeries/9303_p11_cons_en.pdf

				byte[]? Challenge = await this.GetBacChallenge();   // RND.IC

				if (Challenge is null)
				{
					this.Error("Unable to get BAC challenge.");
					return AuthenticateResult.UnableToGetBacChallenge;
				}

				byte[] ChallengeResponse = CalcChallengeResponse3DES(this.documentInformation, Challenge);
				KeyValuePair<byte[]?, byte[]?> Result = await this.ExternalBacAuthenticate(ChallengeResponse);
				byte[]? EIC = Result.Key;	// E.IC
				byte[]? MIC = Result.Value;	// M.IC

				if (EIC is null || MIC is null)
				{
					this.Error("Unable to complete BAC authentication.");
					return AuthenticateResult.UnableToAuthenticateBac;
				}

				this.Error("BAC not implemented.");
				
				// TODO: Implement/Test BAC

				return AuthenticateResult.BacNotImplemented;
			}

			return AuthenticateResult.Success;
		}

		private async Task<byte[]?> TryDownloadCardAccessForAuthentication()
		{
			byte[]? Data = await this.DownloadFile(EF.CardAccess, "EF.CardAccess");
			if (Data is not null)
				return Data;

			if (this.encrypted)
				return Data;

			this.Information("Retrying EF.CardAccess after explicit master file selection.");
			if (!await this.SelectMaster())
			{
				this.Error("Unable to select the master file before reading EF.CardAccess.");
				return Data;
			}

			Data = await this.DownloadFile(EF.CardAccess, "EF.CardAccess");
			return Data;
		}

		/// <summary>
		/// Calculates a response to a BAC challenge using 3DES & SHA1.
		/// </summary>
		/// <param name="Challenge">Challenge</param>
		/// <param name="Rnd1">Random number 1</param>
		/// <param name="Rnd2">Random number 2</param>
		/// <param name="KEnc">Encryption Key</param>
		/// <param name="KMac">MAC Key</param>
		/// <returns>Response</returns>
		public static byte[] CalcChallengeResponse3DES(byte[] Challenge, byte[] Rnd1, byte[] Rnd2,
			byte[] KEnc, byte[] KMac)
		{
			byte[] S = CONCAT(Rnd1, Challenge, Rnd2);   // RND.IFD || RND.IC || K.IFD
			byte[] EIFD;
			byte[] MIFD;

			using (TripleDES Cipher = TripleDES.Create())
			{
				Cipher.Mode = CipherMode.CBC;
				Cipher.Padding = PaddingMode.None;

				using ICryptoTransform Encryptor = Cipher.CreateEncryptor(KEnc, new byte[8]);
				EIFD = Encryptor.TransformFinalBlock(S, 0, 32);
			}

			// MAC Algorithm described in ISO/IEC 9797-1
			// Ref: https://en.wikipedia.org/wiki/ISO/IEC_9797-1

			using (DES Cipher = DES.Create())
			{
				Cipher.Mode = CipherMode.CBC;
				Cipher.Padding = PaddingMode.None;

				int i = 0;
				int c = EIFD.Length;
				int j;

				byte[] Data = new byte[c + 8];
				Buffer.BlockCopy(EIFD, 0, Data, 0, c);
				Data[c] = 0x80;   // Padding method 2, append 80 00 00 00 00 00 00 00

				byte[] Ka = new byte[8];
				byte[] Kb = new byte[8];

				Buffer.BlockCopy(KMac, 0, Ka, 0, 8);
				Buffer.BlockCopy(KMac, 8, Kb, 0, 8);

				byte[] Block = new byte[8];
				byte[]? H = null;

				c += 8;
				using (ICryptoTransform Encryptor2 = Cipher.CreateEncryptor(Ka, new byte[8]))
				{
					while (i < c)
					{
						Buffer.BlockCopy(Data, i, Block, 0, 8);
						i += 8;

						if (H is not null)
						{
							for (j = 0; j < 8; j++)
								Block[j] ^= H[j];
						}

						H = Encryptor2.TransformFinalBlock(Block, 0, 8);
					}

					using (ICryptoTransform FinalDecryptor = Cipher.CreateDecryptor(Kb, new byte[8]))
					{
						H = FinalDecryptor.TransformFinalBlock(H!, 0, 8);
					}

					H = Encryptor2.TransformFinalBlock(H, 0, 8);
				}

				MIFD = H;
			}

			return CONCAT(EIFD, MIFD);
		}

		/// <summary>
		/// Calculates a response to a BAC challenge using 3DES & SHA1.
		/// </summary>
		/// <param name="Info">Document Information</param>
		/// <param name="Challenge">Challenge</param>
		/// <returns>Response</returns>
		public static byte[] CalcChallengeResponse3DES(DocumentInformation Info, byte[] Challenge)
		{
			byte[] Rnd1 = new byte[8];  // RND.IFD
			byte[] Rnd2 = new byte[16]; // K.IFD

			using (RandomNumberGenerator Rnd = RandomNumberGenerator.Create())
			{
				Rnd.GetBytes(Rnd1);
				Rnd.GetBytes(Rnd2);
			}

			return CalcChallengeResponse3DES(Challenge, Rnd1, Rnd2, BAC_KEnc(Info), BAC_KMac(Info));
		}

		/// <summary>
		/// Authenticates the application with the document, using Generic Mapping defined for
		/// the PACE protocol.
		/// </summary>
		/// <returns>true if authenticated, false if unable to authenticate with the document.</returns>
		internal async Task<bool> AuthenticateGenericMapping()
		{
			if (this.protocol is not PaceEcdhProtocol EcdhProtocol)
				return false;

			EllipticCurve? Curve = EcdhProtocol!.Curve;
			if (Curve is null)
				return false;

			try
			{
				// Encrypted Nonce

				byte[]? z = await this.GetPaceEncryptedNonce();
				if (z is null)
				{
					this.Error("Unable to get PACE encrypted nonce.");
					return false;
				}

				this.Information("Encrypted nonce: " + Hashes.BinaryToString(z));

				byte[] Kπ = this.protocol.KDFπ(this.documentInformation);
				byte[] s = this.protocol.DecryptNonce(Kπ, z);

				this.Information("Decrypted nonce: " + Hashes.BinaryToString(s));

				// Main keys

				byte[] LocalPublicKey;
				int KeyIndex = 0;

				IsoDepReplay? Replay = this.tagInterface as IsoDepReplay;

				// Creates a public key in big-endian format.
				LocalPublicKey = this.protocol.CreateNewKey(this.localKeySeed, ref KeyIndex);

				if (Replay is not null)
				{
					string LocalPrivateKey = Replay.GetInfo("Local private key:", this);
					XmlDocument Doc = new();
					Doc.LoadXml(LocalPrivateKey);

					byte[] LocalPublicKey2 = this.protocol.ImportKey(Doc);

					Curve = EcdhProtocol.Curve;
					if (Curve is null)
						return false;

					if (this.localKeySeed is null)
						LocalPublicKey = LocalPublicKey2;
					else if (Convert.ToBase64String(LocalPublicKey) != Convert.ToBase64String(LocalPublicKey2))
					{
						this.Error("Local public key mismatch.");
						return false;
					}
				}

				this.Information("Local public key: " + Hashes.BinaryToString(LocalPublicKey));
				this.Information("Local private key: " + Curve.Export());

				byte[]? RemotePublicKey = await this.GetPaceRemotePublicKey(LocalPublicKey);  // Big-endian format.

				if (RemotePublicKey is null)
				{
					this.Error("Unable to get PACE remote public key.");
					return false;
				}

				this.Information("Remote public key: " + Hashes.BinaryToString(RemotePublicKey));

				if (!Curve.IsPoint(RemotePublicKey, true))
				{
					this.Error("Remote public key not on curve.");
					return false;
				}

				// Shared Secret

				byte[] SharedSecret = this.protocol.GetSharedSecret(RemotePublicKey);

				this.Information("Shared secret: " + Hashes.BinaryToString(SharedSecret));

				// Map

				PointOnCurve Ĝ = EcdhProtocol.GetGenericMap(s, RemotePublicKey);
				byte[] Generator = Curve.Encode(Ĝ, true);

				this.Information("Generator Ĝ: " + Hashes.BinaryToString(Generator));

				// Ephemeral keys

				byte[] LocalEphemeralPrivateKey;

				if (Replay is not null)
				{
					string EphemeralKey = Replay.GetInfo("Local ephemeral private key:", this);
					LocalEphemeralPrivateKey = Hashes.StringToBinary(EphemeralKey);

					if (this.localKeySeed is not null)
					{
						byte[] LocalEphemeralPrivateKey2 = EcdhProtocol.GenerateSecret(this.localKeySeed, ref KeyIndex);

						if (Convert.ToBase64String(LocalEphemeralPrivateKey) != Convert.ToBase64String(LocalEphemeralPrivateKey2))
						{
							this.Error("Local ephemeral private key mismatch.");
							return false;
						}
					}
				}
				else
					LocalEphemeralPrivateKey = EcdhProtocol.GenerateSecret(this.localKeySeed, ref KeyIndex);

				this.Information("Local ephemeral private key: " + Hashes.BinaryToString(LocalEphemeralPrivateKey));

				PointOnCurve P1 = Curve.ScalarMultiplication(LocalEphemeralPrivateKey, Ĝ, true);
				byte[] LocalEphemeralPublicKey = Curve.Encode(P1, true);

				this.Information("Local ephemeral public key: " + Hashes.BinaryToString(LocalEphemeralPublicKey));

				byte[]? RemoteEphemeralPublicKey = await this.GetPaceRemotePublicEphemeralKey(LocalEphemeralPublicKey);

				if (RemoteEphemeralPublicKey is null)
				{
					this.Error("Unable to get PACE remote ephemeral public key.");
					return false;
				}

				this.Information("Remote ephemeral public key: " + Hashes.BinaryToString(RemoteEphemeralPublicKey));

				if (!Curve.IsPoint(RemoteEphemeralPublicKey, true))
				{
					this.Error("Remote ephemeral public key not on curve.");
					return false;
				}

				// Ephemeral shared secret

				int c = RemoteEphemeralPublicKey.Length;
				int c2 = c >> 1;
				byte[] RemoteEphemeralPublicKeyX = new byte[c2];
				byte[] RemoteEphemeralPublicKeyY = new byte[c2];

				Buffer.BlockCopy(RemoteEphemeralPublicKey, 0, RemoteEphemeralPublicKeyX, 0, c2);
				Buffer.BlockCopy(RemoteEphemeralPublicKey, c2, RemoteEphemeralPublicKeyY, 0, c2);

				Array.Reverse(RemoteEphemeralPublicKeyX);
				Array.Reverse(RemoteEphemeralPublicKeyY);

				PointOnCurve RemoteEphemeralPublicPoint = new(
					EllipticCurve.ToInt(RemoteEphemeralPublicKeyX),
					EllipticCurve.ToInt(RemoteEphemeralPublicKeyY));

				PointOnCurve EphemeralSharedPoint = Curve.ScalarMultiplication(
					LocalEphemeralPrivateKey, RemoteEphemeralPublicPoint, true);

				byte[] EphemeralSharedPointX = EphemeralSharedPoint.X.ToByteArray();    // Little-endian

				if (EphemeralSharedPointX.Length != Curve.OrderBytes)
					Array.Resize(ref EphemeralSharedPointX, Curve.OrderBytes);

				Array.Reverse(EphemeralSharedPointX);                                   // Big-endian

				this.Information("Ephemeral shared secret: " + Hashes.BinaryToString(EphemeralSharedPointX));

				// Session keys

				this.ks_Enc = this.protocol.KDF_Enc(EphemeralSharedPointX);
				this.ks_Mac = this.protocol.KDF_Mac(EphemeralSharedPointX);

				this.Information("KS_Enc: " + Hashes.BinaryToString(this.ks_Enc));
				this.Information("KS_Mac: " + Hashes.BinaryToString(this.ks_Mac));

				// Associated Data

				byte[] AD_IFD = PaceProtocol.CreateAssociatedData(this.protocol.Oid, RemoteEphemeralPublicKey);
				byte[] AD_IC = PaceProtocol.CreateAssociatedData(this.protocol.Oid, LocalEphemeralPublicKey);

				this.Information("AD_IFD: " + Hashes.BinaryToString(AD_IFD));
				this.Information("AD_IC: " + Hashes.BinaryToString(AD_IC));

				// Computing MAC

				this.cMac = this.protocol.GetAuthenticator(this.ks_Mac);

				byte[] T_IFD = this.cMac.Sign(AD_IFD, 8);

				this.Information("T_IFD: " + Hashes.BinaryToString(T_IFD));

				byte[]? RemoteToken = await this.GetPaceRemoteVerificationToken(T_IFD);

				if (RemoteToken is null)
				{
					this.Error("Unable to get remote token.");
					return false;
				}

				this.Information("Remote Token: " + Hashes.BinaryToString(RemoteToken));

				if (!this.cMac.Verify(AD_IC, RemoteToken))
				{
					byte[] T_IC = this.cMac.Sign(AD_IC, 8);

					this.Error("PACE token validation failed. Expected _IC: " + Hashes.BinaryToString(T_IC));
					return false;
				}

				this.Information("Authentication successful.");

				this.encrypted = true;
				this.enhancedSecurity = false;
				this.sendSequenceCounter = new byte[this.protocol.BlockLength];

				return true;
			}
			catch (Exception ex)
			{
				this.Exception(ex);
				return false;
			}
		}

		/// <summary>
		/// If Data Group 1 should be read (MRZ). Default=true
		/// </summary>
		public bool ReadDG1 { get; set; } = true;

		/// <summary>
		/// If Data Group 2 should be read (Encoded Identification Features — Face). Default=true
		/// </summary>
		public bool ReadDG2 { get; set; } = true;

		/// <summary>
		/// If Data Group 3 should be read (Additional Identification Feature — Finger(s)). Default=false
		/// </summary>
		public bool ReadDG3 { get; set; } = false;

		/// <summary>
		/// If Data Group 4 should be read (Additional Identification Feature — Finger(s)). Default=false
		/// </summary>
		public bool ReadDG4 { get; set; } = false;

		/// <summary>
		/// If Data Group 5 should be read (Displayed Portrait). Default=false
		/// </summary>
		public bool ReadDG5 { get; set; } = false;

		/// <summary>
		/// If Data Group 7 should be read (Displayed Signature or Usual Mark). Default=false
		/// </summary>
		public bool ReadDG7 { get; set; } = false;

		/// <summary>
		/// If Data Group 8 should be read (Data Feature(s)). Default=false
		/// </summary>
		public bool ReadDG8 { get; set; } = false;

		/// <summary>
		/// If Data Group 9 should be read (Structure Feature(s)). Default=false
		/// </summary>
		public bool ReadDG9 { get; set; } = false;

		/// <summary>
		/// If Data Group 10 should be read (Substance Feature(s)). Default=false
		/// </summary>
		public bool ReadDG10 { get; set; } = false;

		/// <summary>
		/// If Data Group 11 should be read (Additional Personal Detail(s)). Default=true
		/// </summary>
		public bool ReadDG11 { get; set; } = true;

		/// <summary>
		/// If Data Group 12 should be read (Additional Document Detail(s)). Default=false
		/// </summary>
		public bool ReadDG12 { get; set; } = false;

		/// <summary>
		/// If Data Group 13 should be read (Optional Details(s)). Default=false
		/// </summary>
		public bool ReadDG13 { get; set; } = false;

		/// <summary>
		/// If Data Group 14 should be read (Security Options). Default=false
		/// </summary>
		public bool ReadDG14 { get; set; } = false;

		/// <summary>
		/// If Data Group 15 should be read (Active Authentication Public Key Info). Default=false
		/// </summary>
		public bool ReadDG15 { get; set; } = false;

		/// <summary>
		/// If Data Group 16 should be read (Person(s) to Notify). Default=false
		/// </summary>
		public bool ReadDG16 { get; set; } = false;

		/// <summary>
		/// Reads the travel document.
		/// </summary>
		/// <param name="IdDomain">Domain name of Neuron hosting ICAO certificates.</param>
		/// <returns>Result of procedure.</returns>
		public async Task<ReadTravelDocumentResult> ReadTravelDocument(string IdDomain)
		{
			if (!await this.SelectApplication(Applications.DF1))
			{
				this.Error("Unable to select the LDS1 eMRTD application.");
				return ReadTravelDocumentResult.Lds1ApplicationNotFound;
			}

			// Reading EF.COM

			this.Information("LDS1 eMRTD application selected.");

			byte[]? Data = await this.DownloadFile(EF.COM, "EF.COM");
			if (Data is null)
			{
				this.Error("Unable to download EF.COM.");
				return ReadTravelDocumentResult.UnableToReadEfCom;
			}

			if (!TryParseDataObject(Data, this, out ApplicationLevelInformation? AppInfo))
			{
				this.Error("Unable to parse application level information.");
				return ReadTravelDocumentResult.UnableToParseEfCom;
			}

			this.appInfo = AppInfo;
			await this.AppInfoUpdated.Raise(this, EventArgs.Empty);

			// Reading EF.SOD, §4.6.2 ICAO 9303-10

			Data = await this.DownloadFile(EF.SOD, "EF.SOD");
			if (Data is null)
			{
				this.Error("Unable to download EF.SOD.");
				return ReadTravelDocumentResult.UnableToReadEfSod;
			}

			if (!TryParseDataObject(Data, this, out DocumentSecurityObject? SecurityInfo))
			{
				this.Error("Unable to decode Document Security Object.\r\n\r\n" +
					Convert.ToBase64String(Data, Base64FormattingOptions.InsertLineBreaks));
				return ReadTravelDocumentResult.UnableToParseEfSod;
			}

			if ((SecurityInfo.SignedData?.Certificates?.Length ?? 0) == 0)
			{
				this.Error("No certificates available in EF.SOD.");
				return ReadTravelDocumentResult.NoCertificates;
			}

			if (SecurityInfo.SignedData!.Certificates.Length > 1)
			{
				this.Error("Multiple certificates available in EF.SOD.");
				return ReadTravelDocumentResult.MultipleCertificates;
			}

			// Validating chip certificate to ensure valid issuer.

			this.Information("Validating certificate.");
			await this.SetState(TravelDocumentsState.ValidatingCertificate);

			foreach (Certificate Cert in SecurityInfo.SignedData!.Certificates)
			{
				ChunkedList<Certificate> Certificates = [Cert];
				Dictionary<string, bool> CrlUrls = [];

				foreach (string CrlUrl in GetRevocationListUrls(Cert))
					CrlUrls[CrlUrl] = true;

				KeyValuePair<string?, byte[]?> P = GetAuthorityKeyIdentifier(Cert);
				Dictionary<string, bool> Processed = [];
				string? CountryCode = P.Key;
				byte[]? IssuerKeyReference = P.Value;

				if (string.IsNullOrEmpty(CountryCode) || IssuerKeyReference is null)
				{
					this.Error("Required Authority Key Identifier not found in certificate.");
					return ReadTravelDocumentResult.InvalidCertificate;
				}

				while (!string.IsNullOrEmpty(CountryCode) && IssuerKeyReference is not null)
				{
					string Key = Convert.ToBase64String(IssuerKeyReference);
					if (Processed.ContainsKey(Key))
						break;

					Processed[Key] = true;

					this.Information("Retrieving issuer certificate: " + Hashes.BinaryToString(IssuerKeyReference));

					Certificate? IssuerCertificate = await CertificateStore.TryLoadCertificate(
						 IdDomain, CountryCode, IssuerKeyReference, this);

					if (IssuerCertificate is null)
					{
						this.Error("Issuer certificate not found.");
						return ReadTravelDocumentResult.InvalidCertificate;
					}

					Certificates.Insert(0, IssuerCertificate);

					// Make sure to use Certificate Revocation Lists (CRLs) from ICAO approved certificates.

					foreach (string CrlUrl in GetRevocationListUrls(IssuerCertificate))
						CrlUrls[CrlUrl] = true;

					P = GetAuthorityKeyIdentifier(IssuerCertificate);
					CountryCode = P.Key;
					IssuerKeyReference = P.Value;
				}

				if (CrlUrls.Count == 0)
				{
					this.Error("No approved CRLs found.");
					return ReadTravelDocumentResult.InvalidCertificate;
				}

				foreach (string CrlUrl in CrlUrls.Keys)
				{
					this.Information("Retrieving CRL: " + CrlUrl);

					CertificateList? RevokedCertificates = await CertificateStore.TryLoadCrl(CrlUrl, this);
					if (RevokedCertificates is null)
					{
						this.Error("Unable to load CRL.");
						return ReadTravelDocumentResult.InvalidCertificate;
					}

					this.Information("Verifying CRL signature.");

					if (!await RevokedCertificates.VerifySignature(IdDomain, CountryCode!, this))
					{
						this.Error("CRL Signature invalid.");
						return ReadTravelDocumentResult.InvalidCertificate;
					}

					this.Information("Checking if certificates are revoked.");

					if (RevokedCertificates.HasBeenRevoked(Cert, out RevokedReason Reason))
					{
						this.Error("Certificate " + Cert.SerialNumber.ToString("X", CultureInfo.InvariantCulture) + " has been revoked: " + Reason.ToString());
						return ReadTravelDocumentResult.InvalidCertificate;
					}

					foreach (Certificate Certificate2 in Certificates)
					{
						if (RevokedCertificates.HasBeenRevoked(Certificate2, out Reason))
						{
							this.Error("Certificate " + Certificate2.SerialNumber.ToString("X", CultureInfo.InvariantCulture) + " has been revoked: " + Reason.ToString());
							return ReadTravelDocumentResult.InvalidCertificate;
						}
					}
				}

				this.Information("Verifying certificate chain.");

				if (!CertificateChain.VerifySignatures(this, [.. Certificates]))
				{
					this.Error("Signatures in certificate chain not valid.");
					return ReadTravelDocumentResult.InvalidCertificate;
				}
			}

			this.securityinfo = SecurityInfo;
			await this.SecurityInfoUpdated.Raise(this, EventArgs.Empty);

			if (this.ReadDG1 && (this.appInfo.TagList?.HasDataGroup(1) ?? false))
			{
				// Reading EF.DG1 (MRZ), §4.7.1 ICAO 9303-10

				this.Information("EF.DG1 (MRZ) supported.");

				Data = await this.DownloadFile(EF.DG1, "EF.DG1");
				if (Data is null)
				{
					this.Error("Unable to download EF.DG1.");
					return ReadTravelDocumentResult.UnableToReadEfDg;
				}

				if (!this.ValidateDataGroupData(1, Data))
					return ReadTravelDocumentResult.DgHashDigestInvalid;

				if (!TryParseDataObject(Data, this, out MachineReadableZoneInformation? DataGroup1) ||
					DataGroup1.Mrz is null)
				{
					this.Error("Unable to decode DG1 (MRZ Information).\r\n\r\n" +
						Convert.ToBase64String(Data, Base64FormattingOptions.InsertLineBreaks));
					return ReadTravelDocumentResult.UnableToParseEfDg;
				}

				this.mrz = DataGroup1.Mrz;
				if (this.mrz.DocumentInformation is null)
					this.Warning("Unable to parse MRZ information.");

				await this.MrzUpdated.Raise(this, EventArgs.Empty);
			}

			if (this.ReadDG2 && (this.appInfo.TagList?.HasDataGroup(2) ?? false))
			{
				// Reading EF.DG2 (Encoded Identification Features — Face), §4.7.2 ICAO 9303-10

				this.Information("EF.DG2 (Encoded Identification Features — Face) supported.");

				Data = await this.DownloadFile(EF.DG2, "EF.DG2");
				if (Data is null)
				{
					this.Error("Unable to download EF.DG2.");
					return ReadTravelDocumentResult.UnableToReadEfDg;
				}

				if (!this.ValidateDataGroupData(2, Data))
					return ReadTravelDocumentResult.DgHashDigestInvalid;

				if (!TryParseDataObject(Data, this, out BiometricEncodingFace? BiometricEncoding))
				{
					this.Error("Unable to decode Biometric Encoding in DG2 (Encoded Identification Features — Face).\r\n\r\n" +
						Convert.ToBase64String(Data, Base64FormattingOptions.InsertLineBreaks));
					return ReadTravelDocumentResult.UnableToParseEfDg;
				}

				this.biometricEncodingFace = BiometricEncoding.Templates?.Templates;
				await this.BiometricEncodingFaceUpdated.Raise(this, EventArgs.Empty);
			}

			if (this.enhancedSecurity && this.ReadDG3 && (this.appInfo.TagList?.HasDataGroup(3) ?? false))
			{
				try
				{
					// Reading EF.DG3 (Additional Identification Feature — Finger(s)), §4.7.3 ICAO 9303-10

					this.Information("EF.DG3 (Additional Identification Feature — Finger(s)) supported.");

					Data = await this.DownloadFile(EF.DG3, "EF.DG3");
					if (Data is null)
					{
						this.Error("Unable to download EF.DG3.");
						return ReadTravelDocumentResult.UnableToReadEfDg;
					}

					if (!this.ValidateDataGroupData(3, Data))
						return ReadTravelDocumentResult.DgHashDigestInvalid;

					if (!TryParseDataObject(Data, this, out BiometricEncodingFingers? BiometricEncoding))
					{
						this.Error("Unable to decode Biometric Encoding in DG3 (Additional Identification Feature — Finger(s)).\r\n\r\n" +
							Convert.ToBase64String(Data, Base64FormattingOptions.InsertLineBreaks));
						return ReadTravelDocumentResult.UnableToParseEfDg;
					}

					this.biometricEncodingFingers = BiometricEncoding.Templates?.Templates;
					await this.BiometricEncodingFingersUpdated.Raise(this, EventArgs.Empty);
				}
				catch (Exception ex)
				{
					this.Error(ex.Message); // Access to DG3 might be restricted. Just log an error.
				}
			}

			if (this.enhancedSecurity && this.ReadDG4 && (this.appInfo.TagList?.HasDataGroup(4) ?? false))
			{
				try
				{
					// Reading EF.DG4 (Additional Identification Feature — Finger(s)), §4.7.3 ICAO 9303-10

					this.Information("EF.DG4 (Additional Identification Feature — Iris(es)) supported.");

					Data = await this.DownloadFile(EF.DG4, "EF.DG4");
					if (Data is null)
					{
						this.Error("Unable to download EF.DG4.");
						return ReadTravelDocumentResult.UnableToReadEfDg;
					}

					if (!this.ValidateDataGroupData(4, Data))
						return ReadTravelDocumentResult.DgHashDigestInvalid;

					if (!TryParseDataObject(Data, this, out BiometricEncodingIrises? BiometricEncoding))
					{
						this.Error("Unable to decode Biometric Encoding in DG4 (Additional Identification Feature — Iris(es)).\r\n\r\n" +
							Convert.ToBase64String(Data, Base64FormattingOptions.InsertLineBreaks));
						return ReadTravelDocumentResult.UnableToParseEfDg;
					}

					this.biometricEncodingIrises = BiometricEncoding.Templates?.Templates;
					await this.BiometricEncodingIrisesUpdated.Raise(this, EventArgs.Empty);
				}
				catch (Exception ex)
				{
					this.Error(ex.Message); // Access to DG3 might be restricted. Just log an error.
				}
			}

			if (this.ReadDG5 && (this.appInfo.TagList?.HasDataGroup(5) ?? false))
			{
				// Reading EF.DG5 (Displayed Portrait), §4.7.5 ICAO 9303-10

				this.Information("EF.DG5 (Displayed Portrait) supported.");

				Data = await this.DownloadFile(EF.DG5, "EF.DG5");
				if (Data is null)
				{
					this.Error("Unable to download EF.DG5.");
					return ReadTravelDocumentResult.UnableToReadEfDg;
				}

				if (!this.ValidateDataGroupData(5, Data))
					return ReadTravelDocumentResult.DgHashDigestInvalid;

				if (!TryParseDataObject(Data, this, out DisplayedPortraits? DataGroup5))
				{
					this.Error("Unable to decode DG5 (Displayed Portrait).\r\n\r\n" +
						Convert.ToBase64String(Data, Base64FormattingOptions.InsertLineBreaks));
					return ReadTravelDocumentResult.UnableToParseEfDg;
				}

				if ((DataGroup5?.Photos?.Length ?? 0) > 0)
				{
					foreach (DisplayedPortrait Photo in DataGroup5!.Photos!)
						this.Warning(Convert.ToBase64String(Photo.Value));
				}
			}

			if (this.ReadDG7 && (this.appInfo.TagList?.HasDataGroup(7) ?? false))
			{
				// Reading EF.DG7 (Displayed Signature or Usual Mark), §4.7.2 ICAO 9303-10

				this.Information("EF.DG7 (Displayed Signature or Usual Mark) supported.");

				Data = await this.DownloadFile(EF.DG7, "EF.DG7");
				if (Data is null)
				{
					this.Error("Unable to download EF.DG7.");
					return ReadTravelDocumentResult.UnableToReadEfDg;
				}

				if (!this.ValidateDataGroupData(7, Data))
					return ReadTravelDocumentResult.DgHashDigestInvalid;

				if (!TryParseDataObject(Data, this, out DisplayedSignatures? DisplayedSignatures))
				{
					this.Error("Unable to decode Displayed Signatures in DG7 (Displayed Signature or Usual Mark).\r\n\r\n" +
						Convert.ToBase64String(Data, Base64FormattingOptions.InsertLineBreaks));
					return ReadTravelDocumentResult.UnableToParseEfDg;
				}

				this.displayedSignatures = DisplayedSignatures;
				await this.DisplayedSignaturesUpdated.Raise(this, EventArgs.Empty);
			}

			if (this.ReadDG8 && (this.appInfo.TagList?.HasDataGroup(8) ?? false))
			{
				this.Warning("EF.DG8 (Data Feature(s)) supported but not implemented.");

				// TODO: Data Group 8 (Data Feature(s)) (In LDS1 eMRTD Application)
			}

			if (this.ReadDG9 && (this.appInfo.TagList?.HasDataGroup(9) ?? false))
			{
				this.Warning("EF.DG9 (Structure Feature(s)) supported but not implemented.");

				// TODO: Data Group 9 (Structure Feature(s)) (In LDS1 eMRTD Application)
			}

			if (this.ReadDG10 && (this.appInfo.TagList?.HasDataGroup(10) ?? false))
			{
				this.Warning("EF.DG10 (Substance Feature(s)) supported but not implemented.");

				// TODO: Data Group 10 (Substance Feature(s)) (In LDS1 eMRTD Application)
			}

			if (this.ReadDG11 && (this.appInfo.TagList?.HasDataGroup(11) ?? false))
			{
				// Reading EF.DG11 (Additional Personal Detail(s)), §4.7.11 ICAO 9303-10

				this.Information("EF.DG11 (Additional Personal Detail(s)) supported.");

				Data = await this.DownloadFile(EF.DG11, "EF.DG11");
				if (Data is null)
				{
					this.Error("Unable to download EF.DG11.");
					return ReadTravelDocumentResult.UnableToReadEfDg;
				}

				if (!this.ValidateDataGroupData(11, Data))
					return ReadTravelDocumentResult.DgHashDigestInvalid;

				if (!TryParseDataObject(Data, this, out AdditionalPersonalDetails? AdditionalPersonalDetails))
				{
					this.Error("Unable to decode DG11 (Additional Personal Detail(s)).\r\n\r\n" +
						Convert.ToBase64String(Data, Base64FormattingOptions.InsertLineBreaks));
					return ReadTravelDocumentResult.UnableToParseEfDg;
				}

				this.personalInformation = AdditionalPersonalDetails;
				await this.PersonalInformationUpdated.Raise(this, EventArgs.Empty);
			}

			if (this.ReadDG12 && (this.appInfo.TagList?.HasDataGroup(12) ?? false))
			{
				this.Warning("EF.DG12 (Additional Document Detail(s)) supported but not implemented.");

				// TODO: Data Group 12 (Additional Document Detail(s)) (In LDS1 eMRTD Application)
			}

			if (this.ReadDG13 && (this.appInfo.TagList?.HasDataGroup(13) ?? false))
			{
				this.Warning("EF.DG13 (Optional Details(s)) supported but not implemented.");

				// TODO: Data Group 13 (Optional Details(s)) (In LDS1 eMRTD Application)
			}

			if (this.ReadDG14 && (this.appInfo.TagList?.HasDataGroup(14) ?? false))
			{
				this.Warning("EF.DG14 (Security Options) supported.");

				// TODO: Data Group 14 (Security Options) (In LDS1 eMRTD Application)
			}

			if (this.ReadDG15 && (this.appInfo.TagList?.HasDataGroup(15) ?? false))
			{
				this.Warning("EF.DG15 (Active Authentication Public Key Info) supported but not implemented.");

				// TODO: Data Group 15 (Active Authentication Public Key Info) (In LDS1 eMRTD Application)
			}

			if (this.ReadDG16 && (this.appInfo.TagList?.HasDataGroup(16) ?? false))
			{
				this.Warning("EF.DG16 (Person(s) to Notify) supported but not implemented.");

				// TODO: Data Group 16 (Person(s) to Notify) (In LDS1 eMRTD Application)
			}

			await this.SetState(TravelDocumentsState.Idle);

			return ReadTravelDocumentResult.Success;
		}

		private bool ValidateDataGroupData(int Nr, byte[] Data)
		{
			this.Information("Validating data with EF.SOD");

			if (this.securityinfo is null)
			{
				this.Error("EF.SOD not read.");
				return false;
			}
			else if (this.securityinfo.ValidateDataGroup(Nr, Data))
			{
				this.Information("Data valid in accordance to Hash Digest in EF.SOD.");
				return true;
			}
			else
			{
				this.Error("Invalid data. Hash Digest of data does not match Hash Digest in EF.SOD.");
				return false;
			}
		}

		/// <summary>
		/// Parses a specific TLV-encoded data object.
		/// </summary>
		/// <param name="Data">Binary data.</param>
		/// <param name="Client">Client parsing the information.</param>
		/// <param name="DataObject">Parsed data object, if successful.</param>
		/// <returns>If parsing was successful.</returns>
		public static bool TryParseDataObject<T>(byte[] Data, TravelDocumentsClient Client,
			[NotNullWhen(true)] out T? DataObject)
			where T : IDataObject
		{
			if (TryParseDataObjects(Data, Client, out IDataObject[]? DataObjects))
			{
				foreach (IDataObject Object in DataObjects)
				{
					if (Object is T TypedObject)
					{
						DataObject = TypedObject;
						return true;
					}
				}
			}

			DataObject = default;
			return false;
		}

		/// <summary>
		/// Parses TLV-encoded data objects.
		/// </summary>
		/// <param name="Data">Binary data.</param>
		/// <param name="Client">Client parsing the information.</param>
		/// <param name="DataObjects">Array of data objects, or null if not correctly encoded.</param>
		/// <returns>If parsing was successful.</returns>
		public static bool TryParseDataObjects(byte[] Data, TravelDocumentsClient Client,
			[NotNullWhen(true)] out IDataObject[]? DataObjects)
		{
			DataObjects = null;

			ChunkedList<IDataObject> Found = [];
			int i = 0;
			int c = Data.Length;
			ushort Tag;
			ushort Len;
			byte[] Value;

			while (i < c)
			{
				Tag = Data[i++];

				if (Tag == 0x80)
				{
					int j;

					for (j = i; j < c; j++)
					{
						if (Data[j] != 0)
							break;
					}

					if (j == c)
						break;  // Padding
				}

				if ((Tag & 31) == 31)
				{
					if (i == c)
						return false;

					Tag <<= 8;
					Tag |= Data[i++];
				}

				if (i == c)
					return false;

				Len = Data[i++];

				switch (Len)
				{
					case 0x81:
						if (i == c)
							return false;

						Len = Data[i++];
						break;

					case 0x82:
						if (i + 1 >= c)
							return false;

						Len = Data[i++];
						Len <<= 8;
						Len |= Data[i++];
						break;

					default:
						Len &= 0x7f;
						break;
				}

				if (i + Len > c)
					return false;

				Value = new byte[Len];
				if (Len > 0)
				{
					Buffer.BlockCopy(Data, i, Value, 0, Len);
					i += Len;
				}

				if (Tag == 0 && Len == 0)
					break;
				else if (dataObjects.TryGetValue(Tag, out IDataObject? TypedObject))
				{
					if (TypedObject.TryParse(Value, Client, out IDataObject? ParsedObject))
						Found.Add(ParsedObject);
					else
					{
						Client.Warning("Unable to parse data object with tag: " + Tag.ToString("X4", CultureInfo.InvariantCulture));
						Found.Add(new BinaryDataObject(Tag, Value));
					}
				}
				else
				{
					Client.Warning("Unknown application level information tag: " + Tag.ToString("X4", CultureInfo.InvariantCulture));
					Found.Add(new BinaryDataObject(Tag, Value));
				}
			}

			DataObjects = [.. Found];

			return true;
		}

		private static Dictionary<ushort, IDataObject> GetDataObjects()
		{
			Dictionary<ushort, IDataObject> Result = [];

			foreach (Type T in Types.GetTypesImplementingInterface(typeof(IDataObject)))
			{
				ConstructorInfo? CI = Types.GetDefaultConstructor(T);
				if (CI is null)
					continue;

				try
				{
					IDataObject DO = (IDataObject)CI.Invoke(Types.NoParameters);
					Result[DO.Tag] = DO;
				}
				catch (Exception ex)
				{
					Log.Exception(ex);
				}
			}

			return Result;
		}

		/// <summary>
		/// Gets the Subject Key Identifier from a certificate, with associated country code, if
		/// available.
		/// </summary>
		/// <param name="Certificate">Certificate</param>
		/// <returns>Country code and subject key identifier, if found.</returns>
		public static KeyValuePair<string?, byte[]?> GetSubjectKeyIdentifier(Certificate Certificate)
		{
			string CountryCode = Certificate.Subject.CountryName;

			foreach (object Extension in Certificate.Extensions?.Elements ?? Array.Empty<object>())
			{
				if (Extension is SubjectKeyIdentifier SubjectKeyIdentifier &&
					SubjectKeyIdentifier.Identifier.Length > 0)
				{
					return new KeyValuePair<string?, byte[]?>(CountryCode,
						SubjectKeyIdentifier.Identifier);
				}
			}

			return new KeyValuePair<string?, byte[]?>(null, null);
		}

		/// <summary>
		/// Gets the Authority Key Identifier from a certificate, with associated country code, if
		/// available. This is used to determine the trust chain of the document's chip certificate,
		/// which is used to verify the authenticity of the document.
		/// </summary>
		/// <param name="Certificate">Certificate</param>
		/// <returns>Country code and authority key identifier, if found.</returns>
		public static KeyValuePair<string?, byte[]?> GetAuthorityKeyIdentifier(Certificate Certificate)
		{
			string CountryCode = Certificate.Issuer.CountryName;

			foreach (object Extension in Certificate.Extensions?.Elements ?? Array.Empty<object>())
			{
				if (Extension is AuthorityKeyIdentifier AuthorityKeyIdentifier &&
					AuthorityKeyIdentifier.Identifier.Length > 0)
				{
					return new KeyValuePair<string?, byte[]?>(CountryCode,
						AuthorityKeyIdentifier.Identifier);
				}
			}

			return new KeyValuePair<string?, byte[]?>(null, null);
		}

		/// <summary>
		/// Gets the Revocation List URL from the certificate.
		/// </summary>
		/// <param name="Certificate">Certificate</param>
		/// <returns>URL to Revocation List, if found.</returns>
		public static string[] GetRevocationListUrls(Certificate Certificate)
		{
			return GetRevocationListUrls(null, Certificate);
		}

		/// <summary>
		/// Gets the Revocation List URL from the certificate.
		/// </summary>
		/// <param name="Client">Optional client reference.</param>
		/// <param name="Certificate">Certificate</param>
		/// <returns>URL to Revocation List, if found.</returns>
		public static string[] GetRevocationListUrls(ICommunicationLayer? Client, Certificate Certificate)
		{
			ChunkedList<string> Urls = [];

			foreach (object Extension in Certificate.Extensions?.Elements ?? Array.Empty<object>())
			{
				if (Extension is not DistributionPoints DistributionPoints)
					continue;

				foreach (DistributionPoint Point in DistributionPoints.Points)
					Urls.Add(Point.Url);
			}

			if (Urls.Count == 0)
			{
				Client?.Warning("No CRL distribution points found in certificate.");
				return [];
			}

			return [.. Urls];
		}
	}
}
