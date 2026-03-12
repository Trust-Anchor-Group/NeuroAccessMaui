using System;
using System.Collections;
using System.Collections.Generic;
using System.Formats.Asn1;
using System.IO;
using System.Security.Cryptography;
using System.Threading.Tasks;
using NeuroAccess.Nfc.TravelDocuments.Events;
using NeuroAccess.Nfc.TravelDocuments.PACE;
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
	public class TravelDocumentsClient : CommunicationLayer
	{
		private readonly IIsoDepInterface tagInterface;
		private readonly DocumentInformation documentInformation;
		private TravelDocumentsState state;

		/// <summary>
		/// Travel Documents Client, implementing ICAO 9303 to communicate with machine-readable
		/// travel documents, such as passports, visas, and identity cards.
		/// </summary>
		/// <param name="TagInterface">NFC Interface</param>
		/// <param name="DocumentInformation">Document Information parsed from the MRZ.</param>
		/// <param name="Sniffers">Optional sniffers.</param>
		public TravelDocumentsClient(IIsoDepInterface TagInterface,
			DocumentInformation DocumentInformation, params ISniffer[] Sniffers)
			: base(true, Sniffers)
		{
			this.tagInterface = TagInterface;
			this.documentInformation = DocumentInformation;
			this.state = TravelDocumentsState.Detected;
		}

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
		/// Selects a file
		/// </summary>
		/// <param name="FileId">File to select.</param>
		/// <returns>If file was selected.</returns>
		public async Task<bool> SelectFile(ushort FileId)
		{
			// Ref §3.6.2, ISOC 9303-10: https://www2023.icao.int/publications/Documents/9303_p10_cons_en.pdf

			await this.SetState(TravelDocumentsState.SelectingFile, FileId);

			if (this.HasSniffers)
				this.Information("SelectFile(" + FileId.ToString("X4") + ")");

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

			byte[] Response = await this.tagInterface.ExecuteCommand(Command, this);

			return this.CheckResponse(Response);
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
					this.Information(SW2.ToString() + " bytes still available");
					return true;

				case Iso7816StatusCategory.WarningUnchanged:
					switch (SW2)
					{
						case 0:
							this.Warning("Warning, state unchanged. No information given.");
							break;

						default:
							this.Warning("Warning " + SW2.ToString("X2") + " triggered by card. State unchanged.");
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
							this.Warning("Warning " + SW2.ToString("X2") + " triggered by card. State changed.");
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
							this.Error("Error " + SW2.ToString("X2") + " triggered by card. State unchanged.");
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
							this.Error("Error " + SW2.ToString("X2") + " triggered by card. State changed.");
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
							this.Error("Function Not Supported " + SW2.ToString("X2") + " triggered by card.");
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
							this.Error("Not Allowed " + SW2.ToString("X2") + " triggered by card.");
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
							this.Error("Wrong Parameters " + SW2.ToString("X2") + " triggered by card.");
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
					this.Error("Le field incorrect. Should be " + SW2.ToString("X2"));
					return false;

				default:
					this.Error("Unexpected response received. SW1=" + SW1.ToString("X2") +
						", SW2=" + SW2.ToString("X2"));
					return false;
			}
		}

		/// <summary>
		/// Reads binary information from the currently selected file.
		/// </summary>
		/// <returns>Read data, or null if an error occurred.</returns>
		public Task<KeyValuePair<byte[]?, bool>> ReadBinary(ushort Offset)
		{
			return this.ReadBinary(Offset, 0);
		}

		/// <summary>
		/// Reads binary information from the currently selected file.
		/// </summary>
		/// <param name="NrBytes">Number of bytes to read.</param>
		/// <returns>Read data, or null if an error occurred.</returns>
		public async Task<KeyValuePair<byte[]?, bool>> ReadBinary(ushort Offset, byte NrBytes)
		{
			// Ref §3.6.3, ISOC 9303-10: https://www2023.icao.int/publications/Documents/9303_p10_cons_en.pdf

			await this.SetState(TravelDocumentsState.ReadingBinary, Offset);

			if (this.HasSniffers)
				this.Information("ReadBinary(" + Offset.ToString("X4") + "," + NrBytes.ToString("X2") + ")");

			byte[] Command =
			[
				ISO_7816.Classes.Basic,
				ISO_7816.Instructions.ReadBinary,
				(byte)(Offset >> 8),	// P1
				(byte)Offset,			// P2
				NrBytes					// Le
			];

			byte[] Response = await this.tagInterface.ExecuteCommand(Command, this);
			int c = Response.Length;

			if (!this.CheckResponse(Response))
			{
				if (Response is not null &&
					c >= 2 &&
					Response[^2] == (byte)Iso7816StatusCategory.WrongLeField)
				{
					Command[4] = Response[^1];
					Response = await this.tagInterface.ExecuteCommand(Command, this);

					if (!this.CheckResponse(Response))
						return new KeyValuePair<byte[]?, bool>(null, false);
				}
				else
					return new KeyValuePair<byte[]?, bool>(null, false);
			}

			bool More = Response[^1] == (byte)Iso7816StatusCategory.DataStillAvailable;
			byte[] Data = new byte[c - 2];
			Buffer.BlockCopy(Response, 0, Data, 0, c - 2);

			return new KeyValuePair<byte[]?, bool>(Data, More);
		}

		/// <summary>
		/// Selects and downloads a file from the travel document.
		/// </summary>
		/// <param name="FileId">File to download.</param>
		/// <returns>Downloaded file, or null if unable to download file.</returns>
		public async Task<byte[]?> DownloadFile(ushort FileId)
		{
			if (!await this.SelectFile(FileId))
				return null;

			using MemoryStream File = new();
			ushort Offset = 0;

			while (true)
			{
				KeyValuePair<byte[]?, bool> P = await this.ReadBinary(Offset);
				if (P.Key is null)
					return null;

				File.Write(P.Key, 0, P.Key.Length);
				if (!P.Value)
					return File.ToArray();

				ushort Offset2 = (ushort)(Offset + P.Key.Length);
				if (Offset2 < Offset)
					return null;

				Offset = Offset2;
			}
		}

		/// <summary>
		/// Tries to fins a PACE protocol matching information available in the EF.CardAccess file.
		/// </summary>
		/// <param name="CardAccess">Contents of EF.CardAccess file.</param>
		/// <param name="Protocol">Best protocol found.</param>
		/// <returns>Best protocol found, or null if no implementation found matching available options in the EF.CardAccess file.</returns>
		private async Task<IPaceProtocol?> TryFindPaceProtocol(object? CardAccess)
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

			if (CardAccess is not Array SecurityInfos)
				return null;

			ChunkedList<string> OidsFound = [];
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

				OidsFound.Add(Oid);

				Current = Types.FindBest<IPaceProtocol, string>(Oid);
				if (Current is null)
				{
					if (this.HasSniffers)
						this.Information("OID " + Oid + " lacks implemented support.");

					continue;
				}

				if (this.HasSniffers)
					this.Information("OID " + Oid + " (" + Current.GetType().Name.Replace('_', '-') + ") supported.");

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

			if (Best is null || OidsFound.HasFirstItem)
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

			return Best;
		}

		/// <summary>
		/// Initializes PACE authentication.
		/// </summary>
		/// <param name="Protocol">Selected cipher protocol to use.</param>
		/// <returns>If successful.</returns>
		private async Task<bool> InitializePACE(IPaceProtocol Protocol)
		{
			await this.SetState(TravelDocumentsState.SelectingCipher);

			if (this.HasSniffers)
				this.Information("MSE:Set AT(" + Protocol.Oid + ",MRZ)");

			string[] Parts = Protocol.Oid.Split('.');
			int i, c = Parts.Length - 1;
			byte[] PartBytes = new byte[c];

			for (i = 0; i < c; i++)
			{
				if (!byte.TryParse(Parts[i + 1], out PartBytes[i]))     // Skip first 0.
					return false;
			}

			byte[] Command = CONCAT(
				[
					ISO_7816.Classes.Basic,
					ISO_7816.Instructions.MessageSecurityEnvironment,
					0xC1,			// P1 - Set
					0xA4,			// P2 - PACE
					(byte)(5 + c),	// Le
					0x80,			// Algorithm reference
					(byte)c			// OID Length (excluding first zero)
				],
				[
					PartBytes,
					[
						0x83,		// Key reference
						0x01,		// Key reference length
						0x01		// MRZ key reference (0x02 = CAN, 0x03 = PIN, 0x04 = PUK)
					]
				]);

			byte[] Response = await this.tagInterface.ExecuteCommand(Command, this);

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
		/// Seed for computing cryptographic keys (§D.2)
		/// </summary>
		/// <param name="Info">Document Information</param>
		public static byte[] PACE_K(DocumentInformation Info)
		{
			byte[] Data = InternetContent.ISO_8859_1.GetBytes(Info.MRZ_Information);
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
			byte[] Data = InternetContent.ISO_8859_1.GetBytes(Info.MRZ_Information);
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
		/// Decodes a DER-encoded object.
		/// </summary>
		/// <param name="Data">Binary data</param>
		/// <param name="Value">Decoded object.</param>
		/// <returns>If successful.</returns>
		public static bool TryDecodeDER(byte[] Data, out object? Value)
		{
			AsnReader Reader = new(Data, AsnEncodingRules.DER);
			return TryDecodeDERNext(Reader, out Value);
		}

		private static bool TryDecodeDERNext(AsnReader Reader, out object? Value)
		{
			if (!Reader.HasData)
			{
				Value = null;
				return false;
			}

			Asn1Tag Tag = Reader.PeekTag();

			switch (Tag.TagValue)
			{
				case (int)UniversalTagNumber.EndOfContents:
					Value = null;
					return false;

				case (int)UniversalTagNumber.Boolean:
					Value = Reader.ReadBoolean();
					return true;

				case (int)UniversalTagNumber.Integer:
				case (int)UniversalTagNumber.Enumerated:
					Value = Reader.ReadInteger();
					return true;

				case (int)UniversalTagNumber.BitString:
					byte[] Bin = Reader.ReadBitString(out int BitCount);
					BitArray Bits = new(Bin)
					{
						Length = BitCount
					};
					Value = Bits;
					return true;

				case (int)UniversalTagNumber.OctetString:
					Value = Reader.ReadOctetString();
					return true;

				case (int)UniversalTagNumber.Null:
					Reader.ReadNull();
					Value = null;
					return true;

				case (int)UniversalTagNumber.ObjectIdentifier:
					Value = Reader.ReadObjectIdentifier();
					return true;

				case (int)UniversalTagNumber.ObjectDescriptor:  // Obsolete
				case (int)UniversalTagNumber.UTF8String:
				case (int)UniversalTagNumber.NumericString:
				case (int)UniversalTagNumber.PrintableString:
				case (int)UniversalTagNumber.TeletexString:     // Same as UniversalTagNumber.T61String:
				case (int)UniversalTagNumber.VideotexString:
				case (int)UniversalTagNumber.IA5String:
				case (int)UniversalTagNumber.GraphicString:
				case (int)UniversalTagNumber.VisibleString:     // Same as UniversalTagNumber.ISO646String:
				case (int)UniversalTagNumber.GeneralString:
				case (int)UniversalTagNumber.UniversalString:
				case (int)UniversalTagNumber.UnrestrictedCharacterString:
				case (int)UniversalTagNumber.BMPString:
					Value = Reader.ReadCharacterString((UniversalTagNumber)Tag.TagValue);
					return true;

				case (int)UniversalTagNumber.External:          // Same as UniversalTagNumber.InstanceOf:
				case (int)UniversalTagNumber.Set:               // Same as UniversalTagNumber.SetOf:
				case (int)UniversalTagNumber.Embedded:
					AsnReader Inner = Reader.ReadSetOf();
					ChunkedList<object?> Elements = [];

					while (TryDecodeDERNext(Inner, out object? Element))
						Elements.Add(Element);

					Value = Elements.ToArray();
					return true;

				case (int)UniversalTagNumber.Real:
				case (int)UniversalTagNumber.RelativeObjectIdentifier:
				case (int)UniversalTagNumber.Time:
				case (int)UniversalTagNumber.Date:
				case (int)UniversalTagNumber.TimeOfDay:
				case (int)UniversalTagNumber.DateTime:
				case (int)UniversalTagNumber.Duration:
				case (int)UniversalTagNumber.ObjectIdentifierIRI:
				case (int)UniversalTagNumber.RelativeObjectIdentifierIRI:
					Value = Reader.ReadEncodedValue();
					return true;

				case (int)UniversalTagNumber.Sequence:          // Same as UniversalTagNumber.SequenceOf:
					Inner = Reader.ReadSequence();
					Elements = [];

					while (TryDecodeDERNext(Inner, out object? Element))
						Elements.Add(Element);

					Value = Elements.ToArray();
					return true;

				case (int)UniversalTagNumber.UtcTime:
					Value = Reader.ReadUtcTime();
					return true;

				case (int)UniversalTagNumber.GeneralizedTime:
					Value = Reader.ReadGeneralizedTime();
					return true;

				default:
					Value = null;
					return false;
			}
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

			byte[] Response = await this.tagInterface.ExecuteCommand(Command, this);

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

			byte[] Response = await this.tagInterface.ExecuteCommand(Request, this);

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

			byte[] Response = await this.tagInterface.ExecuteCommand(Command, this);

			if (!this.CheckResponse(Response))
				return null;

			if (Response.Length != 10 || Response[8] != 0x90 || Response[9] != 0x00)
			{
				this.Error("Unexpected response received.");
				return null;
			}

			byte[] Challenge = new byte[8];
			Buffer.BlockCopy(Response, 0, Challenge, 0, 8);

			return Response;
		}

		/// <summary>
		/// Send Response to challenge (§7.1.5.4, §D.3)
		/// </summary>
		/// <param name="ChallengeResponse">ChallengeResponse.</param>
		/// <returns>Challenge</returns>
		private async Task<byte[]?> ExternalBacAuthenticate(byte[] ChallengeResponse)
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

			byte[] Response = await this.tagInterface.ExecuteCommand(Command, this);

			if (!this.CheckResponse(Response))
				return null;

			if (Response.Length != 10 || Response[8] != 0x90 || Response[9] != 0x00)
			{
				this.Error("Unexpected response received.");
				return null;
			}

			byte[] Challenge = new byte[8];
			Buffer.BlockCopy(Response, 0, Challenge, 0, 8);

			return Response;
		}

		/// <summary>
		/// Authenticates the client with the travel document chip using the document information
		/// provided in the constructor.
		/// </summary>
		/// <returns>If authentication was successful.</returns>
		public async Task<bool> Authenticate()
		{
			// §4.2 1. https://www2023.icao.int/publications/Documents/9303_p11_cons_en.pdf

			byte[]? Data = await this.DownloadFile(TravelDocumentsExtensions.ElementaryFiles.CardAccess);
			IPaceProtocol? Protocol;

			if (Data is not null &&
				TryDecodeDER(Data, out object? CardAccess) &&
				(Protocol = await this.TryFindPaceProtocol(CardAccess)) is not null)
			{
				// Optional: Read EF.DIR	§4.2 2. https://www2023.icao.int/publications/Documents/9303_p11_cons_en.pdf

				// PACE
				// §4.2 3. https://www2023.icao.int/publications/Documents/9303_p11_cons_en.pdf

				if (this.HasSniffers)
					this.Information("PACE protocol " + Protocol.GetType().Name.Replace('_', '-') + " selected.");

				if (!await this.InitializePACE(Protocol))
				{
					this.Error("Unable to initialize PACE protocol.");
					return false;
				}
				else if (Protocol is PaceEcdhProtocol EecProtocol)
					this.Information("PACE protocol initialized (" + EecProtocol.Curve?.CurveName + ").");
				else
					this.Information("PACE protocol initialized.");

				if (!await Protocol.Authenticate(this))
				{
					this.Error("Authentication unsuccessful.");
					return false;
				}
			}
			else
			{
				// BAC
				// §4.2 4. https://www2023.icao.int/publications/Documents/9303_p11_cons_en.pdf

				this.Information("Attempting legacy BAC protocol.");

				// §4.3, §D.3, https://www.icao.int/publications/Documents/9303_p11_cons_en.pdf

				byte[]? Challenge = await this.GetBacChallenge();

				if (Challenge is null)
				{
					this.Error("Unable to get BAC challenge.");
					return false;
				}

				byte[] ChallengeResponse = CalcChallengeResponse3DES(this.documentInformation, Challenge);
				byte[]? Response = await this.ExternalBacAuthenticate(ChallengeResponse);

				// TODO: Implement/Test BAC
			}

			return true;
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
			byte[] S = TravelDocumentsClient.CONCAT(Rnd1, Challenge, Rnd2);
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
						H = FinalDecryptor.TransformFinalBlock(H, 0, 8);
					}

					H = Encryptor2.TransformFinalBlock(H, 0, 8);
				}

				MIFD = H;
			}

			return TravelDocumentsClient.CONCAT(EIFD, MIFD);
		}

		/// <summary>
		/// Calculates a response to a BAC challenge using 3DES & SHA1.
		/// </summary>
		/// <param name="Info">Document Information</param>
		/// <param name="Challenge">Challenge</param>
		/// <returns>Response</returns>
		public static byte[] CalcChallengeResponse3DES(DocumentInformation Info, byte[] Challenge)
		{
			byte[] Rnd1 = new byte[8];
			byte[] Rnd2 = new byte[16];

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
		/// <param name="Protocol">Cipher suite selected.</param>
		/// <returns>true if authenticated, false if unable to authenticate with the document.</returns>
		internal async Task<bool> AuthenticateGenericMapping(PaceEcdhProtocol Protocol)
		{
			if (Protocol?.Curve is null)
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

				byte[] Kπ = Protocol.KDFπ(this.documentInformation);
				byte[] s = Protocol.DecryptNonce(Kπ, z);

				this.Information("Decrypted nonce: " + Hashes.BinaryToString(s));

				// Main keys

				byte[] LocalPublicKey = Protocol.CreateNewKey();    // Creates a public key in big-endian format.

				this.Information("Local public key: " + Hashes.BinaryToString(LocalPublicKey));
				this.Information("Local private key: " + Protocol.Curve.Export());

				byte[]? RemotePublicKey = await this.GetPaceRemotePublicKey(LocalPublicKey);  // Big-endian format.

				if (RemotePublicKey is null)
				{
					this.Error("Unable to get PACE remote public key.");
					return false;
				}

				this.Information("Remote public key: " + Hashes.BinaryToString(RemotePublicKey));

				if (!Protocol.Curve.IsPoint(RemotePublicKey, true))
				{
					this.Error("Remote public key not on curve.");
					return false;
				}

				// Shared Secret

				byte[] SharedSecret = Protocol.GetSharedSecret(RemotePublicKey);

				this.Information("Shared secret: " + Hashes.BinaryToString(SharedSecret));

				// Map

				PointOnCurve Ĝ = Protocol.GetGenericMap(s, RemotePublicKey);
				byte[] Generator = Protocol.Curve.Encode(Ĝ, true);

				this.Information("Generator Ĝ: " + Hashes.BinaryToString(Generator));


				// Ephemeral keys

				byte[] LocalEphemeralPrivateKey = Protocol.Curve.GenerateSecret();

				this.Information("Local ephemeral private key: " + Hashes.BinaryToString(LocalEphemeralPrivateKey));

				PointOnCurve P1 = Protocol.Curve.ScalarMultiplication(LocalEphemeralPrivateKey, Ĝ, true);
				byte[] LocalEphemeralPublicKey = Protocol.Curve.Encode(P1, true);

				this.Information("Local ephemeral public key: " + Hashes.BinaryToString(LocalEphemeralPublicKey));

				byte[]? RemoteEphemeralPublicKey = await this.GetPaceRemotePublicEphemeralKey(LocalEphemeralPublicKey);

				if (RemoteEphemeralPublicKey is null)
				{
					this.Error("Unable to get PACE remote ephemeral public key.");
					return false;
				}

				this.Information("Remote ephemeral public key: " + Hashes.BinaryToString(RemoteEphemeralPublicKey));

				if (!Protocol.Curve.IsPoint(RemoteEphemeralPublicKey, true))
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

				PointOnCurve EphemeralSharedPoint = Protocol.Curve.ScalarMultiplication(
					LocalEphemeralPrivateKey, RemoteEphemeralPublicPoint, true);

				byte[] EphemeralSharedPointX = EphemeralSharedPoint.X.ToByteArray();    // Little-endian

				if (EphemeralSharedPointX.Length != Protocol.Curve.OrderBytes)
					Array.Resize(ref EphemeralSharedPointX, Protocol.Curve.OrderBytes);

				Array.Reverse(EphemeralSharedPointX);                                   // Big-endian

				this.Information("Ephemeral shared secret: " + Hashes.BinaryToString(EphemeralSharedPointX));

				// Session keys

				byte[] KS_Enc = Protocol.KDF_Enc(EphemeralSharedPointX);
				byte[] KS_Mac = Protocol.KDF_Mac(EphemeralSharedPointX);

				this.Information("KS_Enc: " + Hashes.BinaryToString(KS_Enc));
				this.Information("KS_Mac: " + Hashes.BinaryToString(KS_Mac));

				// Associated Data

				byte[] AD_IFD = PaceProtocol.CreateAssociatedData(Protocol.Oid, RemoteEphemeralPublicKey);
				byte[] AD_IC = PaceProtocol.CreateAssociatedData(Protocol.Oid, LocalEphemeralPublicKey);

				this.Information("AD_IFD: " + Hashes.BinaryToString(AD_IFD));
				this.Information("AD_IC: " + Hashes.BinaryToString(AD_IC));

				// Computing MAC

				CMac Mac = Protocol.GetAuthenticator(KS_Mac);

				byte[] T_IFD = Mac.Sign(AD_IFD, 8);

				this.Information("T_IFD: " + Hashes.BinaryToString(T_IFD));

				byte[]? RemoteToken = await this.GetPaceRemoteVerificationToken(T_IFD);

				if (RemoteToken is null)
				{
					this.Error("Unable to get remote token.");
					return false;
				}

				this.Information("Remote Token: " + Hashes.BinaryToString(RemoteToken));

				if (!Mac.Verify(AD_IC, RemoteToken))
				{
					byte[] T_IC = Mac.Sign(AD_IC, 8);

					this.Error("PACE token validation failed. Expected _IC: " + Hashes.BinaryToString(T_IC));
					return false;
				}

				this.Information("Authentication successful.");

				return true;
			}
			catch (Exception ex)
			{
				this.Exception(ex);
				return false;
			}
		}

	}
}
