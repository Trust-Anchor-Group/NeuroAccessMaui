using System.Globalization;
using NeuroAccess.Nfc.TravelDocuments;
using NeuroAccess.Nfc.TravelDocuments.PACE;
using NeuroAccess.Nfc.TravelDocuments.PACE.Id_PACE_ECDH_GM;
using Waher.Runtime.Inventory;
using Waher.Script.Constants;
using Waher.Security;
using Waher.Security.EllipticCurves;

namespace NeuroAccess.Nfc.Test
{
	[TestClass]
	public class PaceTests
	{
		[AssemblyInitialize]
		public static void AssemblyInit(TestContext _)
		{
			Types.Initialize(
				typeof(PaceTests).Assembly,
				typeof(PaceProtocol).Assembly);
		}

		// Testing PACE - Generic Mapping, in accordance with ICAO Doc 9303
		// Reference tests: §G, https://www2023.icao.int/publications/Documents/9303_p11_cons_en.pdf

		[TestMethod]
		[DataRow(
			"P<GBRDOE<<JOE<<<<<<<<<<<<<<<<<<<<<<<<<<<<<<<\nT220001293GBR6408125M1010318<<<<<<<<<<<<<<06",
			"T22000129", "640812", "101031", "T22000129364081251010318",
			"89DED1B26624EC1E634C1989302849DD")]
		public void Test_01_Seed(string Mrz, string DocumentNumber, string DateOfBirth,
			string ExpiryDate, string MrzInformation, string Kπ)
		{
			Assert.IsTrue(TravelDocumentsExtensions.ParseMrz(Mrz, out DocumentInformation? Info));
			Assert.AreEqual(DocumentNumber, Info!.DocumentNumber);
			Assert.AreEqual(DateOfBirth, Info.DateOfBirth);
			Assert.AreEqual(ExpiryDate, Info.ExpiryDate);
			Assert.AreEqual(MrzInformation, Info.MRZ_Information);
			Assert.AreEqual(Kπ, Hashes.BinaryToString(new Id_PACE_ECDH_GM_AES_CBC_CMAC_128().KDFπ(Info)).ToUpperInvariant());
		}

		[TestMethod]
		[DataRow("MSgwEgYKBAB/AAcCAgQCBAIBAgIBEDASBgoEAH8ABwICBAYEAgECAgEQ", true)]
		public void Test_02_Parse_EF_CardAccess(string CardAccess, bool IsBase64)
		{
			byte[] Bin = Decode(CardAccess, IsBase64);
			Assert.IsTrue(TravelDocumentsClient.TryDecodeDER(Bin, out object? Value));
			Assert.IsNotNull(Value);

			Array? SecurityInfos = Value as Array;
			Assert.IsNotNull(SecurityInfos);

			foreach (object Element in SecurityInfos)
			{
				Array? SecurityInfo = Element as Array;
				Assert.IsNotNull(SecurityInfo);

				foreach (object Element2 in SecurityInfo)
					Console.Out.WriteLine(Element2.ToString());

				Console.Out.WriteLine();
			}
		}

		private static byte[] Decode(string s, bool IsBase64)
		{
			return IsBase64 ? Convert.FromBase64String(s) : Hashes.StringToBinary(s);
		}

		[TestMethod]
		[DataRow("3012060A 04007F00 07020204 02020201 0202010D", false, typeof(BrainpoolP256))]
		public void Test_03_Parse_SecurityInfo(string CardAccess, bool IsBase64,
			Type CurveType)
		{
			byte[] Bin = Decode(CardAccess, IsBase64);
			Assert.IsTrue(TravelDocumentsClient.TryDecodeDER(Bin, out object? Value));
			Assert.IsNotNull(Value);

			Array? SecurityInfo = Value as Array;
			Assert.IsNotNull(SecurityInfo);
			Assert.HasCount(3, SecurityInfo);

			foreach (object Element2 in SecurityInfo)
				Console.Out.WriteLine(Element2.ToString());

			string? Oid = SecurityInfo.GetValue(0) as string;
			Assert.IsNotNull(Oid);
			Assert.IsNotEmpty(Oid);

			IPaceProtocol? Protocol = Types.FindBest<IPaceProtocol, string>(Oid);
			Assert.IsNotNull(Protocol);

			Assert.IsTrue(Protocol.Configure(SecurityInfo));

			PaceEcdhProtocol? EecProtocol = Protocol as PaceEcdhProtocol;
			Assert.IsNotNull(EecProtocol);

			Console.Out.WriteLine(EecProtocol.GetType().Name);
			Console.Out.WriteLine(EecProtocol.Curve!.CurveName);

			Assert.AreEqual(CurveType, EecProtocol.Curve?.GetType());
		}

