using NeuroAccess.Nfc.TravelDocuments;

namespace NeuroAccess.Nfc.Test
{
	[TestClass]
	public class MrzCountryCodeTests
	{
		// Germany uses the single-letter code "D", padded with filler as "D<<", ICAO Doc 9303-3 §5:
		// https://www.icao.int/sites/default/files/publications/DocSeries/9303_p3_cons_en.pdf

		[TestMethod]
		public void Test_01_Parse_MRZ_TD3_Germany()
		{
			string Mrz = "P<D<<MUSTERMANN<<ERIKA<<<<<<<<<<<<<<<<<<<<<<\nC01X00T478D<<6408125F2702283<<<<<<<<<<<<<<04";
			MrzValidationResult Result = MrzValidator.Validate(Mrz);
			Assert.IsTrue(Result.IsParsed, Result.FailureReason);
			Assert.AreEqual(MrzLayout.Td3, Result.Layout);
			Assert.AreEqual("DEU", Result.DocumentInformation!.IssuingState);
			Assert.AreEqual("DEU", Result.DocumentInformation.Nationality);
			Assert.AreEqual("C01X00T47", Result.DocumentInformation.DocumentNumber);
			Assert.AreEqual(Result.RequiredCheckCount, Result.PassedRequiredCheckCount);
		}

		[TestMethod]
		public void Test_02_Parse_MRZ_TD1_Germany()
		{
			string Mrz = "IDD<<T220001293<<<<<<<<<<<<<<<\n6408125F3108011D<<<<<<<<<<<<<2\nMUSTERMANN<<ERIKA<<<<<<<<<<<<<";
			MrzValidationResult Result = MrzValidator.Validate(Mrz);
			Assert.IsTrue(Result.IsParsed, Result.FailureReason);
			Assert.AreEqual(MrzLayout.Td1, Result.Layout);
			Assert.AreEqual("DEU", Result.DocumentInformation!.IssuingState);
			Assert.AreEqual("DEU", Result.DocumentInformation.Nationality);
			Assert.AreEqual("T22000129", Result.DocumentInformation.DocumentNumber);
			Assert.AreEqual(Result.RequiredCheckCount, Result.PassedRequiredCheckCount);
		}

		[TestMethod]
		public void Test_03_ParseMrz_Germany()
		{
			string Mrz = "P<D<<MUSTERMANN<<ERIKA<<<<<<<<<<<<<<<<<<<<<<\nC01X00T478D<<6408125F2702283<<<<<<<<<<<<<<04";
			Assert.IsTrue(MrzExtensions.ParseMrz(Mrz, out DocumentInformation? Info));
			Assert.AreEqual("DEU", Info!.IssuingState);
			Assert.AreEqual("DEU", Info.Nationality);
		}
	}
}
