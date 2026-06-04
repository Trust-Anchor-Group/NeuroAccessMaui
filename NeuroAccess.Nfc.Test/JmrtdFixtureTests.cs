using System.Security.Cryptography;
using System.Text.Json;
using NeuroAccess.Nfc.Test.IcaoPart3;
using NeuroAccess.Nfc.TravelDocuments;
using NeuroAccess.Nfc.TravelDocuments.DataObjects;
using Waher.Networking;
using ISO19794ImageDataType = NeuroAccess.Nfc.TravelDocuments.ISO19794.ImageDataType;
using ISO19794RepresentationFace = NeuroAccess.Nfc.TravelDocuments.ISO19794.RepresentationFace;

namespace NeuroAccess.Nfc.Test
{
	/// <summary>
	/// Tests synthetic LDS fixtures generated with JMRTD.
	/// </summary>
	[TestClass]
	public class JmrtdFixtureTests
	{
		/// <summary>
		/// Verifies the minimal JMRTD ISO/IEC 19794 JPEG fixture parses directly.
		/// </summary>
		[TestMethod]
		public void Test_01_Minimal_Iso19794_Jpeg_Fixture_Parses()
		{
			AssertFixtureParses("minimal-iso19794-jpeg", ISO19794ImageDataType.Jpeg, true, false);
		}

		/// <summary>
		/// Verifies the JMRTD DG11 fixture parses the additional name fields directly.
		/// </summary>
		[TestMethod]
		public void Test_02_Dg11_Fixture_Parses()
		{
			AssertFixtureParses("dg11-iso19794-jpeg", ISO19794ImageDataType.Jpeg, true, true);
		}

		/// <summary>
		/// Verifies the JMRTD ISO/IEC 19794 JPEG 2000 fixture parses directly.
		/// </summary>
		[TestMethod]
		public void Test_03_Iso19794_Jpeg2000_Fixture_Parses()
		{
			AssertFixtureParses("minimal-iso19794-jpeg2000", ISO19794ImageDataType.Jpeg2000, true, false);
		}

		/// <summary>
		/// Verifies the JMRTD no-DG2 fixture still parses DG1 and DG11.
		/// </summary>
		[TestMethod]
		public void Test_04_No_Dg2_Fixture_Parses_Dg1_And_Dg11()
		{
			AssertFixtureParses("no-dg2-dg11", null, false, true);
		}

		/// <summary>
		/// Verifies the JMRTD ISO/IEC 39794 DG2 fixture parses through the normal DG2 path.
		/// </summary>
		[TestMethod]
		public void Test_05_Iso39794_Jpeg_Fixture_Parses()
		{
			AssertFixtureParses("minimal-iso39794-jpeg", ISO19794ImageDataType.Jpeg, true, false);
		}

		/// <summary>
		/// Verifies fixture files can be downloaded through a stateful ISO-DEP simulator.
		/// </summary>
		/// <returns>A task representing the asynchronous test.</returns>
		[TestMethod]
		public async Task Test_06_Stateful_IsoDep_Simulator_Downloads_Fixture_Files()
		{
			JmrtdFixture Fixture = JmrtdFixtureLoader.Load("dg11-iso19794-jpeg");
			VirtualEmrtdCard IsoDep = new(Fixture.Files);

			using TravelDocumentsClient Client = new(IsoDep, CreateDocumentInformation(), null);
			Assert.IsTrue(await Client.SelectApplication(Applications.DF1));

			byte[]? DownloadedDg1 = await Client.DownloadFile(EF.DG1, "EF.DG1");
			byte[]? DownloadedDg2 = await Client.DownloadFile(EF.DG2, "EF.DG2");
			byte[]? DownloadedDg11 = await Client.DownloadFile(EF.DG11, "EF.DG11");

			CollectionAssert.AreEqual(Fixture.Files[EF.DG1], DownloadedDg1);
			CollectionAssert.AreEqual(Fixture.Files[EF.DG2], DownloadedDg2);
			CollectionAssert.AreEqual(Fixture.Files[EF.DG11], DownloadedDg11);
		}