		[TestMethod]
		[DataRow(
			"P<GBRDOE<<JOE<<<<<<<<<<<<<<<<<<<<<<<<<<<<<<<\nT220001293GBR6408125M1010318<<<<<<<<<<<<<<06",
			"3012060A 04007F00 07020204 02020201 0202010D",
			"3F00C4D3 9D153F2B 2A214A07 8D899B22",
			"95A3A016 522EE98D 01E76CB6 B98B42C3",
			false)]
		public void Test_04_DecryptNonce(string Mrz, string CardAccess,
			string DecryptedNonce, string EncryptedNonce, bool IsBase64)
		{
			Assert.IsTrue(TravelDocumentsExtensions.ParseMrz(Mrz, out DocumentInformation? Info));

			byte[] Bin = Decode(CardAccess, IsBase64);
			Assert.IsTrue(TravelDocumentsClient.TryDecodeDER(Bin, out object? Value));
			Array? SecurityInfo = Value as Array;
			string? Oid = SecurityInfo!.GetValue(0) as string;
			IPaceProtocol? Protocol = Types.FindBest<IPaceProtocol, string>(Oid!);
			Assert.IsTrue(Protocol!.Configure(SecurityInfo));
			PaceEcdhProtocol? EecProtocol = (PaceEcdhProtocol)Protocol;

			byte[] z = Decode(EncryptedNonce, IsBase64);
			byte[] s = EecProtocol.DecryptNonce(Info!, z);

			Assert.AreEqual(
				DecryptedNonce.Replace(" ", string.Empty),
				Hashes.BinaryToString(s).ToUpperInvariant());
		}

