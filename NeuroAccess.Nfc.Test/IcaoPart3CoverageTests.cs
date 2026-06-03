using System.Xml;
using System.Xml.Linq;
using System.Xml.Schema;
using System.Globalization;
using NeuroAccess.Nfc.Test.IcaoPart3;
using NeuroAccess.Nfc.TravelDocuments;
using NeuroAccess.Nfc.TravelDocuments.PACE;
using NeuroAccess.Nfc.TravelDocuments.Security;

namespace NeuroAccess.Nfc.Test
{
	/// <summary>
	/// Tests ICAO Part 3 matrix import and v1 coverage classification.
	/// </summary>
	[TestClass]
	public class IcaoPart3CoverageTests
	{
		/// <summary>
		/// Gets or sets the MSTest context.
		/// </summary>
		public TestContext TestContext { get; set; } = null!;

		/// <summary>
		/// Verifies the committed compact ICAO matrix validates against its XSD.
		/// </summary>
		[TestMethod]
		public void Test_01_Matrix_Xml_Validates_Against_Schema()
		{
			XmlSchemaSet Schemas = new();
			Schemas.Add(null, IcaoPart3TestData.GetMatrixPath("matrix.xsd"));

			XmlReaderSettings Settings = new()
			{
				ValidationType = ValidationType.Schema,
				Schemas = Schemas
			};

			List<string> Errors = [];
			Settings.ValidationEventHandler += (_, Args) => Errors.Add(Args.Message);

			using XmlReader Reader = XmlReader.Create(IcaoPart3TestData.GetMatrixPath("matrix.xml"), Settings);
			while (Reader.Read())
			{
			}

			Assert.AreEqual(0, Errors.Count, string.Join(Environment.NewLine, Errors));
		}

		/// <summary>
		/// Verifies matrix metadata matches the converted ICAO source corpus.
		/// </summary>
		[TestMethod]
		public void Test_02_Matrix_Metadata_Matches_Source()
		{
			IcaoPart3Matrix Matrix = LoadMatrix();

			Assert.AreEqual("3.2", Matrix.Version);
			Assert.AreEqual("2025-10-17", Matrix.Date);
			Assert.AreEqual(411, Matrix.CaseCount);
			Assert.AreEqual(411, Matrix.Cases.Length);
			Assert.AreEqual(777, Matrix.StructuredApduCount);
			Assert.AreEqual(777, Matrix.Cases.SelectMany(Case => Case.Steps).SelectMany(Step => Step.Apdus).Count());
			Assert.AreEqual(Matrix.Cases.Length, Matrix.Cases.Select(Case => Case.Id).Distinct(StringComparer.Ordinal).Count());
		}

		/// <summary>
		/// Verifies every ICAO Part 3 case receives a pass, skip, or blocked classification.
		/// </summary>
		[TestMethod]
		public void Test_03_All_Cases_Are_Classified_With_Reasons()
		{
			IcaoPart3Matrix Matrix = LoadMatrix();
			IcaoPart3CoverageResult[] Results = CreateCoverageResults(Matrix);

			Assert.AreEqual(411, Results.Length);
			Assert.IsFalse(Results.Any(Result => string.IsNullOrWhiteSpace(Result.Reason)));
			Assert.IsFalse(Results.Any(Result => string.IsNullOrWhiteSpace(Result.Capability)));
			Assert.IsFalse(Results.Any(Result => Result.Status == IcaoPart3CoverageStatus.Failed),
				string.Join(Environment.NewLine, Results
					.Where(Result => Result.Status == IcaoPart3CoverageStatus.Failed)
					.Select(Result => Result.CaseId + ": " + Result.Reason)));
		}