		/// <summary>
		/// Verifies a large JMRTD fixture uses extended READ BINARY offsets.
		/// </summary>
		/// <returns>A task representing the asynchronous test.</returns>
		[TestMethod]
		public async Task Test_07_Large_Fixture_Uses_Extended_Read_Binary_Offsets()
		{
			JmrtdFixture Fixture = JmrtdFixtureLoader.Load("large-iso19794-jpeg");
			VirtualEmrtdCard IsoDep = new(Fixture.Files);

			using TravelDocumentsClient Client = new(IsoDep, CreateDocumentInformation(), null);
			Assert.IsTrue(await Client.SelectApplication(Applications.DF1));
			byte[]? DownloadedDg2 = await Client.DownloadFile(EF.DG2, "EF.DG2");

			Assert.IsNotNull(DownloadedDg2);
			Assert.IsTrue(DownloadedDg2.Length > ushort.MaxValue);
			Assert.IsTrue(IsoDep.SawExtendedReadBinary);
			CollectionAssert.AreEqual(Fixture.Files[EF.DG2], DownloadedDg2);
		}

		/// <summary>
		/// Verifies fixture manifests match the committed fixture bytes.
		/// </summary>
		[TestMethod]
		public void Test_08_Manifests_Match_Fixture_Hashes()
		{
			foreach (string FixtureName in Directory.GetDirectories(JmrtdFixtureLoader.GetJmrtdTestDataPath()))
			{
				string ManifestPath = Path.Combine(FixtureName, "manifest.json");
				Assert.IsTrue(File.Exists(ManifestPath), "Missing manifest for " + FixtureName);

				using JsonDocument Manifest = JsonDocument.Parse(File.ReadAllText(ManifestPath));
				JsonElement FilesElement = Manifest.RootElement.GetProperty("files");

				foreach (JsonProperty FileProperty in FilesElement.EnumerateObject())
				{
					string FilePath = Path.Combine(FixtureName, FileProperty.Name);
					Assert.IsTrue(File.Exists(FilePath), "Missing fixture file " + FilePath);

					byte[] Data = File.ReadAllBytes(FilePath);
					string ExpectedHash = FileProperty.Value.GetProperty("sha256").GetString() ?? string.Empty;
					int ExpectedLength = FileProperty.Value.GetProperty("length").GetInt32();

					Assert.AreEqual(ExpectedLength, Data.Length);
					Assert.AreEqual(ExpectedHash, Convert.ToHexString(SHA256.HashData(Data)).ToLowerInvariant());
				}
			}
		}

		/// <summary>
		/// Verifies synthetic EF.SOD signature variants parse and verify.
		/// </summary>
		[TestMethod]
		[DataRow("minimal-ecdsa-sha512-explicit-iso19794-jpeg", 64, 1, 8)]
		[DataRow("minimal-brainpoolp512r1-ecdsa-sha512-explicit-iso19794-jpeg", 64, 1, 8)]
		[DataRow("minimal-rsapss-sha256-iso19794-jpeg", 32, 1, 8)]
		[DataRow("minimal-ecdsa-sha384-named-iso19794-jpeg", 48, 1, 8)]
		[DataRow("minimal-lds110-rsa-sha256-iso19794-jpeg", 32, 1, 10)]
		public void Test_09_Synthetic_Sod_Fixtures_Verify(string FixtureName, int ExpectedDigestLength,
			int ExpectedLdsMajorVersion, int ExpectedLdsMinorVersion)
		{
			AssertFixtureParses(FixtureName, ISO19794ImageDataType.Jpeg, true, false);

			JmrtdFixture Fixture = JmrtdFixtureLoader.Load(FixtureName);
			using TravelDocumentsClient Client = new(new EmptyIsoDepInterface(), CreateDocumentInformation(), null);

			Assert.IsTrue(TravelDocumentsClient.TryParseDataObject(Fixture.Files[EF.COM], Client, out ApplicationLevelInformation? AppInfo));
			Assert.AreEqual(ExpectedLdsMajorVersion, AppInfo.LdsMajorVersion);
			Assert.AreEqual(ExpectedLdsMinorVersion, AppInfo.LdsMinorVersion);
			SetAppInfo(Client, AppInfo);

			Assert.IsTrue(TravelDocumentsClient.TryParseDataObject(Fixture.Files[EF.SOD], Client, out DocumentSecurityObject? SecurityInfo));
			Assert.IsTrue(SecurityInfo.SignatureVerified);
			Assert.AreEqual(1, SecurityInfo.SignerCount);
			Assert.AreEqual(1, SecurityInfo.CertificateCount);
			Assert.IsNotNull(SecurityInfo.CmsSignedData);
			Assert.AreEqual("2.23.136.1.1.1", SecurityInfo.CmsSignedData.ContentType);
			Assert.AreEqual(ExpectedDigestLength, SecurityInfo.CmsSignedData.SignerInfos[0].MessageDigest?.Length);
		}

