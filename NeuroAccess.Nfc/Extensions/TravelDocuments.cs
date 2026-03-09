using System;
using System.Collections;
using System.Collections.Generic;
using System.Formats.Asn1;
using System.IO;
using System.Security.Cryptography;
using System.Text.RegularExpressions;
using System.Threading.Tasks;
using NeuroAccess.Nfc.Extensions.PACE;
using Waher.Runtime.Collections;
using Waher.Script.Functions.Scalar;

namespace NeuroAccess.Nfc.Extensions
{
	/// <summary>
	/// Contains NFC Extensions for Machine-Readable Travel Documents.
	/// 
	/// References:
	/// https://www2023.icao.int/publications/Documents/9303_p10_cons_en.pdf
	/// https://www2023.icao.int/publications/Documents/9303_p11_cons_en.pdf
	/// </summary>
	public static class TravelDocuments
	{
		/// <summary>
		/// Elementary Files in travel documents.
		/// </summary>
		public static class ElementaryFiles
		{
			/// <summary>
			/// EF.CardAccess. §3.11.3 ICAO Doc 9303, https://www2023.icao.int/publications/Documents/9303_p10_cons_en.pdf
			/// </summary>
			public const ushort CardAccess = 0x011c;
		}

		/// <summary>
		/// Derives Basic Access Control Keys from the second row of the 
		/// Machine-Readable string in passport (MRZ).
		/// </summary>
		/// <param name="MRZ">Machine-Readable text.</param>
		/// <param name="Info">Parsed Document Information.</param>
		/// <returns>If the string could be parsed.</returns>
		public static bool ParseMrz(string MRZ, out DocumentInformation? Info)
		{
			Match M = td2_mrz_nr9charsplus.Match(MRZ);
			if (M.Success)
			{
				Info = AssembleInfo2(M);
				return Info is not null;
			}

			M = td2_mrz_nr9chars.Match(MRZ);
			if (M.Success)
			{
				Info = AssembleInfo1(M);
				return Info is not null;
			}

			M = td1_mrz_nr9charsplus.Match(MRZ);
			if (M.Success)
			{
				Info = AssembleInfo2(M);
				return Info is not null;
			}

			M = td1_mrz_nr9chars.Match(MRZ);
			if (M.Success)
			{
				Info = AssembleInfo1(M);
				return Info is not null;
			}

			// TODO: Checks

			Info = null;
			return false;
		}

		private static DocumentInformation? AssembleInfo2(Match M)
		{
			DocumentInformation Result = AssembleInfo(M);
			Result.DocumentNumber = M.Groups["Nr1"].Value + M.Groups["Nr2"].Value;

			return CalcMrzInfo(Result, M) ? Result : null;
		}

		private static DocumentInformation? AssembleInfo1(Match M)
		{
			DocumentInformation Result = AssembleInfo(M);
			Result.DocumentNumber = M.Groups["Nr"].Value;

			return CalcMrzInfo(Result, M) ? Result : null;
		}

		private static bool CalcMrzInfo(DocumentInformation Info, Match M)
		{
			if (Info.DocumentNumber is null || Info.DateOfBirth is null || Info.ExpiryDate is null)
				return false;

			string NrCheck = M.Groups["NrCheck"].Value;
			if (NrCheck != CalcCheckDigit(Info.DocumentNumber))
				return false;

			string BirthCheck = M.Groups["BirthCheck"].Value;
			if (BirthCheck != CalcCheckDigit(Info.DateOfBirth))
				return false;

			string ExpiryCheck = M.Groups["ExpiryCheck"].Value;
			if (ExpiryCheck != CalcCheckDigit(Info.ExpiryDate))
				return false;

			if (!string.IsNullOrEmpty(Info.OptionalData))
			{
				string s = Info.OptionalData!.Replace("<", string.Empty);

				if (!string.IsNullOrEmpty(s))
				{
					string OptionalCheck = M.Groups["OptionalCheck"].Value;
					if (OptionalCheck != CalcCheckDigit(Info.OptionalData))
						return false;
				}

				Info.OptionalData = s;
			}

			// TODO: Check OverallCheck

			Info.MRZ_Information = Info.DocumentNumber + NrCheck +
				Info.DateOfBirth + BirthCheck + Info.ExpiryDate + ExpiryCheck;

			Info.DocumentNumber = Info.DocumentNumber.Replace("<", string.Empty);

			// TODO: Check note in §9.7.3 ICAO 9303-p11:
			//
			// TD1-documents with document numbers longer than nine characters, the
			// document number needs to be concatenated from the document number field
			// and the optional data field of the MRZ, excluding the filler character.

			return true;
		}

