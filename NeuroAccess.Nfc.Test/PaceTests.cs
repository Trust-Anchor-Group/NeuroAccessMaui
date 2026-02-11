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
			byte[] Bin = IsBase64 ? Convert.FromBase64String(CardAccess) : Hashes.StringToBinary(CardAccess);
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

		[TestMethod]
		[DataRow("3012060A 04007F00 07020204 02020201 0202010D", false)]
		public void Test_03_Parse_SecurityInfo(string CardAccess, bool IsBase64)
		{
			byte[] Bin = IsBase64 ? Convert.FromBase64String(CardAccess) : Hashes.StringToBinary(CardAccess);
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

			Assert.AreEqual(typeof(BrainpoolP256), EecProtocol.Curve?.GetType());
		}
	}
}
