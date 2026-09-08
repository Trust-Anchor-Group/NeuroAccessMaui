using NeuroAccess.Nfc.TravelDocuments;
using System.Globalization;
using Waher.Security;

namespace NeuroAccess.Nfc.Test
{
	[TestClass]
	public class BacTests
	{
		// Testing parsing of Machine-Readable string on identity documents, in accordance with ICAO Doc 9303
		// Reference tests: Appendix D, https://www.icao.int/sites/default/files/publications/DocSeries/9303_p11_cons_en.pdf

		[TestMethod]
		public void Test_01_Parse_MRZ_TD2_9charsplus()
		{
			// Example from Appendix D.2, Example 1, https://www.icao.int/sites/default/files/publications/DocSeries/9303_p11_cons_en.pdf

			string Mrz = "I<UTOSTEVENSON<<PETER<JOHN<<<<<<<<<<\nD23145890<UTO3407127M95071227349<<<8";
			Assert.IsTrue(MrzExtensions.ParseMrz(Mrz, out DocumentInformation? Info));
			Assert.AreEqual("I", Info!.DocumentType);
			Assert.AreEqual("UTO", Info.IssuingState);
			Assert.AreEqual("UTO", Info.Nationality);
			Assert.HasCount(1, Info.PrimaryIdentifier!);
			Assert.AreEqual("STEVENSON", Info.PrimaryIdentifier![0]);
			Assert.HasCount(2, Info.SecondaryIdentifier!);
			Assert.AreEqual("PETER", Info.SecondaryIdentifier![0]);
			Assert.AreEqual("JOHN", Info.SecondaryIdentifier[1]);
			Assert.AreEqual("M", Info.Gender);
			Assert.AreEqual("D23145890734", Info.DocumentNumber);
			Assert.AreEqual("340712", Info.DateOfBirth);
			Assert.AreEqual("950712", Info.ExpiryDate);
			Assert.AreEqual("D23145890734934071279507122", Info.MRZ_Information);

			string KSeed = Hashes.BinaryToString(TravelDocumentsClient.BAC_KSeed(Info));
			Console.Out.WriteLine("KSeed: " + KSeed);

			string KEnc = Hashes.BinaryToString(TravelDocumentsClient.BAC_KEnc(Info));
			Console.Out.WriteLine("KEnc: " + KEnc);

			string KMac = Hashes.BinaryToString(TravelDocumentsClient.BAC_KMac(Info));
			Console.Out.WriteLine("KMac: " + KMac);
		}

		[TestMethod]
		public void Test_02_Parse_MRZ_TD2_9chars()
		{
			// Example from Appendix D.2, Example 2, https://www.icao.int/sites/default/files/publications/DocSeries/9303_p11_cons_en.pdf

			string Mrz = "I<UTOERIKSSON<<ANNA<MARIA<<<<<<<<<<<\nL898902C<3UTO6908061F9406236<<<<<<<2";
			Assert.IsTrue(MrzExtensions.ParseMrz(Mrz, out DocumentInformation? Info));
			Assert.AreEqual("I", Info!.DocumentType);
			Assert.AreEqual("UTO", Info.IssuingState);
			Assert.AreEqual("UTO", Info.Nationality);
			Assert.HasCount(1, Info.PrimaryIdentifier!);
			Assert.AreEqual("ERIKSSON", Info.PrimaryIdentifier![0]);
			Assert.HasCount(2, Info.SecondaryIdentifier!);
			Assert.AreEqual("ANNA", Info.SecondaryIdentifier![0]);
			Assert.AreEqual("MARIA", Info.SecondaryIdentifier[1]);
			Assert.AreEqual("F", Info.Gender);
			Assert.AreEqual("L898902C", Info.DocumentNumber);
			Assert.AreEqual("690806", Info.DateOfBirth);
			Assert.AreEqual("940623", Info.ExpiryDate);
			Assert.AreEqual("L898902C<369080619406236", Info.MRZ_Information);

			// Example from Appendix D.1, https://www.icao.int/sites/default/files/publications/DocSeries/9303_p11_cons_en.pdf
			// Example from Appendix D.2, Example 4, https://www.icao.int/sites/default/files/publications/DocSeries/9303_p11_cons_en.pdf

			string KSeed = Hashes.BinaryToString(TravelDocumentsClient.BAC_KSeed(Info));
			Console.Out.WriteLine("KSeed: " + KSeed);
			Assert.AreEqual("239AB9CB282DAF66231DC5A4DF6BFBAE", KSeed.ToUpper(CultureInfo.InvariantCulture));

			string KEnc = Hashes.BinaryToString(TravelDocumentsClient.BAC_KEnc(Info));
			Console.Out.WriteLine("KEnc: " + KEnc);
			Assert.AreEqual("AB94FDECF2674FDFB9B391F85D7F76F2", KEnc.ToUpper(CultureInfo.InvariantCulture));

			string KMac = Hashes.BinaryToString(TravelDocumentsClient.BAC_KMac(Info));
			Console.Out.WriteLine("KMac: " + KMac);
			Assert.AreEqual("7962D9ECE03D1ACD4C76089DCE131543", KMac.ToUpper(CultureInfo.InvariantCulture));
		}