		[TestMethod]
		// Generic mapping example in ICAO 9303-11, Appendix G
		[DataRow(
			// MRZ:
			"P<GBRDOE<<JOE<<<<<<<<<<<<<<<<<<<<<<<<<<<<<<<\nT220001293GBR6408125M1010318<<<<<<<<<<<<<<06",
			// Elliptic Curve Parameters:
			"3012060A 04007F00 07020204 02020201 0202010D",
			// Decrypted and Encrypted nonces:
			"3F00C4D3 9D153F2B 2A214A07 8D899B22",
			"95A3A016 522EE98D 01E76CB6 B98B42C3",
			// Terminal keys:
			new uint[] { 0x7F4EF07B, 0x9EA82FD7, 0x8AD689B3, 0x8D0BC78C, 0xF21F249D, 0x953BC46F, 0x4C6E1925, 0x9C010F99 },
			"7ACF3EFC 982EC455 65A4B155 129EFBC7 4650DCBF A6362D89 6FC70262 E0C2CC5E 544552DC B6725218 799115B5 5C9BAA6D 9F6BC3A9 618E70C2 5AF71777 A9C4922D",
			// Chip keys:
			new uint[] { 0x498FF497, 0x56F2DC15, 0x87840041, 0x839A8598, 0x2BE7761D, 0x14715FB0, 0x91EFA7BC, 0xE9058560 },
			"824FBA91 C9CBE26B EF53A0EB E7342A3B F178CEA9 F45DE0B7 0AA60165 1FBA3F57 30D8C879 AAA9C9F7 3991E61B 58F4D52E B87A0A0C 709A49DC 63719363 CCD13C54",
			// Shared Secret & Mapped Generator:
			"60332EF2 450B5D24 7EF6D386 8397D398 852ED6E8 CAF6FFEE F6BF85CA 57057FD5 0840CA74 15BAF3E4 3BD414D3 5AA4608B 93A2CAF3 A4E3EA4E 82C9C13D 03EB7181",
			"8CED63C9 1426D4F0 EB1435E7 CB1D74A4 6723A0AF 21C89634 F65A9AE8 7A9265E2 8C879506 743F8611 AC33645C 5B985C80 B5F09A0B 83407C1B 6A4D857A E76FE522",
			// Ephemeral Terminal keys:
			new uint[] { 0xA73FB703, 0xAC1436A1, 0x8E0CFA5A, 0xBB3F7BEC, 0x7A070E7A, 0x6788486B, 0xEE230C4A, 0x22762595 },
			"2DB7A64C 0355044E C9DF1905 14C625CB A2CEA487 54887122 F3A5EF0D 5EDD301C 3556F3B3 B186DF10 B857B58F 6A7EB80F 20BA5DC7 BE1D43D9 BF850149 FBB36462",
			// Ephemeral Chip keys:
			new uint[] { 0x107CF586, 0x96EF6155, 0x053340FD, 0x633392BA, 0x81909DF7, 0xB9706F22, 0x6F32086C, 0x7AFF974A },
			"9E880F84 2905B8B3 181F7AF7 CAA9F0EF B743847F 44A306D2 D28C1D9E C65DF6DB 7764B222 77A2EDDC 3C265A9F 018F9CB8 52E111B7 68B32690 4B59A019 3776F094",
			// Ephemeral Shared Secret:
			"28768D20 701247DA E81804C9 E780EDE5 82A9996D B4A31502 0B273319 7DB84925",
			// Session keys:
			"F5F0E35C 0D7161EE 6724EE51 3A0D9A7F",
			"FE251C78 58B356B2 4514B3BD 5F4297D1",
			// Input data
			"7F494F06 0A04007F 00070202 04020286 41049E88 0F842905 B8B3181F 7AF7CAA9 F0EFB743 847F44A3 06D2D28C 1D9EC65D F6DB7764 B22277A2 EDDC3C26 5A9F018F 9CB852E1 11B768B3 26904B59 A0193776 F094",
			"7F494F06 0A04007F 00070202 04020286 41042DB7 A64C0355 044EC9DF 190514C6 25CBA2CE A4875488 7122F3A5 EF0D5EDD 301C3556 F3B3B186 DF10B857 B58F6A7E B80F20BA 5DC7BE1D 43D9BF85 0149FBB3 6462",
			// MAC tokens
			"C2B0BD78 D94BA866",
			"3ABB9674 BCE93C08",
			false, typeof(Id_PACE_ECDH_GM_AES_CBC_CMAC_128), typeof(BrainpoolP256))]
		public void Test_05_GenericMapping(string Mrz, string CardAccess,
			string DecryptedNonce, string EncryptedNonce,
			uint[] TerminalPrivateKey, string TerminalPublicKey,
			uint[] ChipPrivateKey, string ChipPublicKey,
			string SharedSecret, string MappedGenerator,
			uint[] EphemeralTerminalPrivateKey, string EphemeralTerminalPublicKey,
			uint[] EphemeralChipPrivateKey, string EphemeralChipPublicKey,
			string EphemeralSharedSecret,
			string EncryptionKey, string SignatureKey,
			string InputDataTerminal, string InputDataChip,
			string TokenTerminal, string TokenChip,
			bool IsBase64, Type AlgorithmType, Type CurveType)
		{
			Assert.IsTrue(TravelDocumentsExtensions.ParseMrz(Mrz, out DocumentInformation? Info));

			// Elliptic Curve Parameters

			byte[] Bin = Decode(CardAccess, IsBase64);
			Assert.IsTrue(TravelDocumentsClient.TryDecodeDER(Bin, out object? Value));
			Array? SecurityInfo = (Array)Value!;
			string? Oid = (string)SecurityInfo.GetValue(0)!;
			PaceEcdhProtocol? EecProtocol = (PaceEcdhProtocol)Types.FindBest<IPaceProtocol, string>(Oid);
			Assert.AreEqual(AlgorithmType, EecProtocol.GetType());
			Assert.IsTrue(EecProtocol!.Configure(SecurityInfo));

			// Encrypted Nonce

			byte[] z = Decode(EncryptedNonce, IsBase64);
			byte[] s = EecProtocol.DecryptNonce(Info!, z);

			Assert.AreEqual(
				DecryptedNonce.Replace(" ", string.Empty),
				Hashes.BinaryToString(s).ToUpperInvariant());

			// Main keys

			EecProtocol.SetPrivateKey(PrimeFieldCurve.ToByteSecret(TerminalPrivateKey));
			Assert.AreEqual(TerminalPublicKey.Replace(" ", string.Empty),
				Hashes.BinaryToString(EecProtocol.Curve!.PublicKeyBigEndian).ToUpperInvariant());

			EllipticCurve ChipCurve = (EllipticCurve)Types.Instantiate(CurveType);
			ChipCurve.SetPrivateKey(PrimeFieldCurve.ToByteSecret(ChipPrivateKey));
			Assert.AreEqual(ChipPublicKey.Replace(" ", string.Empty),
				Hashes.BinaryToString(ChipCurve.PublicKeyBigEndian).ToUpperInvariant());

			// Shared Secret

			byte[] H = EecProtocol.GetSharedSecret(ChipCurve.PublicKeyBigEndian);

			Assert.AreEqual(SharedSecret.Replace(" ", string.Empty),
				Hashes.BinaryToString(H).ToUpperInvariant());

			// Map

			PointOnCurve Ĝ = EecProtocol.GetGenericMap(s, ChipCurve.PublicKeyBigEndian);
			byte[] Generator = EecProtocol.Curve.Encode(Ĝ, true);

			Assert.AreEqual(MappedGenerator.Replace(" ", string.Empty),
				Hashes.BinaryToString(Generator).ToUpperInvariant());

			// Ephemeral keys

			byte[] LocalPrivateEphemeralKey = PrimeFieldCurve.ToByteSecret(EphemeralTerminalPrivateKey);
			PointOnCurve P1 = EecProtocol.Curve.ScalarMultiplication(LocalPrivateEphemeralKey, Ĝ, true);
			byte[] LocalPublicEphemeralKey = EecProtocol.Curve.Encode(P1, true);

			Assert.AreEqual(EphemeralTerminalPublicKey.Replace(" ", string.Empty),
				Hashes.BinaryToString(LocalPublicEphemeralKey).ToUpperInvariant());

			byte[] RemotePrivateEphemeralKey = PrimeFieldCurve.ToByteSecret(EphemeralChipPrivateKey);
			PointOnCurve P2 = EecProtocol.Curve.ScalarMultiplication(RemotePrivateEphemeralKey, Ĝ, true);
			byte[] RemotePublicEphemeralKey = EecProtocol.Curve.Encode(P2, true);

			Assert.AreEqual(EphemeralChipPublicKey.Replace(" ", string.Empty),
				Hashes.BinaryToString(RemotePublicEphemeralKey).ToUpperInvariant());

			// Ephemeral shared secret

			int c = RemotePublicEphemeralKey.Length;
			int c2 = c >> 1;
			byte[] RemotePublicEphemeralKeyX = new byte[c2];
			byte[] RemotePublicEphemeralKeyY = new byte[c2];

			Buffer.BlockCopy(RemotePublicEphemeralKey, 0, RemotePublicEphemeralKeyX, 0, c2);
			Buffer.BlockCopy(RemotePublicEphemeralKey, c2, RemotePublicEphemeralKeyY, 0, c2);

			Array.Reverse(RemotePublicEphemeralKeyX);
			Array.Reverse(RemotePublicEphemeralKeyY);

			PointOnCurve RemotePublicEphemeralPoint = new(
				EllipticCurve.ToInt(RemotePublicEphemeralKeyX),
				EllipticCurve.ToInt(RemotePublicEphemeralKeyY));

			PointOnCurve EphemeralSharedPoint = EecProtocol.Curve.ScalarMultiplication(
				LocalPrivateEphemeralKey, RemotePublicEphemeralPoint, true);

			byte[] EphemeralSharedPointX = EphemeralSharedPoint.X.ToByteArray();

			if (EphemeralSharedPointX.Length != EecProtocol.Curve.OrderBytes)
				Array.Resize(ref EphemeralSharedPointX, EecProtocol.Curve.OrderBytes);

			Array.Reverse(EphemeralSharedPointX);

			Assert.AreEqual(EphemeralSharedSecret.Replace(" ", string.Empty),
				Hashes.BinaryToString(EphemeralSharedPointX).ToUpperInvariant());

			// Session keys

			byte[] KS_Enc = EecProtocol.KDF_Enc(EphemeralSharedPointX);
			Assert.AreEqual(EncryptionKey.Replace(" ", string.Empty),
				Hashes.BinaryToString(KS_Enc).ToUpperInvariant());

			byte[] KS_Mac = EecProtocol.KDF_Mac(EphemeralSharedPointX);
			Assert.AreEqual(SignatureKey.Replace(" ", string.Empty),
				Hashes.BinaryToString(KS_Mac).ToUpperInvariant());

			// Associated Data

			byte[] AD_IFD = PaceProtocol.CreateAssociatedData(Oid, RemotePublicEphemeralKey);
			byte[] AD_IC = PaceProtocol.CreateAssociatedData(Oid, LocalPublicEphemeralKey);

			Assert.AreEqual(InputDataTerminal.Replace(" ", string.Empty),
				Hashes.BinaryToString(AD_IFD).ToUpperInvariant());

			Assert.AreEqual(InputDataChip.Replace(" ", string.Empty),
				Hashes.BinaryToString(AD_IC).ToUpperInvariant());

			// Computing MAC

			CMac Mac = CMac.CreateAes128CMac(KS_Mac);

			byte[] T_IFD = Mac.Sign(AD_IFD, 8);
			byte[] T_IC = Mac.Sign(AD_IC, 8);

			Assert.AreEqual(TokenTerminal.Replace(" ", string.Empty),
				Hashes.BinaryToString(T_IFD).ToUpperInvariant());

			Assert.AreEqual(TokenChip.Replace(" ", string.Empty),
				Hashes.BinaryToString(T_IC).ToUpperInvariant());
		}
	}
}