		/// <summary>
		/// Verifies representative executable cases pass and unsupported protected cases remain blocked.
		/// </summary>
		[TestMethod]
		public void Test_04_Representative_Coverage_Statuses_Are_Stable()
		{
			IcaoPart3Matrix Matrix = LoadMatrix();
			Dictionary<string, IcaoPart3CoverageResult> Results = Matrix.Cases
				.Select(CreateCoverageClassifier().Classify)
				.ToDictionary(Result => Result.CaseId, StringComparer.Ordinal);

			Assert.AreEqual(IcaoPart3CoverageStatus.Passed, Results["ISO7816_A_1"].Status);
			Assert.AreEqual(IcaoPart3CoverageStatus.Passed, Results["ISO7816_B_1"].Status);
			Assert.AreEqual(IcaoPart3CoverageStatus.Passed, Results["ISO7816_B_19"].Status);
			Assert.AreEqual(IcaoPart3CoverageStatus.Passed, Results["LDS_A_1"].Status);
			Assert.AreEqual(IcaoPart3CoverageStatus.Passed, Results["LDS_B_1"].Status);
			Assert.AreEqual(IcaoPart3CoverageStatus.Passed, Results["LDS_C_1"].Status);
			Assert.AreEqual(IcaoPart3CoverageStatus.Passed, Results["LDS_D_1"].Status);
			Assert.AreEqual(IcaoPart3CoverageStatus.Passed, Results["LDS_N_2"].Status);
			Assert.AreEqual(IcaoPart3CoverageStatus.Passed, Results["ISO7816_D_1"].Status);
			Assert.AreEqual("PaceProtectedReadClient", Results["ISO7816_D_1"].Capability);
			Assert.AreEqual(IcaoPart3CoverageStatus.Passed, Results["ISO7816_Q_1"].Status);
			Assert.AreEqual("PaceCardAccessFile", Results["ISO7816_Q_1"].Capability);
			Assert.AreEqual(IcaoPart3CoverageStatus.Passed, Results["ISO7816_P_1"].Status);
			Assert.AreEqual("PacePositiveMrzFlow", Results["ISO7816_P_1"].Capability);
			Assert.AreEqual(IcaoPart3CoverageStatus.Passed, Results["ISO7816_P_3"].Status);
			Assert.AreEqual("PacePostAuthenticationSecurityPolicy", Results["ISO7816_P_3"].Capability);
			Assert.AreEqual(IcaoPart3CoverageStatus.Passed, Results["ISO7816_P_5"].Status);
			Assert.AreEqual("PaceMseSetAtValidation", Results["ISO7816_P_5"].Capability);
			Assert.AreEqual(IcaoPart3CoverageStatus.Passed, Results["ISO7816_P_11"].Status);
			Assert.AreEqual("PaceNonceStepValidation", Results["ISO7816_P_11"].Capability);
			Assert.AreEqual(IcaoPart3CoverageStatus.Passed, Results["ISO7816_P_15"].Status);
			Assert.AreEqual("PaceMappingStepValidation", Results["ISO7816_P_15"].Capability);
			Assert.AreEqual(IcaoPart3CoverageStatus.Passed, Results["ISO7816_P_19"].Status);
			Assert.AreEqual("PaceKeyAgreementStepValidation", Results["ISO7816_P_19"].Capability);
			Assert.AreEqual(IcaoPart3CoverageStatus.Passed, Results["ISO7816_P_31"].Status);
			Assert.AreEqual("PaceMutualAuthenticationStepValidation", Results["ISO7816_P_31"].Capability);
			Assert.AreEqual(IcaoPart3CoverageStatus.BlockedSecureMessaging, Results["ISO7816_C_12"].Status);
		}