		/// <summary>
		/// Verifies CMS EF.SOD mutations are rejected.
		/// </summary>
		[TestMethod]
		public void Test_10_Cms_Sod_Mutations_Are_Rejected()
		{
			JmrtdFixture Fixture = JmrtdFixtureLoader.Load("minimal-ecdsa-sha512-explicit-iso19794-jpeg");
			byte[] Sod = Fixture.Files[EF.SOD];
			using TravelDocumentsClient Client = new(new EmptyIsoDepInterface(), CreateDocumentInformation(), null);

			Assert.IsTrue(TravelDocumentsClient.TryParseDataObject(Fixture.Files[EF.COM], Client, out ApplicationLevelInformation? AppInfo));
			SetAppInfo(Client, AppInfo);

			Assert.IsTrue(TravelDocumentsClient.TryParseDataObject(Sod, Client, out DocumentSecurityObject? SecurityInfo));
			byte[] MessageDigest = SecurityInfo.CmsSignedData!.SignerInfos[0].MessageDigest!;

			AssertSodRejected(Client, MutateFirstPatternByte(Sod, MessageDigest, 0), "messageDigest mutation");
			AssertSodRejected(Client, MutatePatternByte(Sod, [0x06, 0x06, 0x67, 0x81, 0x08, 0x01, 0x01, 0x01], 1, 7), "contentType mutation");
			AssertSodRejected(Client, MutateLastByte(Sod), "signature mutation");
			AssertSodRejected(Client, RetagCertificateSetAsCrlSet(Sod), "missing certificate mutation");
			AssertSodRejected(Client, MutateAllPatternBytes(Sod, [0x06, 0x09, 0x60, 0x86, 0x48, 0x01, 0x65, 0x03, 0x04, 0x02, 0x03], 10), "unsupported digest OID mutation");
		}

		private static void AssertFixtureParses(string FixtureName, ISO19794ImageDataType? ExpectedImageDataType,
			bool ExpectDg2, bool ExpectDg11)
		{
			JmrtdFixture Fixture = JmrtdFixtureLoader.Load(FixtureName);
			using TravelDocumentsClient Client = new(new EmptyIsoDepInterface(), CreateDocumentInformation(), null);

			Assert.IsTrue(TravelDocumentsClient.TryParseDataObject(Fixture.Files[EF.COM], Client, out ApplicationLevelInformation? AppInfo));
			Assert.IsNotNull(AppInfo.TagList);

			Assert.IsTrue(TravelDocumentsClient.TryParseDataObject(Fixture.Files[EF.DG1], Client, out MachineReadableZoneInformation? Dg1));
			Assert.IsNotNull(Dg1.Mrz);
			Assert.IsNotNull(Dg1.Mrz.DocumentInformation);
			Assert.IsNotNull(Dg1.Mrz.DocumentInformation.PrimaryIdentifier);
			Assert.IsNotNull(Dg1.Mrz.DocumentInformation.SecondaryIdentifier);
			CollectionAssert.Contains(Dg1.Mrz.DocumentInformation.PrimaryIdentifier, "SPECIMEN");
			CollectionAssert.Contains(Dg1.Mrz.DocumentInformation.SecondaryIdentifier, "ALICE");

			Assert.AreEqual(ExpectDg2, Fixture.Files.ContainsKey(EF.DG2));
			if (ExpectDg2)
				AssertDg2Parses(Fixture.Files[EF.DG2], Client, ExpectedImageDataType!.Value);

			Assert.AreEqual(ExpectDg11, Fixture.Files.ContainsKey(EF.DG11));
			if (ExpectDg11)
			{
				Assert.IsTrue(TravelDocumentsClient.TryParseDataObject(Fixture.Files[EF.DG11], Client, out AdditionalPersonalDetails? Dg11));
				Assert.AreEqual("SPECIMEN<<ALICE", Dg11.FullName);
				Assert.AreEqual("19900101", Dg11.DateOfBirth);
			}
		}

