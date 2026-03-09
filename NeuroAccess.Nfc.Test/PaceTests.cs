using NeuroAccess.Nfc.Extensions;
using NeuroAccess.Nfc.Extensions.PACE;
using Waher.Runtime.Inventory;
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
		public void Test_01_Seed()
		{
			string Mrz = "P<GBRDOE<<JOE<<<<<<<<<<<<<<<<<<<<<<<<<<<<<<<\nT220001293GBR6408125M1010318<<<<<<<<<<<<<<06";
			Assert.IsTrue(TravelDocuments.ParseMrz(Mrz, out DocumentInformation? Info));
			Assert.AreEqual("T22000129", Info!.DocumentNumber);
			Assert.AreEqual("640812", Info.DateOfBirth);
			Assert.AreEqual("101031", Info.ExpiryDate);
			Assert.AreEqual("T22000129364081251010318", Info.MRZ_Information);
			Assert.AreEqual("89DED1B26624EC1E634C1989302849DD", Hashes.BinaryToString(Info.KSeed()).ToUpperInvariant());
		}

		[TestMethod]
		[DataRow("MSgwEgYKBAB/AAcCAgQCBAIBAgIBEDASBgoEAH8ABwICBAYEAgECAgEQ", true)]
		public void Test_02_Parse_EF_CardAccess(string CardAccess, bool IsBase64)
		{
			byte[] Bin = Decode(CardAccess, IsBase64);
			Assert.IsTrue(TravelDocuments.TryDecodeDER(Bin, out object? Value));
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
			Assert.IsTrue(TravelDocuments.TryDecodeDER(Bin, out object? Value));
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

			PaceEecProtocol? EecProtocol = Protocol as PaceEecProtocol;
			Assert.IsNotNull(EecProtocol);

			Console.Out.WriteLine(EecProtocol.GetType().FullName);
			Console.Out.WriteLine(EecProtocol.Curve!.CurveName);

			Assert.AreEqual(CurveType, EecProtocol.Curve?.GetType());
		}

		[TestMethod]
		[DataRow("3012060A 04007F00 07020204 02020201 0202010D",
			new uint[] { 0x5D8BB87B, 0xD74D985A, 0x4B7D4325, 0xB9F7B976, 0xFE835122, 0x77340079, 0x8914AA22, 0x738135CC },
			"7F1D410A DB7DDB3B 84BF1030 800981A9 105D7457 B4A3ADE0 02384F30 86C67EDE 1AB88910 4A27DB6D 842B0190 20FBF3CE ACB0DC62 7F7BDCAC 29969E19 D0E553C1",
			new uint[] { 0x9E56A6B5, 0x9C95D06E, 0xCE5CD10F, 0x983BB2F4, 0xF1943528, 0xE577F238, 0x81D89D8C, 0x3BBEE0AA },
			"A234236A A9B9621E 8EFB73B5 245C0E09 D2576E52 77183C12 08BDD552 80CAE8B3 04F36571 3A356E65 A451E165 ECC9AC0A C46E3771 342C8FE5 AEDD0926 85338E23",
			"2C1DCC17 73346492 C6636A36 EE4B965E 292E9AAE 7EE37736 EF58B9D0 A043F348 403A8CF3 3CA7DC0D 9DF61D08 89CE2442 4FF97C1A AD48A5CA 2A554B07 1EF7638D ",
			false, typeof(BrainpoolP256))]
		public void Test_04_Derive_Shared_Secret(string CardAccess, uint[] TermionalPrivateKey,
			string TerminalPublicKey, uint[] ChipPrivateKey, string ChipPublicKey,
			string SharedSecret, bool IsBase64, Type CurveType)
		{
			byte[] Bin = Decode(CardAccess, IsBase64);
			Assert.IsTrue(TravelDocuments.TryDecodeDER(Bin, out object? Value));
			Array? SecurityInfo = (Array)Value!;
			string? Oid = (string)SecurityInfo.GetValue(0)!;
			PaceEecProtocol? EecProtocol = (PaceEecProtocol)Types.FindBest<IPaceProtocol, string>(Oid);
			Assert.IsTrue(EecProtocol!.Configure(SecurityInfo));

			EecProtocol.SetPrivateKey(PrimeFieldCurve.ToByteSecret(TermionalPrivateKey));
			Assert.AreEqual(TerminalPublicKey.Replace(" ", string.Empty),
				Hashes.BinaryToString(EecProtocol.Curve!.PublicKeyBigEndian).ToUpperInvariant());

			EllipticCurve ChipCurve = (EllipticCurve)Types.Instantiate(CurveType);
			ChipCurve.SetPrivateKey(PrimeFieldCurve.ToByteSecret(ChipPrivateKey));
			Assert.AreEqual(ChipPublicKey.Replace(" ", string.Empty),
				Hashes.BinaryToString(ChipCurve.PublicKeyBigEndian).ToUpperInvariant());

			byte[] SharedSecret1 = EecProtocol.GetSharedSecret(ChipCurve.PublicKey);
			byte[] SharedSecret2 = ChipCurve.GetSharedKey(EecProtocol.Curve.PublicKey, EecProtocol.HashFunction);

			Assert.AreEqual(
				Hashes.BinaryToString(SharedSecret1).ToUpperInvariant(),
				Hashes.BinaryToString(SharedSecret2).ToUpperInvariant());

			byte[] SharedSecretNoHash = ChipCurve.GetSharedKey(EecProtocol.Curve.PublicKey, NoHash);
			Console.Out.WriteLine("Shared Secret (no hash): " + Hashes.BinaryToString(SharedSecretNoHash).ToUpperInvariant());

			Assert.AreEqual(SharedSecret.Replace(" ", string.Empty),
				Hashes.BinaryToString(SharedSecretNoHash).ToUpperInvariant());
		}

		private static byte[] NoHash(byte[] Data)
		{
			return Data;
		}
	}
}