		/// <summary>
		/// Verifies the virtual card supports concrete APDU behaviors needed by v1 coverage.
		/// </summary>
		[TestMethod]
		public void Test_05_Virtual_Card_Executes_Concrete_Apdu_Behavior()
		{
			JmrtdFixture Fixture = JmrtdFixtureLoader.Load("large-iso19794-jpeg");
			VirtualEmrtdCard Card = new(Fixture.Files);

			CollectionAssert.AreEqual(new byte[] { 0x90, 0x00 },
				Card.Transmit([0x00, 0xa4, 0x04, 0x0c, 0x07, 0xa0, 0x00, 0x00, 0x02, 0x47, 0x10, 0x01]));
			CollectionAssert.AreEqual(new byte[] { 0x90, 0x00 },
				Card.Transmit([0x00, 0xa4, 0x02, 0x0c, 0x02, 0x01, 0x02]));

			byte[] ReadFirstByte = Card.Transmit([0x00, 0xb0, 0x00, 0x00, 0x01]);
			Assert.AreEqual(0x75, ReadFirstByte[0]);
			Assert.AreEqual(0x90, ReadFirstByte[^2]);
			Assert.AreEqual(0x00, ReadFirstByte[^1]);

			byte[] ReadBySfi = Card.Transmit([0x00, 0xb0, 0x82, 0x00, 0x01]);
			Assert.AreEqual(0x75, ReadBySfi[0]);
			Assert.AreEqual(0x90, ReadBySfi[^2]);
			Assert.AreEqual(0x00, ReadBySfi[^1]);

			byte[] ExtendedRead = Card.Transmit([0x00, 0xb1, 0x00, 0x00, 0x05, 0x54, 0x03, 0x01, 0x00, 0x00, 0x01]);
			Assert.IsTrue(Card.SawExtendedReadBinary);
			Assert.AreEqual(0x90, ExtendedRead[^2]);
			Assert.AreEqual(0x00, ExtendedRead[^1]);
		}

		/// <summary>
		/// Verifies the committed coverage summary matches the current generated coverage.
		/// </summary>
		[TestMethod]
		public void Test_06_Coverage_Summary_Matches_Baseline()
		{
			IcaoPart3Matrix Matrix = LoadMatrix();
			IcaoPart3CoverageResult[] Results = CreateCoverageResults(Matrix);
			XDocument Summary = IcaoPart3CoverageSummary.Create(Matrix, Results);
			string SummaryText = IcaoPart3CoverageSummary.Format(Summary);
			string OutputPath = Path.Combine(this.TestContext.ResultsDirectory ?? AppContext.BaseDirectory,
				"icao-part3-coverage-summary.xml");
			File.WriteAllText(OutputPath, SummaryText);
			this.TestContext.AddResultFile(OutputPath);

			string BaselinePath = IcaoPart3TestData.GetMatrixSourcePath("coverage-summary.xml");
			if (String.Equals(Environment.GetEnvironmentVariable("UPDATE_ICAO_COVERAGE_REPORT"),
				"1", StringComparison.Ordinal))
			{
				File.WriteAllText(BaselinePath, SummaryText);
			}

			Assert.AreEqual(File.ReadAllText(BaselinePath), SummaryText);
		}

		/// <summary>
		/// Verifies the broad coverage fixture removes missing-fixture blockers.
		/// </summary>
		[TestMethod]
		public void Test_07_Coverage_Fixture_Contains_All_Matrix_Files()
		{
			IcaoPart3Matrix Matrix = LoadMatrix();
			IcaoPart3CoverageResult[] Results = CreateCoverageResults(Matrix);

			Assert.AreEqual(0, Results.Count(Result => Result.Status == IcaoPart3CoverageStatus.BlockedMissingFixture),
				string.Join(Environment.NewLine, Results
					.Where(Result => Result.Status == IcaoPart3CoverageStatus.BlockedMissingFixture)
					.Select(Result => Result.CaseId + ": " + Result.Reason)));
		}

		/// <summary>
		/// Verifies representative coverage-only fixture files can be selected and read.
		/// </summary>
		[TestMethod]
		public void Test_08_Virtual_Card_Reads_New_Coverage_Fixture_Files()
		{
			JmrtdFixture Fixture = JmrtdFixtureLoader.Load("coverage-lds-all-files");
			VirtualEmrtdCard Card = new(Fixture.Files);

			CollectionAssert.AreEqual(new byte[] { 0x90, 0x00 },
				Card.Transmit([0x00, 0xa4, 0x04, 0x0c, 0x07, 0xa0, 0x00, 0x00, 0x02, 0x47, 0x10, 0x01]));
			AssertSelectedFileRead(Card, EF.DG3, 0x63);
			AssertSelectedFileRead(Card, EF.DG12, 0x6c);
			AssertSelectedFileRead(Card, EF.DG16, 0x70);
			AssertSelectedFileRead(Card, EF.DIR, 0x61);
		}

		private static IcaoPart3Matrix LoadMatrix()
		{
			return IcaoPart3TestData.LoadMatrix();
		}