		[TestMethod]
		public void Test_03_Parse_MRZ_TD1_9charsplus()
		{
			// Example from Appendix D.2, Example 3, https://www.icao.int/sites/default/files/publications/DocSeries/9303_p11_cons_en.pdf

			string Mrz = "I<UTOD23145890<7349<<<<<<<<<<<\n3407127M9507122UTO<<<<<<<<<<<2\nSTEVENSON<<PETER<JOHN<<<<<<<<<";
			Assert.IsTrue(MrzExtensions.ParseMrz(Mrz, out DocumentInformation? Info));
			Assert.AreEqual("I", Info!.DocumentType);
			Assert.AreEqual("UTO", Info.IssuingState);
			Assert.AreEqual("UTO", Info.Nationality);
			Assert.HasCount(1, Info.PrimaryIdentifier!);
			Assert.AreEqual("STEVENSON", Info.PrimaryIdentifier![0]);
			Assert.HasCount(2, Info.SecondaryIdentifier!);
			Assert.AreEqual("PETER", Info.SecondaryIdentifier![0]);
			Assert.AreEqual("JOHN", Info.SecondaryIdentifier[1]);
			Assert.AreEqual("M", Info.Gender);
			Assert.AreEqual("D23145890734", Info.DocumentNumber);
			Assert.AreEqual("340712", Info.DateOfBirth);
			Assert.AreEqual("950712", Info.ExpiryDate);
			Assert.AreEqual("D23145890734934071279507122", Info.MRZ_Information);

			string KSeed = Hashes.BinaryToString(TravelDocumentsClient.BAC_KSeed(Info));
			Console.Out.WriteLine("KSeed: " + KSeed);

			string KEnc = Hashes.BinaryToString(TravelDocumentsClient.BAC_KEnc(Info));
			Console.Out.WriteLine("KEnc: " + KEnc);

			string KMac = Hashes.BinaryToString(TravelDocumentsClient.BAC_KMac(Info));
			Console.Out.WriteLine("KMac: " + KMac);
		}