		private static void AssertDg2Parses(byte[] Dg2Data, TravelDocumentsClient Client,
			ISO19794ImageDataType ExpectedImageDataType)
		{
			Assert.IsTrue(TravelDocumentsClient.TryParseDataObject(Dg2Data, Client, out BiometricEncodingFace? Dg2));
			Assert.IsNotNull(Dg2.Templates);
			Assert.IsNotNull(Dg2.Templates.Templates);
			Assert.HasCount(1, Dg2.Templates.Templates);

			BiometricInformationTemplate Template = Dg2.Templates.Templates[0];
			Assert.IsNotNull(Template.BiometricDataBlock);
			Assert.IsNotNull(Template.BiometricDataBlock.Record);
			Assert.HasCount(1, Template.BiometricDataBlock.Record.Representations);

			ISO19794RepresentationFace Face = (ISO19794RepresentationFace)Template.BiometricDataBlock.Record.Representations[0];
			Assert.AreEqual(ExpectedImageDataType, Face.ImageDataType);
			Assert.IsTrue(Face.Width > 0);
			Assert.IsTrue(Face.Height > 0);
			Assert.IsTrue(Face.ImageData.Length > 0);
		}

		private static void AssertSodRejected(TravelDocumentsClient Client, byte[] Sod, string CaseName)
		{
			Assert.IsFalse(TravelDocumentsClient.TryParseDataObject(Sod, Client,
				out DocumentSecurityObject? _), CaseName);
		}

		private static byte[] MutateLastByte(byte[] Data)
		{
			byte[] Mutated = (byte[])Data.Clone();
			Mutated[^1] ^= 0x01;
			return Mutated;
		}

		private static byte[] MutateFirstPatternByte(byte[] Data, byte[] Pattern, int PatternIndex)
		{
			return MutatePatternByte(Data, Pattern, 0, PatternIndex);
		}

		private static byte[] MutatePatternByte(byte[] Data, byte[] Pattern, int OccurrenceIndex,
			int PatternIndex)
		{
			byte[] Mutated = (byte[])Data.Clone();
			int Offset = FindPattern(Mutated, Pattern, OccurrenceIndex);

			Assert.IsTrue(Offset >= 0, "Pattern not found.");
			Mutated[Offset + PatternIndex] ^= 0x01;
			return Mutated;
		}

		private static byte[] MutateAllPatternBytes(byte[] Data, byte[] Pattern, int PatternIndex)
		{
			byte[] Mutated = (byte[])Data.Clone();
			int Offset = 0;
			bool Found = false;

			while ((Offset = FindPattern(Mutated, Pattern, 0, Offset)) >= 0)
			{
				Found = true;
				Mutated[Offset + PatternIndex] ^= 0x7c;
				Offset += Pattern.Length;
			}

			Assert.IsTrue(Found, "Pattern not found.");
			return Mutated;
		}