		private static IcaoPart3CoverageClassifier CreateCoverageClassifier()
		{
			return IcaoPart3TestData.CreateCoverageClassifier();
		}

		private static IcaoPart3CoverageResult[] CreateCoverageResults(IcaoPart3Matrix Matrix)
		{
			return IcaoPart3TestData.CreateCoverageResults(Matrix);
		}

		private static void AssertSelectedFileRead(VirtualEmrtdCard Card, ushort FileId, byte ExpectedFirstByte)
		{
			byte[] SelectCommand =
			[
				0x00,
				0xa4,
				0x02,
				0x0c,
				0x02,
				(byte)(FileId >> 8),
				(byte)FileId
			];
			CollectionAssert.AreEqual(new byte[] { 0x90, 0x00 }, Card.Transmit(SelectCommand));

			byte[] Response = Card.Transmit([0x00, 0xb0, 0x00, 0x00, 0x01]);
			Assert.AreEqual(ExpectedFirstByte, Response[0]);
			Assert.AreEqual(0x90, Response[^2]);
			Assert.AreEqual(0x00, Response[^1]);
		}

		/// <summary>
		/// Verifies the full matrix report exposes passed, failed, and inconclusive outcomes.
		/// </summary>
		[TestMethod]
		public void Test_09_Full_Execution_Report_Is_Written()
		{
			IcaoPart3Matrix Matrix = LoadMatrix();
			IcaoPart3CoverageResult[] Results = CreateCoverageResults(Matrix);
			XDocument Report = IcaoPart3ExecutionReport.Create(Matrix, Results, "FastDotNet");
			string ReportText = IcaoPart3ExecutionReport.Format(Report);
			string OutputPath = Path.Combine(this.TestContext.ResultsDirectory ?? AppContext.BaseDirectory,
				"icao-part3-execution-report.xml");
			File.WriteAllText(OutputPath, ReportText);
			this.TestContext.AddResultFile(OutputPath);

			string BinOutputPath = Path.Combine(AppContext.BaseDirectory, "icao-part3-execution-report.xml");
			if (!String.Equals(OutputPath, BinOutputPath, StringComparison.OrdinalIgnoreCase))
				File.WriteAllText(BinOutputPath, ReportText);

			XElement Root = Report.Root ?? throw new InvalidOperationException("Report has no root element.");
			IEnumerable<XElement> Outcomes = Root.Element("outcomes")?.Elements("outcome") ??
				throw new InvalidOperationException("Report has no outcomes element.");
			Dictionary<string, int> Counts = Outcomes.ToDictionary(
				Element => Element.Attribute("name")?.Value ?? string.Empty,
				Element => Int32.Parse(Element.Attribute("count")?.Value ?? "0", CultureInfo.InvariantCulture));

			Assert.AreEqual(411, Counts.Values.Sum());
			Assert.IsTrue(Counts["Passed"] > 0);
			Assert.AreEqual(0, Counts["Failed"]);
			Assert.IsTrue(Counts["Inconclusive"] > 0);
			Assert.AreEqual(411, Root.Element("cases")?.Elements("case").Count());
		}

		/// <summary>
		/// Verifies the production NFC client path can select and download the core LDS files used by the matrix.
		/// </summary>
		/// <returns>A task representing the asynchronous test.</returns>
		[TestMethod]
		public async Task Test_10_TravelDocumentsClient_Downloads_Core_Matrix_Files()
		{
			JmrtdFixture Fixture = JmrtdFixtureLoader.Load("coverage-lds-all-files");
			EmrtdProtocolSimulatorOptions Options = new EmrtdProtocolSimulatorOptions(Fixture.Files);
			EmrtdProtocolSimulator Simulator = new EmrtdProtocolSimulator(Options);
			EmrtdProtocolSimulatorIsoDepInterface IsoDep = new EmrtdProtocolSimulatorIsoDepInterface(Simulator);

			using TravelDocumentsClient Client = new(IsoDep, CreateDocumentInformation(), null);
			Assert.IsTrue(await Client.SelectApplication(Applications.DF1));

			await AssertClientDownloadsFile(Client, Fixture, EF.COM, "EF.COM");
			await AssertClientDownloadsFile(Client, Fixture, EF.DG1, "EF.DG1");
			await AssertClientDownloadsFile(Client, Fixture, EF.DG2, "EF.DG2");
			await AssertClientDownloadsFile(Client, Fixture, EF.SOD, "EF.SOD");
			await AssertClientDownloadsFile(Client, Fixture, EF.DG11, "EF.DG11");
		}