		[TestMethod]
		public void Test_04_Parse_MRZ_TD1_9chars()
		{
			// Example from Appendix D.2, Example 4, https://www.icao.int/sites/default/files/publications/DocSeries/9303_p11_cons_en.pdf

			string Mrz = "I<UTOL898902C<3<<<<<<<<<<<<<<<\n6908061F9406236UTO<<<<<<<<<<<2\nERIKSSON<<ANNA<MARIA<<<<<<<<<<";
			Assert.IsTrue(MrzExtensions.ParseMrz(Mrz, out DocumentInformation? Info));
			Assert.AreEqual("I", Info!.DocumentType);
			Assert.AreEqual("UTO", Info.IssuingState);
			Assert.AreEqual("UTO", Info.Nationality);
			Assert.HasCount(1, Info.PrimaryIdentifier!);
			Assert.AreEqual("ERIKSSON", Info.PrimaryIdentifier![0]);
			Assert.HasCount(2, Info.SecondaryIdentifier!);
			Assert.AreEqual("ANNA", Info.SecondaryIdentifier![0]);
			Assert.AreEqual("MARIA", Info.SecondaryIdentifier[1]);
			Assert.AreEqual("F", Info.Gender);
			Assert.AreEqual("L898902C", Info.DocumentNumber);
			Assert.AreEqual("690806", Info.DateOfBirth);
			Assert.AreEqual("940623", Info.ExpiryDate);
			Assert.AreEqual("L898902C<369080619406236", Info.MRZ_Information);

			string KSeed = Hashes.BinaryToString(TravelDocumentsClient.BAC_KSeed(Info));
			Console.Out.WriteLine("KSeed: " + KSeed);
			Assert.AreEqual("239AB9CB282DAF66231DC5A4DF6BFBAE", KSeed.ToUpper(CultureInfo.InvariantCulture));

			string KEnc = Hashes.BinaryToString(TravelDocumentsClient.BAC_KEnc(Info));
			Console.Out.WriteLine("KEnc: " + KEnc);
			Assert.AreEqual("AB94FDECF2674FDFB9B391F85D7F76F2", KEnc.ToUpper(CultureInfo.InvariantCulture));
			// KEnc corresponds to Ka | Kb (c=1) in Appendix D.1 §4

			string KMac = Hashes.BinaryToString(TravelDocumentsClient.BAC_KMac(Info));
			Console.Out.WriteLine("KMac: " + KMac);
			Assert.AreEqual("7962D9ECE03D1ACD4C76089DCE131543", KMac.ToUpper(CultureInfo.InvariantCulture));
			// KMac corresponds to Ka | Kb (c=2) in Appendix D.1 §4
		}

		[TestMethod]
		public void Test_05_Parse_MRZ()
		{
			// Section 3.1, ICAO 9303-3, https://www.icao.int/sites/default/files/publications/DocSeries/9303_p3_cons_en.pdf

			string Mrz = "P<UTOERIKSSON<<ANNA<MARIA<<<<<<<<<<<<<<<<<<<\nL898902C36UTO7408122F1204159ZE184226B<<<<<10";
			Assert.IsTrue(MrzExtensions.ParseMrz(Mrz, out _));
		}

		[TestMethod]
		public void Test_06_Parse_MRZ()
		{
			// Appendix B, ICAO 9303-5, https://www.icao.int/sites/default/files/publications/DocSeries/9303_p5_cons_en.pdf

			string Mrz = "I<UTOD231458907<<<<<<<<<<<<<<<\n7408122F1204159UTO<<<<<<<<<<<6\nERIKSSON<<ANNA<MARIA<<<<<<<<<<";
			Assert.IsTrue(MrzExtensions.ParseMrz(Mrz, out _));
		}

		[TestMethod]
		public void Test_07_Parse_MRZ()
		{
			// Appendix B, ICAO 9303-6, https://www.icao.int/sites/default/files/publications/DocSeries/9303_p6_cons_en.pdf

			string Mrz = "I<UTOERIKSSON<<ANNA<MARIA<<<<<<<<<<<\nD231458907UTO7408122F1204159<<<<<<<6";
			Assert.IsTrue(MrzExtensions.ParseMrz(Mrz, out _));
		}