		private static string CalcCheckDigit(string Value)
		{
			// §4.9, ISO/IEC 9303, Part 3: https://www.icao.int/publications/Documents/9303_p3_cons_en.pdf

			int Sum = 0;
			int i = 0;
			int j;

			foreach (char ch in Value)
			{
				if (ch >= '0' && ch <= '9')
					j = ch - '0';
				else if (ch >= 'A' && ch <= 'Z')
					j = ch - 'A' + 10;
				else if (ch >= 'a' && ch <= 'z')
					j = ch - 'a' + 10;
				else if (ch == '<')
					j = 0;
				else
					return string.Empty;

				j *= weights[i++];
				Sum += j;
				i %= 3;
			}

			return new string((char)('0' + Sum % 10), 1);
		}

		private static readonly int[] weights = [7, 3, 1];

		private static DocumentInformation AssembleInfo(Match M)
		{
			return new DocumentInformation()
			{
				DocumentType = M.Groups["DocType"].Value,
				IssuingState = M.Groups["Issuer"].Value,
				Nationality = M.Groups["Nationality"].Value,
				PrimaryIdentifier = M.Groups["PID"].Value.Split('<'),
				SecondaryIdentifier = M.Groups["SID"].Value.Split('<'),
				Gender = M.Groups["Gender"].Value,
				DocumentNumber = M.Groups["Nr"].Value,
				DateOfBirth = M.Groups["Birth"].Value,
				ExpiryDate = M.Groups["Expires"].Value,
				OptionalData = M.Groups["Optional"].Value
			};
		}

		// TD2, ref: ICAO 9303-5, §B: https://www.icao.int/publications/Documents/9303_p5_cons_en.pdf
		private static readonly Regex td2_mrz_nr9charsplus = new(@"^(?'DocType'.{1,2})<(?'Issuer'\w{3})(?'PID'[^<]+(<[^<]+)*)<<(?'SID'[^<]+(<[^<]+)*)<*\n(?'Nr1'[^<]{9})<(?'Nationality'\w{3})(?'Birth'[^<]*)(?'BirthCheck'\d)(?'Gender'[MF])(?'Expires'[^<]{6})(?'ExpiryCheck'\d)(?'Nr2'[^<]*)(?'NrCheck'\d)((?'Optional'.*)(?'OptionalCheck'\d))?<*(?'OverallCheck'\d)$", RegexOptions.Multiline);
		private static readonly Regex td2_mrz_nr9chars = new(@"^(?'DocType'.{1,2})<(?'Issuer'\w{3})(?'PID'[^<]+(<[^<]+)*)<<(?'SID'[^<]+(<[^<]+)*)<*\n(?'Nr'.{9})(?'NrCheck'\d)(?'Nationality'\w{3})(?'Birth'[^<]{6})(?'BirthCheck'\d)(?'Gender'[MF])(?'Expires'[^<]{6})(?'ExpiryCheck'\d)((?'Optional'.*)(?'OptionalCheck'\d))?<*(?'OverallCheck'\d)$", RegexOptions.Multiline);