		/// <summary>
		/// Verifies the standards-shaped protocol simulator exposes explicit protocol state and non-faked auth hooks.
		/// </summary>
		[TestMethod]
		public void Test_11_Protocol_Simulator_Models_State_And_Auth_Entry_Points()
		{
			JmrtdFixture Fixture = JmrtdFixtureLoader.Load("coverage-lds-all-files");
			EmrtdProtocolSimulatorOptions Options = new EmrtdProtocolSimulatorOptions(Fixture.Files)
			{
				RequireSecureMessagingForDataGroups = true,
				EnableBacEntryPoints = true,
				EnablePaceEntryPoints = true
			};
			EmrtdProtocolSimulator Simulator = new EmrtdProtocolSimulator(Options);

			CollectionAssert.AreEqual(new byte[] { 0x90, 0x00 },
				Simulator.Transmit([0x00, 0xa4, 0x04, 0x0c, 0x07, 0xa0, 0x00, 0x00, 0x02, 0x47, 0x10, 0x01]));
			Assert.AreEqual(EmrtdProtocolSimulatorState.LdsApplicationSelected, Simulator.State);

			CollectionAssert.AreEqual(new byte[] { 0x69, 0x82 },
				Simulator.Transmit([0x00, 0xb0, 0x81, 0x00, 0x01]));

			CollectionAssert.AreEqual(new byte[] { 0x90, 0x00 },
				Simulator.Transmit([0x00, 0xa4, 0x00, 0x0c, 0x02, 0x3f, 0x00]));
			Assert.AreEqual(EmrtdProtocolSimulatorState.MasterFileSelected, Simulator.State);

			byte[] Challenge = Simulator.Transmit([0x00, 0x84, 0x00, 0x00, 0x08]);
			Assert.IsTrue(Simulator.SawBacGetChallenge);
			Assert.AreEqual(10, Challenge.Length);
			Assert.AreEqual(0x90, Challenge[^2]);
			Assert.AreEqual(0x00, Challenge[^1]);

			CollectionAssert.AreEqual(new byte[] { 0x90, 0x00 },
				Simulator.Transmit([0x00, 0x22, 0xc1, 0xa4, 0x12,
					0x80, 0x0a, 0x04, 0x00, 0x7f, 0x00, 0x07, 0x02, 0x02, 0x04, 0x02, 0x02,
					0x83, 0x01, 0x01,
					0x84, 0x01, 0x0d]));
			Assert.IsTrue(Simulator.SawPaceSetAt);
			Assert.AreEqual(EmrtdProtocolSimulatorState.PaceSecurityEnvironmentSelected, Simulator.State);

			CollectionAssert.AreEqual(new byte[] { 0x67, 0x00 },
				Simulator.Transmit([0x00, 0x86, 0x00, 0x00, 0x00]));
			Assert.IsTrue(Simulator.SawGeneralAuthenticate);

			CollectionAssert.AreEqual(new byte[] { 0x69, 0x82 },
				Simulator.Transmit([0x0c, 0xb0, 0x00, 0x00, 0x01]));
		}