		private static byte[] RetagCertificateSetAsCrlSet(byte[] Data)
		{
			byte[] Mutated = (byte[])Data.Clone();
			int Offset = 0;

			ReadTlv(Mutated, ref Offset, out _, out int Tag, out _, out int ContentOffset,
				out _, out _);
			Assert.AreEqual(0x77, Tag);

			Offset = ContentOffset;
			ReadTlv(Mutated, ref Offset, out _, out Tag, out _, out int ContentInfoOffset,
				out _, out _);
			Assert.AreEqual(0x30, Tag);

			Offset = ContentInfoOffset;
			SkipTlv(Mutated, ref Offset);       // signedData OID
			ReadTlv(Mutated, ref Offset, out _, out Tag, out _, out int ExplicitOffset,
				out _, out _);
			Assert.AreEqual(0xa0, Tag);

			Offset = ExplicitOffset;
			ReadTlv(Mutated, ref Offset, out _, out Tag, out _, out int SignedDataOffset,
				out _, out _);
			Assert.AreEqual(0x30, Tag);

			Offset = SignedDataOffset;
			SkipTlv(Mutated, ref Offset);       // version
			SkipTlv(Mutated, ref Offset);       // digestAlgorithms
			SkipTlv(Mutated, ref Offset);       // encapContentInfo

			Assert.AreEqual(0xa0, Mutated[Offset]);
			Mutated[Offset] = 0xa1;
			return Mutated;
		}

		private static int FindPattern(byte[] Data, byte[] Pattern, int OccurrenceIndex)
		{
			return FindPattern(Data, Pattern, OccurrenceIndex, 0);
		}

		private static int FindPattern(byte[] Data, byte[] Pattern, int OccurrenceIndex, int StartOffset)
		{
			int Found = 0;
			for (int i = StartOffset; i <= Data.Length - Pattern.Length; i++)
			{
				bool Matches = true;
				for (int j = 0; j < Pattern.Length; j++)
				{
					if (Data[i + j] != Pattern[j])
					{
						Matches = false;
						break;
					}
				}

				if (Matches)
				{
					if (Found++ == OccurrenceIndex)
						return i;
				}
			}

			return -1;
		}

		private static void SkipTlv(byte[] Data, ref int Offset)
		{
			ReadTlv(Data, ref Offset, out _, out _, out _, out _, out _, out int EndOffset);
			Offset = EndOffset;
		}

		private static void ReadTlv(byte[] Data, ref int Offset, out int TagOffset, out int Tag,
			out int HeaderLength, out int ContentOffset, out int ContentLength, out int EndOffset)
		{
			TagOffset = Offset;
			Tag = Data[Offset++];

			if ((Tag & 0x1f) == 0x1f)
			{
				while (Offset < Data.Length)
				{
					int TagByte = Data[Offset++];
					Tag = (Tag << 8) | TagByte;

					if ((TagByte & 0x80) == 0)
						break;
				}
			}

			int LengthByte = Data[Offset++];
			if ((LengthByte & 0x80) == 0)
				ContentLength = LengthByte;
			else
			{
				int LengthBytes = LengthByte & 0x7f;
				ContentLength = 0;

				for (int i = 0; i < LengthBytes; i++)
					ContentLength = (ContentLength << 8) | Data[Offset++];
			}

			ContentOffset = Offset;
			HeaderLength = ContentOffset - TagOffset;
			EndOffset = ContentOffset + ContentLength;
		}

		private static void SetAppInfo(TravelDocumentsClient Client, ApplicationLevelInformation AppInfo)
		{
			System.Reflection.FieldInfo? Field = typeof(TravelDocumentsClient).GetField("appInfo",
				System.Reflection.BindingFlags.Instance | System.Reflection.BindingFlags.NonPublic);

			Assert.IsNotNull(Field);
			Field.SetValue(Client, AppInfo);
		}

		private static DocumentInformation CreateDocumentInformation()
		{
			return new DocumentInformation()
			{
				MRZ_Information = "NA000000<790010110350101"
			};
		}

		private sealed class EmptyIsoDepInterface : IIsoDepInterface
		{
			public INfcTag? Tag => null;

			public Task OpenIfClosed()
			{
				return Task.CompletedTask;
			}

			public void CloseIfOpen()
			{
			}

			public Task<byte[]> GetHighLayerResponse()
			{
				return Task.FromResult<byte[]>([]);
			}

			public Task<byte[]> GetHistoricalBytes()
			{
				return Task.FromResult<byte[]>([]);
			}

			public void SetTimeout(int Timeout)
			{
			}

			public Task<byte[]> ExecuteCommand(byte[] Command, ICommunicationLayer CommunicationLayer)
			{
				throw new NotSupportedException();
			}

			public void Dispose()
			{
			}
		}
	}
}