		// TD1, ref: ICAO 9303-5, §B: https://www.icao.int/publications/Documents/9303_p5_cons_en.pdf
		private static readonly Regex td1_mrz_nr9charsplus = new(@"^(?'DocType'.{1,2})<(?'Issuer'\w{3})(?'Nr1'[^<]{9})<(?'Nr2'.{3})(?'NrCheck'\d)((?'Optional'.*)(?'OptionalCheck'\d))?<*\n(?'Birth'[^<]{6})(?'BirthCheck'\d)(?'Gender'[MF])(?'Expires'[^<]{6})(?'ExpiryCheck'\d)(?'Nationality'\w{3})<*(?'OverallCheck'\d)\n(?'PID'[^<]+(<[^<]+)*)<<(?'SID'[^<]+(<[^<]+)*).*$", RegexOptions.Multiline);
		private static readonly Regex td1_mrz_nr9chars = new(@"^(?'DocType'.{1,2})<(?'Issuer'\w{3})(?'Nr'.{9})(?'NrCheck'.)((?'Optional'.*)(?'OptionalCheck'\d))?<*\n(?'Birth'[^<]{6})(?'BirthCheck'\d)(?'Gender'[MF])(?'Expires'[^<]{6})(?'ExpiryCheck'\d)(?'Nationality'\w{3})<*(?'OverallCheck'\d)\n(?'PID'[^<]+(<[^<]+)*)<<(?'SID'[^<]+(<[^<]+)*).*$", RegexOptions.Multiline);

		/// <summary>
		/// Processes basic status word response codes.
		/// </summary>
		/// <param name="TagInterface">Interfacer performing communication.</param>
		/// <param name="CheckResponse">Response received.</param>
		/// <returns>If processing can continue.</returns>
		private static bool CheckResponse(this IIsoDepInterface TagInterface, byte[] CheckResponse)
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
					TagInterface.Information(SW2.ToString() + " bytes still available");
					return true;

				case Iso7816StatusCategory.WarningUnchanged:
					switch (SW2)
					{
						case 0:
							TagInterface.Warning("Warning, state unchanged. No information given.");
							break;

						default:
							TagInterface.Warning("Warning " + SW2.ToString("X2") + " triggered by card. State unchanged.");
							break;

						case 0x81:
							TagInterface.Warning("Part of returned data may be corrupted");
							break;

						case 0x82:
							TagInterface.Warning("End of file or record reached before reading Ne bytes.");
							break;

						case 0x83:
							TagInterface.Warning("Selected file deactivated.");
							break;

						case 0x84:
							TagInterface.Warning("File control information not formatted correctly.");
							break;

						case 0x85:
							TagInterface.Warning("Selected file in termination state.");
							break;

						case 0x86:
							TagInterface.Warning("No input data available from a sensor on the card.");
							break;
					}
					return true;

				case Iso7816StatusCategory.WarningChanged:
					switch (SW2)
					{
						case 0:
							TagInterface.Warning("Warning, state changed. No information given.");
							break;

						default:
							TagInterface.Warning("Warning " + SW2.ToString("X2") + " triggered by card. State changed.");
							break;

						case 0x81:
							TagInterface.Warning("File filled up by the last write.");
							break;
					}
					return true;

				case Iso7816StatusCategory.ErrorUnchanged:
					switch (SW2)
					{
						case 0:
							TagInterface.Error("Error, state unchanged. No information given.");
							break;

						default:
							TagInterface.Error("Error " + SW2.ToString("X2") + " triggered by card. State unchanged.");
							break;

						case 0x01:
							TagInterface.Error("Immediate response required by the card.");
							break;
					}
					return false;

				case Iso7816StatusCategory.ErrorChanged:
					switch (SW2)
					{
						case 0:
							TagInterface.Error("Error, state changed. No information given.");
							break;

						default:
							TagInterface.Error("Error " + SW2.ToString("X2") + " triggered by card. State changed.");
							break;

						case 0x81:
							TagInterface.Error("Memory failure.");
							break;
					}
					return false;

				case Iso7816StatusCategory.SecurityIssue:
					TagInterface.Error("Security issue detected.");
					return false;

				case Iso7816StatusCategory.WrongLength:
					TagInterface.Error("Wrong length.");
					return false;

				case Iso7816StatusCategory.FunctionNotSupported:
					switch (SW2)
					{
						case 0:
							TagInterface.Error("Function Not Supported. No information given.");
							break;

						default:
							TagInterface.Error("Function Not Supported " + SW2.ToString("X2") + " triggered by card.");
							break;

						case 0x81:
							TagInterface.Error("Logical channel not supported.");
							break;

						case 0x82:
							TagInterface.Error("Secure messaging not supported.");
							break;

						case 0x83:
							TagInterface.Error("Last command of the chain expected.");
							break;

						case 0x84:
							TagInterface.Error("Command chaining not supported.");
							break;
					}
					return false;