		/// <summary>
		/// Verifies the production NFC client can perform PACE and then read through secure messaging.
		/// </summary>
		/// <returns>A task representing the asynchronous test.</returns>
		[TestMethod]
		public async Task Test_12_TravelDocumentsClient_Performs_Pace_And_Protected_Reads()
		{
			Waher.Runtime.Inventory.Types.Initialize(typeof(TravelDocumentsClient).Assembly);

			DocumentInformation Information = new DocumentInformation()
			{
				MRZ_Information = "T22000129364081251010318"
			};
			JmrtdFixture Fixture = JmrtdFixtureLoader.Load("coverage-lds-all-files");
			Dictionary<ushort, byte[]> Files = new Dictionary<ushort, byte[]>(Fixture.Files)
			{
				[EF.CardAccess] = HexToBytes("31143012060A04007F0007020204020202010202010D")
			};
			Assert.IsTrue(ASN1.TryDecodeDer(Files[EF.CardAccess], out object? CardAccess));
			Assert.IsInstanceOfType<Vector>(CardAccess);
			Assert.IsTrue(((Vector)CardAccess!).Cast<object>().Any(Item => Item is IPaceProtocol));

			EmrtdProtocolSimulatorOptions Options = new EmrtdProtocolSimulatorOptions(Files)
			{
				DocumentInformation = Information,
				RequireSecureMessagingForDataGroups = true,
				EnablePaceEntryPoints = true
			};
			EmrtdProtocolSimulator CardAccessSimulator = new EmrtdProtocolSimulator(Options);
			EmrtdProtocolSimulatorIsoDepInterface CardAccessIsoDep = new EmrtdProtocolSimulatorIsoDepInterface(CardAccessSimulator);
			using TravelDocumentsClient CardAccessClient = new(CardAccessIsoDep, Information, null);
			byte[]? DownloadedCardAccess = await CardAccessClient.DownloadFile(EF.CardAccess, "EF.CardAccess");
			CollectionAssert.AreEqual(Files[EF.CardAccess], DownloadedCardAccess);

			EmrtdProtocolSimulator Simulator = new EmrtdProtocolSimulator(Options);
			EmrtdProtocolSimulatorIsoDepInterface IsoDep = new EmrtdProtocolSimulatorIsoDepInterface(Simulator);

			using TravelDocumentsClient Client = new(IsoDep, Information, null);

			Assert.AreEqual(AuthenticateResult.Success, await Client.Authenticate());
			Assert.AreEqual(EmrtdProtocolSimulatorState.SecureMessagingEstablished, Simulator.State);
			Assert.IsTrue(await Client.SelectApplication(Applications.DF1));

			byte[]? DownloadedDg1 = await Client.DownloadFile(EF.DG1, "EF.DG1");
			Assert.IsNotNull(DownloadedDg1);
			CollectionAssert.AreEqual(Files[EF.DG1], DownloadedDg1);
		}

		/// <summary>
		/// Verifies the production NFC client can perform the full PACE-protected read flow for core LDS files.
		/// </summary>
		/// <returns>A task representing the asynchronous test.</returns>
		[TestMethod]
		public async Task Test_13_TravelDocumentsClient_Performs_Full_Pace_Core_Lds_Read_Flow()
		{
			JmrtdFixture Fixture = JmrtdFixtureLoader.Load("coverage-lds-all-files");
			await AssertCardAccessCanBeReadBeforePace(Fixture);

			TravelDocumentsClient Client = CreatePaceClient(Fixture, out EmrtdProtocolSimulator Simulator,
				out Dictionary<ushort, byte[]> Files);
			using (Client)
			{
				Assert.AreEqual(AuthenticateResult.Success, await Client.Authenticate());
				Assert.AreEqual(EmrtdProtocolSimulatorState.SecureMessagingEstablished, Simulator.State);
				Assert.IsTrue(await Client.SelectApplication(Applications.DF1));

				await AssertClientDownloadsBytes(Client, Files, EF.COM, "EF.COM");
				await AssertClientDownloadsBytes(Client, Files, EF.DG1, "EF.DG1");
				await AssertClientDownloadsBytes(Client, Files, EF.DG2, "EF.DG2");
				await AssertClientDownloadsBytes(Client, Files, EF.DG11, "EF.DG11");
				await AssertClientDownloadsBytes(Client, Files, EF.SOD, "EF.SOD");
			}
		}