		[TestMethod]
		public void Test_08_Validate_MRZ_TD3_CheckDigits()
		{
			// Section 3.1, ICAO 9303-3, https://www.icao.int/sites/default/files/publications/DocSeries/9303_p3_cons_en.pdf

			string Mrz = "P<UTOERIKSSON<<ANNA<MARIA<<<<<<<<<<<<<<<<<<<\nL898902C36UTO7408122F1204159ZE184226B<<<<<10";
			MrzValidationResult Result = MrzValidator.Validate(Mrz);

			Assert.IsTrue(Result.IsParsed);
			Assert.AreEqual(MrzLayout.Td3, Result.Layout);
			Assert.IsTrue(Result.DocumentNumberCheckPassed);
			Assert.IsTrue(Result.DateOfBirthCheckPassed);
			Assert.IsTrue(Result.ExpiryCheckPassed);
			Assert.IsTrue(Result.CompositeCheckPassed);
			Assert.AreEqual(4, Result.RequiredCheckCount);
			Assert.AreEqual(4, Result.PassedRequiredCheckCount);
		}

		[TestMethod]
		public void Test_09_Validate_MRZ_TD1_CheckDigits()
		{
			// Example from Appendix D.2, Example 4, https://www.icao.int/sites/default/files/publications/DocSeries/9303_p11_cons_en.pdf

			string Mrz = "I<UTOL898902C<3<<<<<<<<<<<<<<<\n6908061F9406236UTO<<<<<<<<<<<2\nERIKSSON<<ANNA<MARIA<<<<<<<<<<";
			MrzValidationResult Result = MrzValidator.Validate(Mrz);

			Assert.IsTrue(Result.IsParsed);
			Assert.AreEqual(MrzLayout.Td1, Result.Layout);
			Assert.IsTrue(Result.DocumentNumberCheckPassed);
			Assert.IsTrue(Result.DateOfBirthCheckPassed);
			Assert.IsTrue(Result.ExpiryCheckPassed);
			Assert.IsTrue(Result.CompositeCheckPassed);
			Assert.AreEqual(4, Result.RequiredCheckCount);
			Assert.AreEqual(4, Result.PassedRequiredCheckCount);
		}

		[TestMethod]
		public void Test_10_Validate_MRZ_TD2_Rejects_Bad_Composite_Check()
		{
			// Valid base example from Appendix D.2, Example 2:
			// https://www.icao.int/sites/default/files/publications/DocSeries/9303_p11_cons_en.pdf

			string Mrz = "I<UTOERIKSSON<<ANNA<MARIA<<<<<<<<<<<\nL898902C<3UTO6908061F9406236<<<<<<<1";
			MrzValidationResult Result = MrzValidator.Validate(Mrz);

			Assert.IsFalse(Result.IsParsed);
			Assert.AreEqual(MrzLayout.Td2, Result.Layout);
			Assert.IsTrue(Result.DocumentNumberCheckPassed);
			Assert.IsTrue(Result.DateOfBirthCheckPassed);
			Assert.IsTrue(Result.ExpiryCheckPassed);
			Assert.IsFalse(Result.CompositeCheckPassed);
			Assert.AreEqual(4, Result.RequiredCheckCount);
			Assert.AreEqual(3, Result.PassedRequiredCheckCount);
		}

		[TestMethod]
		public void Test_11_Validate_MRZ_TD3_Rejects_Bad_Document_Number_Check()
		{
			// Valid base example from Section 3.1, ICAO 9303-3:
			// https://www.icao.int/sites/default/files/publications/DocSeries/9303_p3_cons_en.pdf

			string Mrz = "P<UTOERIKSSON<<ANNA<MARIA<<<<<<<<<<<<<<<<<<<\nL898902C37UTO7408122F1204159ZE184226B<<<<<10";
			MrzValidationResult Result = MrzValidator.Validate(Mrz);

			Assert.IsFalse(Result.IsParsed);
			Assert.AreEqual(MrzLayout.Td3, Result.Layout);
			Assert.IsFalse(Result.DocumentNumberCheckPassed);
			Assert.IsTrue(Result.DateOfBirthCheckPassed);
			Assert.IsTrue(Result.ExpiryCheckPassed);
			Assert.IsFalse(Result.CompositeCheckPassed);
			Assert.AreEqual(4, Result.RequiredCheckCount);
			Assert.AreEqual(2, Result.PassedRequiredCheckCount);
		}