				case Iso7816StatusCategory.NotAllowed:
					switch (SW2)
					{
						case 0:
							TagInterface.Error("Not Allowed. No information given.");
							break;

						default:
							TagInterface.Error("Not Allowed " + SW2.ToString("X2") + " triggered by card.");
							break;

						case 0x81:
							TagInterface.Error("Command incompatible with file structure.");
							break;

						case 0x82:
							TagInterface.Error("Security status not satisfied.");
							break;

						case 0x83:
							TagInterface.Error("Authentication method blocked.");
							break;

						case 0x84:
							TagInterface.Error("Reference data not usable.");
							break;

						case 0x85:
							TagInterface.Error("Conditions of use not satisfied.");
							break;

						case 0x86:
							TagInterface.Error("Command not allowed (no current EF).");
							break;

						case 0x87:
							TagInterface.Error("Expected secure messaging data objects missing.");
							break;

						case 0x88:
							TagInterface.Error("Incorrect secure messaging data objects.");
							break;
					}
					return false;

				case Iso7816StatusCategory.WrongParameters:
					switch (SW2)
					{
						case 0:
							TagInterface.Error("Wrong Parameters. No information given.");
							break;

						default:
							TagInterface.Error("Wrong Parameters " + SW2.ToString("X2") + " triggered by card.");
							break;

						case 0x80:
							TagInterface.Error("Incorrect parameters in the command data field.");
							break;

						case 0x81:
							TagInterface.Error("Function not supported.");
							break;

						case 0x82:
							TagInterface.Error("File or application not found.");
							break;

						case 0x83:
							TagInterface.Error("Record not found.");
							break;

						case 0x84:
							TagInterface.Error("Not enough memory space in the file.");
							break;

						case 0x85:
							TagInterface.Error("Nc inconsistent with TLV structure.");
							break;

						case 0x86:
							TagInterface.Error("Incorrect parameters P1-P2.");
							break;

						case 0x87:
							TagInterface.Error("Nc inconsistent with parameters P1-P2.");
							break;

						case 0x88:
							TagInterface.Error("Referenced data or reference data not found (exact meaning depending on the command).");
							break;

						case 0x89:
							TagInterface.Error("File already exists.");
							break;

						case 0x8A:
							TagInterface.Error("DF name already exists.");
							break;
					}
					return false;

				case Iso7816StatusCategory.WrongLeField:
					TagInterface.Error("Le field incorrect. Should be " + SW2.ToString("X2"));
					return false;