		/// <summary>
		/// Verifies the production NFC client can perform PACE and then read a large DG2 file through secure messaging.
		/// </summary>
		/// <returns>A task representing the asynchronous test.</returns>
		[TestMethod]
		public async Task Test_14_TravelDocumentsClient_Performs_Full_Pace_Large_Dg2_Read_Flow()
		{
			JmrtdFixture Fixture = JmrtdFixtureLoader.Load("large-iso19794-jpeg");
			TravelDocumentsClient Client = CreatePaceClient(Fixture, out EmrtdProtocolSimulator Simulator,
				out Dictionary<ushort, byte[]> Files);
			using (Client)
			{
				Assert.AreEqual(AuthenticateResult.Success, await Client.Authenticate());
				Assert.AreEqual(EmrtdProtocolSimulatorState.SecureMessagingEstablished, Simulator.State);
				Assert.IsTrue(await Client.SelectApplication(Applications.DF1));

				byte[]? DownloadedDg2 = await Client.DownloadFile(EF.DG2, "EF.DG2");

				Assert.IsNotNull(DownloadedDg2);
				Assert.IsTrue(DownloadedDg2.Length > ushort.MaxValue);
				Assert.IsTrue(Simulator.SawExtendedReadBinary);
				CollectionAssert.AreEqual(Files[EF.DG2], DownloadedDg2);
			}
		}

		private static async Task AssertClientDownloadsFile(TravelDocumentsClient Client, JmrtdFixture Fixture,
			ushort FileId, string FileName)
		{
			byte[]? Downloaded = await Client.DownloadFile(FileId, FileName);

			Assert.IsNotNull(Downloaded, "Unable to download " + FileName + ".");
			CollectionAssert.AreEqual(Fixture.Files[FileId], Downloaded);
		}

		private static async Task AssertClientDownloadsBytes(TravelDocumentsClient Client,
			Dictionary<ushort, byte[]> Files, ushort FileId, string FileName)
		{
			byte[]? Downloaded = await Client.DownloadFile(FileId, FileName);

			Assert.IsNotNull(Downloaded, "Unable to download " + FileName + ".");
			CollectionAssert.AreEqual(Files[FileId], Downloaded);
		}

		private static TravelDocumentsClient CreatePaceClient(JmrtdFixture Fixture,
			out EmrtdProtocolSimulator Simulator, out Dictionary<ushort, byte[]> Files)
		{
			Waher.Runtime.Inventory.Types.Initialize(typeof(TravelDocumentsClient).Assembly);

			DocumentInformation Information = new DocumentInformation()
			{
				MRZ_Information = "T22000129364081251010318"
			};

			Files = new Dictionary<ushort, byte[]>(Fixture.Files)
			{
				[EF.CardAccess] = HexToBytes("31143012060A04007F0007020204020202010202010D")
			};

			EmrtdProtocolSimulatorOptions Options = new EmrtdProtocolSimulatorOptions(Files)
			{
				DocumentInformation = Information,
				RequireSecureMessagingForDataGroups = true,
				EnablePaceEntryPoints = true
			};
			Simulator = new EmrtdProtocolSimulator(Options);
			EmrtdProtocolSimulatorIsoDepInterface IsoDep = new EmrtdProtocolSimulatorIsoDepInterface(Simulator);

			return new TravelDocumentsClient(IsoDep, Information, null);
		}

		private static async Task AssertCardAccessCanBeReadBeforePace(JmrtdFixture Fixture)
		{
			TravelDocumentsClient Client = CreatePaceClient(Fixture, out _, out Dictionary<ushort, byte[]> Files);
			using (Client)
			{
				Assert.IsTrue(ASN1.TryDecodeDer(Files[EF.CardAccess], out object? CardAccess));
				Assert.IsInstanceOfType<Vector>(CardAccess);
				Assert.IsTrue(((Vector)CardAccess!).Cast<object>().Any(Item => Item is IPaceProtocol));

				byte[]? DownloadedCardAccess = await Client.DownloadFile(EF.CardAccess, "EF.CardAccess");
				CollectionAssert.AreEqual(Files[EF.CardAccess], DownloadedCardAccess);
			}
		}

		private static DocumentInformation CreateDocumentInformation()
		{
			return new DocumentInformation()
			{
				MRZ_Information = "NA000000<790010110350101"
			};
		}

		private static byte[] HexToBytes(string Hex)
		{
			byte[] Result = new byte[Hex.Length / 2];
			for (int i = 0; i < Result.Length; i++)
				Result[i] = Byte.Parse(Hex.Substring(i * 2, 2), NumberStyles.HexNumber, CultureInfo.InvariantCulture);

			return Result;
		}
	}
}