		[TestMethod]
		public void Test_12_BAC_ChallengeResponse()
		{
			// Example from Appendix D.3, https://www.icao.int/sites/default/files/publications/DocSeries/9303_p11_cons_en.pdf
			// Example from Appendix D.2, Example 4, https://www.icao.int/sites/default/files/publications/DocSeries/9303_p11_cons_en.pdf

			byte[] Challenge = Hashes.StringToBinary("4608F91988702212");               // RND.IC
			byte[] Rnd1 = Hashes.StringToBinary("781723860C06C226");                    // RND.IFD
			byte[] Rnd2 = Hashes.StringToBinary("0B795240CB7049B01C19B33E32804F0B");    // K.IFD
			byte[] KEnc = Hashes.StringToBinary("AB94FDECF2674FDFB9B391F85D7F76F2");
			byte[] KMac = Hashes.StringToBinary("7962D9ECE03D1ACD4C76089DCE131543");

			byte[] Response = TravelDocumentsClient.CalcBacChallengeResponse3DES(Challenge, Rnd1, Rnd2, KEnc, KMac, null);

			Assert.AreEqual("72C29C2371CC9BDB65B779B8E8D37B29ECC154AA56A8799FAE2F498F76ED92F25F1448EEA8AD90A7",
				Hashes.BinaryToString(Response).ToUpper(CultureInfo.InvariantCulture));
			// Corresponds to cmd_data in Appendix D.3, §6
			//
			// Returned from the card when sending challenge response: 
			// resp_data = 46B9342A41396CD7386BF5803104D7CEDC122B9132139BAF2EEDC94EE178534F2F2D235D074D7449

			byte[] RespData = Hashes.StringToBinary("46B9342A41396CD7386BF5803104D7CEDC122B9132139BAF2EEDC94EE178534F2F2D235D074D7449");

			Assert.IsTrue(TravelDocumentsClient.AuthenticateBacResponseData(RespData, Challenge,
				Rnd2, KEnc, KMac, out byte[]? KIC, out byte[]? KSEnc, out byte[]? KSMac,
				out byte[]? Ssc, null));

			Assert.IsNotNull(KIC);
			Assert.IsNotNull(KSEnc);
			Assert.IsNotNull(KSMac);
			Assert.IsNotNull(Ssc);

			Assert.AreEqual("0B4F80323EB3191CB04970CB4052790B",
				Hashes.BinaryToString(KIC).ToUpper(CultureInfo.InvariantCulture));

			Assert.AreEqual("979EC13B1CBFE9DCD01AB0FED307EAE5",
				Hashes.BinaryToString(KSEnc).ToUpper(CultureInfo.InvariantCulture));

			Assert.AreEqual("F1CB1F1FB5ADF208806B89DC579DC1F8",
				Hashes.BinaryToString(KSMac).ToUpper(CultureInfo.InvariantCulture));

			Assert.AreEqual("887022120C06C226",
				Hashes.BinaryToString(Ssc).ToUpper(CultureInfo.InvariantCulture));

			/* IC part:
			
			byte[] EIC = new byte[32];
			byte[] MIC = new byte[8];

			Buffer.BlockCopy(RespData, 0, EIC, 0, 32);
			Buffer.BlockCopy(RespData, 32, MIC, 0, 8);

			byte[] Rnd3 = Hashes.StringToBinary("0B4F80323EB3191CB04970CB4052790B");    // K.IC
			byte[] KSeed = TravelDocumentsClient.XOR(Rnd2, Rnd3);

			Assert.AreEqual("0036D272F5C350ACAC50C3F572D23600",
				Hashes.BinaryToString(KSeed).ToUpper(CultureInfo.InvariantCulture));

			byte[] KSEnc = TravelDocumentsClient.BAC_KSEnc(KSeed);
			Console.Out.WriteLine("KSEnc: " + Hashes.BinaryToString(KSEnc));

			byte[] KSMac = TravelDocumentsClient.BAC_KSMac(KSeed);
			Console.Out.WriteLine("KSMac: " + Hashes.BinaryToString(KSMac));

			Assert.AreEqual("979EC13B1CBFE9DCD01AB0FED307EAE5",
				Hashes.BinaryToString(KSEnc).ToUpper(CultureInfo.InvariantCulture));

			Assert.AreEqual("F1CB1F1FB5ADF208806B89DC579DC1F8",
				Hashes.BinaryToString(KSMac).ToUpper(CultureInfo.InvariantCulture));

			byte[] Ssc = new byte[8];   // Send sequence counter

			Buffer.BlockCopy(Challenge, Challenge.Length - 4, Ssc, 0, 4);
			Buffer.BlockCopy(Rnd1, Rnd1.Length - 4, Ssc, 4, 4);

			Assert.AreEqual("887022120C06C226",
				Hashes.BinaryToString(Ssc).ToUpper(CultureInfo.InvariantCulture));

			byte[] R = TravelDocumentsClient.CONCAT(Challenge, Rnd1, Rnd3);

			Assert.AreEqual("4608F91988702212781723860C06C2260B4F80323EB3191CB04970CB4052790B",
				Hashes.BinaryToString(R).ToUpper(CultureInfo.InvariantCulture));

			byte[] RespData2 = TravelDocumentsClient.CalcChallengeResponse3DES(R, [], [], KEnc, KMac);

			Buffer.BlockCopy(RespData2, 0, EIC, 0, 32);
			Buffer.BlockCopy(RespData2, 32, MIC, 0, 8);

			Assert.AreEqual("46B9342A41396CD7386BF5803104D7CEDC122B9132139BAF2EEDC94EE178534F",
				Hashes.BinaryToString(EIC).ToUpper(CultureInfo.InvariantCulture));

			Assert.AreEqual("2F2D235D074D7449",
				Hashes.BinaryToString(MIC).ToUpper(CultureInfo.InvariantCulture));

			*/

		}