				default:
					TagInterface.Error("Unexpected response received. SW1=" + SW1.ToString("X2") +
						", SW2=" + SW2.ToString("X2"));
					return false;
			}
		}

		/// <summary>
		/// Selects a File
		/// </summary>
		/// <param name="TagInterface">NFC interface to tag.</param>
		/// <param name="FileId">File to select.</param>
		/// <returns>If file was selected.</returns>
		public static async Task<bool> SelectFile(this IIsoDepInterface TagInterface, ushort FileId)
		{
			if (TagInterface.HasSniffers)
				TagInterface.Information("SelectFile(" + FileId.ToString("X4") + ")");

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

			byte[] Response = await TagInterface.ExecuteCommand(Command);

			return TagInterface.CheckResponse(Response);
		}

		/// <summary>
		/// Reads binary information from the currently selected file.
		/// </summary>
		/// <param name="TagInterface">NFC interface to tag.</param>
		/// <returns>Read data, or null if an error occurred.</returns>
		public static Task<KeyValuePair<byte[]?, bool>> ReadBinary(this IIsoDepInterface TagInterface,
			ushort Offset)
		{
			return TagInterface.ReadBinary(Offset, 0);
		}

		/// <summary>
		/// Reads binary information from the currently selected file.
		/// </summary>
		/// <param name="TagInterface">NFC interface to tag.</param>
		/// <param name="NrBytes">Number of bytes to read.</param>
		/// <returns>Read data, or null if an error occurred.</returns>
		public static async Task<KeyValuePair<byte[]?, bool>> ReadBinary(this IIsoDepInterface TagInterface,
			ushort Offset, byte NrBytes)
		{
			if (TagInterface.HasSniffers)
				TagInterface.Information("ReadBinary(" + Offset.ToString("X4") + "," + NrBytes.ToString("X2") + ")");

			byte[] Command =
			[
				ISO_7816.Classes.Basic,
				ISO_7816.Instructions.ReadBinary,
				(byte)(Offset >> 8),	// P1
				(byte)Offset,			// P2
				NrBytes					// Le
			];

			byte[] Response = await TagInterface.ExecuteCommand(Command);
			int c = Response.Length;

			if (!TagInterface.CheckResponse(Response))
			{
				if (Response is not null &&
					c >= 2 &&
					Response[^2] == (byte)Iso7816StatusCategory.WrongLeField)
				{
					Command[4] = Response[^1];
					Response = await TagInterface.ExecuteCommand(Command);

					if (!TagInterface.CheckResponse(Response))
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
		/// <param name="TagInterface">NFC interface to tag.</param>
		/// <param name="FileId">File to download.</param>
		/// <returns>Downloaded file, or null if unable to download file.</returns>
		public static async Task<byte[]?> DownloadFile(this IIsoDepInterface TagInterface, ushort FileId)
		{
			if (!await TagInterface.SelectFile(FileId))
				return null;

			using MemoryStream File = new();
			ushort Offset = 0;

			while (true)
			{
				KeyValuePair<byte[]?, bool> P = await TagInterface.ReadBinary(Offset);
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

		public static async Task<bool> InitializePACE(this IIsoDepInterface TagInterface, IPaceProtocol Protocol)
		{
			if (TagInterface.HasSniffers)
				TagInterface.Information("MSE:Set AT(" + Protocol.Oid + ",MRZ)");

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

			byte[] Response = await TagInterface.ExecuteCommand(Command);

			return TagInterface.CheckResponse(Response);
		}

		/// <summary>
		/// Concatenates a series of byte arrays.
		/// </summary>
		/// <param name="Bytes">First byte array</param>
		/// <param name="MoreBytes">following bytes arrays.</param>
		/// <returns>Concatenated byte array.</returns>
		public static byte[] CONCAT(this byte[] Bytes, params byte[][] MoreBytes)
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
		/// Decodes a DER-encoded object.
		/// </summary>
		/// <param name="Data">Binary data</param>
		/// <param name="Value">Decoded object.</param>
		/// <returns>If successful.</returns>
		public static bool TryDecodeDER(this byte[] Data, out object? Value)
		{
			AsnReader Reader = new(Data, AsnEncodingRules.DER);
			return Reader.TryDecodeDERNext(out Value);
		}

		private static bool TryDecodeDERNext(this AsnReader Reader, out object? Value)
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
		/// <param name="TagInterface">NFC interface to tag.</param>
		/// <returns>Nonce</returns>
		public static async Task<byte[]?> GetPaceEncryptedNonce(this IIsoDepInterface TagInterface)
		{
			TagInterface.Information("GetEncryptedNonce");

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

			byte[] Response = await TagInterface.ExecuteCommand(Command);

			if (!TagInterface.CheckResponse(Response))
				return null;

			if (Response.Length < 6 ||
				Response[0] != 0x7c ||
				Response.Length != Response[1] + 4 ||
				Response[2] != 0x80 ||      // Encrypted nonce
				Response.Length != Response[3] + 6 ||
				Response[^2] != 0x90 ||
				Response[^1] != 0x00)
			{
				TagInterface.Error("Unexpected response received.");
				return null;
			}

			int c = Response[3];
			byte[] Nonce = new byte[c];

			Buffer.BlockCopy(Response, 4, Nonce, 0, c);

			return Response;
		}

		/// <summary>
		/// Get PACE Remote Public Key
		/// </summary>
		/// <param name="TagInterface">NFC interface to tag.</param>
		/// <param name="LocalPublicKey">Local Public Key</param>
		/// <returns>Remote Public Key</returns>
		public static async Task<byte[]?> GetPaceRemotePublicKey(this IIsoDepInterface TagInterface,
			byte[] LocalPublicKey)
		{
			TagInterface.Information("GetRemotePublicKey");

			int c = LocalPublicKey.Length;

			byte[] Command = CONCAT(
				[
					ISO_7816.Classes.Chaining,
					ISO_7816.Instructions.GeneralAuthenticate,
					0x00,									// P1
					0x00,									// P2
					(byte)(c + 5)		// Lc
				],
				[
					[
						0x7c,			// Dynamic Authentication Data
						(byte)(c + 3),
						0x81,			// Mapping Data
						(byte)(c + 1),
						0x04			// X coordinate following by Y coordinate (default for EEC curves)
					],
					LocalPublicKey,
					[ 0x00 ]	// Le (Maximal response length: 256 bytes)
				]);

			byte[] Response = await TagInterface.ExecuteCommand(Command);

			if (!TagInterface.CheckResponse(Response))
				return null;

			if (Response.Length < 6 ||
				Response[0] != 0x7c ||
				Response.Length != Response[1] + 4 ||
				Response[2] != 0x82 ||      // Mapping data
				Response.Length != Response[3] + 6 ||
				Response[4] != 0x04 ||      // X coordinate following by Y coordinate (default for EEC curves)
				Response[^2] != 0x90 ||
				Response[^1] != 0x00)
			{
				TagInterface.Error("Unexpected response received.");
				return null;
			}

			c = Response[3] - 1;
			byte[] RemotePublicKey = new byte[c];

			Buffer.BlockCopy(Response, 5, RemotePublicKey, 0, c);

			return Response;
		}

		/// <summary>
		/// Get Challenge (§7.1.5.4, §D.3)
		/// </summary>
		/// <param name="TagInterface">NFC interface to tag.</param>
		/// <returns>Challenge</returns>
		public static async Task<byte[]?> GetBacChallenge(this IIsoDepInterface TagInterface)
		{
			TagInterface.Information("GetChallenge");

			byte[] Command =
			[
				ISO_7816.Classes.Basic,
				ISO_7816.Instructions.GetChallenge,
				0x00,	// P1
				0x00,	// P2
				0x08	// Le
			];

			byte[] Response = await TagInterface.ExecuteCommand(Command);

			if (!TagInterface.CheckResponse(Response))
				return null;

			if (Response.Length != 10 || Response[8] != 0x90 || Response[9] != 0x00)
			{
				TagInterface.Error("Unexpected response received.");
				return null;
			}

			byte[] Challenge = new byte[8];
			Buffer.BlockCopy(Response, 0, Challenge, 0, 8);

			return Response;
		}

		/// <summary>
		/// Send Response to challenge (§7.1.5.4, §D.3)
		/// </summary>
		/// <param name="TagInterface">NFC interface to tag.</param>
		/// <param name="ChallengeResponse">ChallengeResponse.</param>
		/// <returns>Challenge</returns>
		public static async Task<byte[]?> ExternalBacAuthenticate(this IIsoDepInterface TagInterface,
			byte[] ChallengeResponse)
		{
			TagInterface.Information("ChallengeResponse");

			byte Lc = (byte)ChallengeResponse.Length;
			byte[] Command = new byte[]
			{
				ISO_7816.Classes.Basic,
				ISO_7816.Instructions.ExternalAuthenticate,
				0x00,	// P1
				0x00,	// P2
				Lc
			}.CONCAT(ChallengeResponse,
			[
				0x28	// Le
			]);

			byte[] Response = await TagInterface.ExecuteCommand(Command);

			if (!TagInterface.CheckResponse(Response))
				return null;

			if (Response.Length != 10 || Response[8] != 0x90 || Response[9] != 0x00)
			{
				TagInterface.Error("Unexpected response received.");
				return null;
			}

			byte[] Challenge = new byte[8];
			Buffer.BlockCopy(Response, 0, Challenge, 0, 8);

			return Response;
		}
	}
}