		[TestMethod]
		public void Test_13_BAC_ChallengeResponse()
		{
			// Ref: https://sourceforge.net/p/jmrtd/discussion/580232/thread/1131f402/

			DocumentInformation Info = new()
			{
				MRZ_Information = "GF043591<586012072309062"
			};

			byte[] KEnc = TravelDocumentsClient.BAC_KEnc(Info);
			Assert.AreEqual("BA43433BF47AAEF875234FDF3208206D", Hashes.BinaryToString(KEnc).ToUpper(CultureInfo.InvariantCulture));

			byte[] KMac = TravelDocumentsClient.BAC_KMac(Info);
			Assert.AreEqual("EA6445CD622CEAECBF7C9B7CB020B95D", Hashes.BinaryToString(KMac).ToUpper(CultureInfo.InvariantCulture));

			byte[] Challenge = Hashes.StringToBinary("8EAF826F89F1E525");               // RND.ICC
			byte[] Rnd1 = Hashes.StringToBinary("23E85A993A9AC5B4");                    // RND.IFD
			byte[] Rnd2 = Hashes.StringToBinary("75DC87E50C8EF30047D0B5325E83204D");    // K.IFD

			byte[] Response = TravelDocumentsClient.CalcBacChallengeResponse3DES(Challenge, Rnd1, Rnd2, KEnc, KMac, null);

			Assert.AreEqual("4782B1700DD4F60373DA6632FCD1AB1E500D46FA11DEBDF9B88C39FCA7FDF8DB" +    // E.IFD
				"BE51F41D52D4B879",                                                                 // MAC
				Hashes.BinaryToString(Response).ToUpper(CultureInfo.InvariantCulture));
		}
	}
}
