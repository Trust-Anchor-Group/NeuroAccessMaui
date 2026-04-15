using System.Globalization;
using System.Security.Cryptography.Pkcs;
using System.Security.Cryptography.X509Certificates;
using System.Text;
using Microsoft.VisualStudio.TestPlatform.CoreUtilities.Extensions;
using NeuroAccess.Nfc.TravelDocuments;
using NeuroAccess.Nfc.TravelDocuments.Certificates;
using NeuroAccess.Nfc.TravelDocuments.RevocationLists;
using NeuroAccess.Nfc.TravelDocuments.Security;
using Waher.Content;
using Waher.Networking;
using Waher.Networking.Sniffers;
using Waher.Runtime.Collections;
using Waher.Security;

namespace NeuroAccess.Nfc.Test
{
	[TestClass]
	public class SodTests
	{
		private const string idDomain = "id.tagroot.io";

		/// <summary>
		/// Test context
		/// </summary>
		public TestContext TestContext { get; set; } = null!;

		[TestCleanup]
		public void TestCleanup()
		{
			ExportAsn1Statistics(Console.Out);
		}

		// Testing parsing of Document Security Objects. (§4.6.2, ICAO 9303-10)

		[TestMethod]
		[DataRow("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")]
		public void Test_01_Parse_LDS_v1_8(string Base64)
		{
			byte[] Bin = Convert.FromBase64String(Base64);

			SignedCms SignedData = new();
			SignedData.Decode(Bin);

			ASN1.TryDecodeDer(SignedData.ContentInfo.Content, out object? Content);
			Vector? ContentVector = Content as Vector;
			Assert.IsNotNull(ContentVector);

			Assert.IsTrue(ASN1.TryInstantiate(SignedData.ContentInfo.ContentType.Value!, out ISecurityObject? SecurityObject));
			Assert.IsFalse(SecurityObject.IsConfigured);

			if (!SecurityObject.Configure(ContentVector))
			{
				ASN1.ReportOidNotConfigured(SecurityObject.Oid);
				Assert.Fail("Unable to configure object.");
			}

			Console.Out.WriteLine(JSON.Encode(SecurityObject, true));
			Console.Out.WriteLine();
			Console.Out.WriteLine();
			Console.Out.WriteLine();
			Console.Out.WriteLine(JSON.Encode(SignedData, true));

			SignedData.CheckSignature(true);

			foreach (X509Certificate2 Cert in SignedData.Certificates)
			{
				Assert.IsTrue(Certificate.TryParse(Cert.RawData, out Certificate? Cert2));
				KeyValuePair<string?, byte[]?> P = TravelDocumentsClient.GetAuthorityKeyIdentifier(Cert2);
				string? CountryCode = P.Key;
				byte[]? IssuerKeyReference = P.Value;

				Assert.IsNotNull(CountryCode);
				Assert.IsNotNull(IssuerKeyReference);

				Console.Out.WriteLine(Hashes.BinaryToString(IssuerKeyReference));
			}
		}

		[TestMethod]
		[DataRow("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")]
		public async Task Test_02_VerifyCertificate(string Base64)
		{
			TestContextWriter SnifferWriter = new(this.TestContext!);
			TextWriterSniffer Sniffer = new(SnifferWriter, BinaryPresentationMethod.Hexadecimal, "Unit Test Sniffer");
			CommunicationLayer Client = new(true, Sniffer);
			byte[] Bin = Convert.FromBase64String(Base64);

			SignedCms SignedData = new();
			SignedData.Decode(Bin);
			SignedData.CheckSignature(true);

			foreach (X509Certificate2 Cert in SignedData.Certificates)
			{
				Assert.IsTrue(Certificate.TryParse(Cert.RawData, out Certificate? Cert2));

				Console.Out.WriteLine("Certificate in Signed CMS");
				Console.Out.WriteLine(new string('=', 80));
				Console.Out.WriteLine(JSON.Encode(Cert2.Asn1Vector, true));

				ChunkedList<Certificate> Certificates = [Cert2];
				Dictionary<string, bool> CrlUrls = [];

				foreach (string CrlUrl in TravelDocumentsClient.GetRevocationListUrls(Cert2))
					CrlUrls[CrlUrl] = true;

				KeyValuePair<string?, byte[]?> P = TravelDocumentsClient.GetAuthorityKeyIdentifier(Cert2);
				Dictionary<string, bool> Processed = [];
				string? CountryCode = P.Key;
				byte[]? IssuerKeyReference = P.Value;

				// Authority Key Identifier (AKI) extension required by ICAO.
				Assert.IsNotNull(CountryCode);
				if (string.IsNullOrEmpty(CountryCode) || IssuerKeyReference is null)
					Assert.Fail("No Authority Key Identifier found in certificate.");

				while (!string.IsNullOrEmpty(CountryCode) && IssuerKeyReference is not null)
				{
					Console.Out.WriteLine();
					Console.Out.WriteLine("Issuer Key Reference: " + CountryCode + ", " + Hashes.BinaryToString(IssuerKeyReference));

					string Key = Convert.ToBase64String(IssuerKeyReference);
					if (Processed.ContainsKey(Key))
					{
						Console.Out.WriteLine("Already processed...");
						break;
					}

					Processed[Key] = true;

					Certificate? IssuerCertificate = await CertificateStore.TryLoadCertificate(
						idDomain, CountryCode, IssuerKeyReference, Client);
					Assert.IsNotNull(IssuerCertificate, "Issuer certificate not found.");

					Certificates.Insert(0, IssuerCertificate);

					Console.Out.WriteLine(new string('=', 80));
					Console.Out.WriteLine(JSON.Encode(IssuerCertificate.Asn1Vector, true));

					// Make sure to use Certificate Revocation Lists (CRLs) from ICAO approved certificates.

					foreach (string CrlUrl in TravelDocumentsClient.GetRevocationListUrls(IssuerCertificate))
						CrlUrls[CrlUrl] = true;

					P = TravelDocumentsClient.GetAuthorityKeyIdentifier(IssuerCertificate);
					CountryCode = P.Key;
					IssuerKeyReference = P.Value;
				}

				Assert.IsGreaterThan(0, CrlUrls.Count);

				foreach (string CrlUrl in CrlUrls.Keys)
				{
					CertificateList? RevokedCertificates = await CertificateStore.TryLoadCrl(CrlUrl, Client);
					Assert.IsNotNull(RevokedCertificates);

					Console.Out.WriteLine();
					Console.Out.WriteLine("Checking Revocation List: " + CrlUrl);
					Console.Out.WriteLine(new string('=', 80));
					Console.Out.WriteLine(JSON.Encode(RevokedCertificates.Asn1Vector, true));

					Assert.IsTrue(await RevokedCertificates.VerifySignature(idDomain, CountryCode!, Client));

					if (RevokedCertificates.HasBeenRevoked(Cert2, out RevokedReason Reason))
					{
						Assert.Fail("Certificate " + Cert2.SerialNumber.ToString("X", CultureInfo.InvariantCulture) +
							" has been revoked: " + Reason.ToString());
					}

					foreach (Certificate Certificate2 in Certificates)
					{
						if (RevokedCertificates.HasBeenRevoked(Certificate2, out Reason))
						{
							Assert.Fail("Certificate " + Certificate2.SerialNumber.ToString("X", CultureInfo.InvariantCulture) +
								" has been revoked: " + Reason.ToString());
						}
					}
				}

				X509Chain Chain = X509Chain.Create();
				bool First = true;

				Chain.ChainPolicy.RevocationMode = X509RevocationMode.NoCheck;       // Custom Revocation List check performed earlier
				Chain.ChainPolicy.RevocationFlag = X509RevocationFlag.EntireChain;
				Chain.ChainPolicy.VerificationFlags = X509VerificationFlags.NoFlag;
				Chain.ChainPolicy.TrustMode = X509ChainTrustMode.CustomRootTrust;
				Chain.ChainPolicy.UrlRetrievalTimeout = TimeSpan.FromSeconds(30);

				foreach (Certificate Certificate2 in Certificates)
				{
					X509Certificate2 Certificate2_2 = X509CertificateLoader.LoadCertificate(Certificate2.Binary);

					if (First)
					{
						Chain.ChainPolicy.CustomTrustStore.Add(Certificate2_2);
						First = false;
					}
					else
						Chain.ChainPolicy.ExtraStore.Add(Certificate2_2);
				}

				if (!Chain.Build(Cert))
				{
					StringBuilder sb = new();

					sb.AppendLine("Validation failed: ");

					foreach (X509ChainStatus Status in Chain.ChainStatus)
						sb.AppendLine(Status.StatusInformation);

					Assert.Fail(sb.ToString());
				}
			}

			await Sniffer.FlushAsync();
			await Task.Delay(500, CancellationToken.None);
		}

		[TestMethod]
		[DataRow(
			"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")]
		public void Test_03_VerifyChain(string RootBase64, string CertBase64)
		{
			TestContextWriter SnifferWriter = new(this.TestContext!);
			TextWriterSniffer Sniffer = new(SnifferWriter, BinaryPresentationMethod.Hexadecimal, "Unit Test Sniffer");
			CommunicationLayer Client = new(true, Sniffer);

			Assert.IsTrue(Certificate.TryParse(Convert.FromBase64String(RootBase64), out Certificate? Root));
			Assert.IsTrue(Certificate.TryParse(Convert.FromBase64String(CertBase64), out Certificate? Cert));

			Assert.IsTrue(CertificateChain.VerifySignatures(Client, Root, Cert));

			// TODO: Check rules in certificate extensions
		}

		[TestMethod]
		public async Task Test_04_VerifyAllIcaoCertificates()
		{
			ASN1.GetOidsNotRecognized(true);    // Clears statistics.

			string[] FileNames = Directory.GetFiles("..\\..\\..\\..\\..\\IcaoPkiCertificates\\Root\\IcaoPki", "*.cer", SearchOption.AllDirectories);
			Assert.IsGreaterThan(0, FileNames.Length, "ICAO Certificates project not available in sibling folder.");

			ChunkedList<string> FailedCertificates = [];
			SortedDictionary<string, int> NrOkPerCountry = [];
			SortedDictionary<string, int> NrFailedPerCountry = [];
			SortedDictionary<string, int> NrCertificates = [];
			SortedDictionary<string, int> Exceptions = [];
			int NrOk = 0;
			int NrFailed = 0;

			foreach (string FileName in FileNames)
			{
				string CountryCode = Path.GetFileName(Path.GetDirectoryName(FileName))!;

				ASN1.Inc(CountryCode, NrCertificates);

				try
				{
					byte[] Bin = File.ReadAllBytes(FileName);

					if (!Certificate.TryParse(Bin, out Certificate? Cert))
					{
						NrFailed++;
						FailedCertificates.Add(FileName);
						ASN1.Inc(CountryCode, NrFailedPerCountry);
						continue;
					}

					Certificate[] Chain = await CertificateChain.GetChain(Cert, idDomain);

					if (!CertificateChain.VerifySignatures(Chain))
					{
						NrFailed++;
						FailedCertificates.Add(FileName);
						ASN1.Inc(CountryCode, NrFailedPerCountry);
						continue;
					}

					NrOk++;
					ASN1.Inc(CountryCode, NrOkPerCountry);
				}
				catch (Exception ex)
				{
					StringBuilder sb = new();

					sb.AppendLine(ex.Message);
					sb.AppendLine();
					sb.Append(ex.StackTrace ?? "Stack trace not available");

					ASN1.Inc(sb.ToString(), Exceptions);
					NrFailed++;
					FailedCertificates.Add(FileName);
					ASN1.Inc(CountryCode, NrFailedPerCountry);
				}
			}

			ExportStatistics(Console.Out, NrOk, NrFailed, NrCertificates, NrOkPerCountry,
				NrFailedPerCountry, Exceptions, FailedCertificates);
		}

		private static void ExportStatistics(TextWriter Output, int NrOk, int NrFailed,
			SortedDictionary<string, int> NrCertificates,
			SortedDictionary<string, int> NrOkPerCountry,
			SortedDictionary<string, int> NrFailedPerCountry,
			SortedDictionary<string, int> Exceptions,
			ChunkedList<string> FailedCertificates)
		{
			Output.WriteLine("Nr OK: " + NrOk.ToString(CultureInfo.InvariantCulture));
			Output.WriteLine("Nr Failed: " + NrFailed.ToString(CultureInfo.InvariantCulture));

			Output.WriteLine();
			Output.WriteLine("Statistics per country:");
			Output.WriteLine();
			Output.WriteLine("| Country | Certificates |        Nr OK |    Nr Failed |");
			Output.WriteLine("|:--------|-------------:|-------------:|-------------:|");

			SortedDictionary<string, int> CountriesWithErrors = [];
			SortedDictionary<string, int> CountriesWithNoErrors = [];
			string s;

			foreach (KeyValuePair<string, int> P in NrCertificates)
			{
				Output.Write("| ");
				Output.Write(P.Key);
				Output.Write(new string(' ', 8 - P.Key.Length));
				Output.Write('|');

				s = P.Value.ToString(CultureInfo.InvariantCulture);
				Output.Write(new string(' ', 13 - s.Length));
				Output.Write(s);

				if (NrOkPerCountry.TryGetValue(P.Key, out int i))
					s = i.ToString(CultureInfo.InvariantCulture);
				else
					s = string.Empty;

				Output.Write(" |");
				Output.Write(new string(' ', 13 - s.Length));
				Output.Write(s);

				if (NrFailedPerCountry.TryGetValue(P.Key, out i))
				{
					s = i.ToString(CultureInfo.InvariantCulture);
					CountriesWithErrors[P.Key] = P.Value;
				}
				else
				{
					s = string.Empty;
					CountriesWithNoErrors[P.Key] = P.Value;
				}

				Output.Write(" |");
				Output.Write(new string(' ', 13 - s.Length));
				Output.Write(s);
				Output.WriteLine(" |");
			}

			Output.WriteLine();
			Output.WriteLine("Countries with errors:");
			Output.WriteLine();
			Output.WriteLine("| Country | Certificates |        Nr OK |    Nr Failed |");
			Output.WriteLine("|:--------|-------------:|-------------:|-------------:|");

			foreach (KeyValuePair<string, int> P in CountriesWithErrors)
			{
				Output.Write("| ");
				Output.Write(P.Key);
				Output.Write(new string(' ', 8 - P.Key.Length));
				Output.Write('|');

				s = P.Value.ToString(CultureInfo.InvariantCulture);
				Output.Write(new string(' ', 13 - s.Length));
				Output.Write(s);

				if (NrOkPerCountry.TryGetValue(P.Key, out int i))
					s = i.ToString(CultureInfo.InvariantCulture);
				else
					s = string.Empty;

				Output.Write(" |");
				Output.Write(new string(' ', 13 - s.Length));
				Output.Write(s);

				if (NrFailedPerCountry.TryGetValue(P.Key, out i))
					s = i.ToString(CultureInfo.InvariantCulture);
				else
					s = string.Empty;

				Output.Write(" |");
				Output.Write(new string(' ', 13 - s.Length));
				Output.Write(s);
				Output.WriteLine(" |");
			}

			Output.WriteLine();
			Output.WriteLine("Countries with no errors:");
			Output.WriteLine();
			Output.WriteLine("| Country | Certificates |");
			Output.WriteLine("|:--------|-------------:|");

			foreach (KeyValuePair<string, int> P in CountriesWithNoErrors)
			{
				Output.Write("| ");
				Output.Write(P.Key);
				Output.Write(new string(' ', 8 - P.Key.Length));
				Output.Write('|');

				s = P.Value.ToString(CultureInfo.InvariantCulture);
				Output.Write(new string(' ', 13 - s.Length));
				Output.Write(s);
				Output.WriteLine(" |");
			}

			if (Exceptions.Count > 0)
			{
				Output.WriteLine();
				Output.WriteLine("Exceptions encountered:");
				Output.WriteLine();

				foreach (KeyValuePair<string, int> P in Exceptions)
				{
					Output.WriteLine();
					Output.WriteLine("Times: " + P.Value.ToString(CultureInfo.InvariantCulture));
					Output.WriteLine(P.Key);
				}
			}
			else
			{
				Output.WriteLine();
				Output.WriteLine("No exceptions encountered:");
				Output.WriteLine();
			}

			if (NrFailed > 0)
			{
				Output.WriteLine();

				foreach (string FileName in FailedCertificates)
					Output.WriteLine(FileName);

				Assert.Fail("Some ICAO certificates failed to verify. See output for details.");
			}
		}

		private static void ExportAsn1Statistics(TextWriter Output)
		{
			KeyValuePair<string, int>[] AlgorithmsUsed = ASN1.GetAlgorithmsUsed(true);
			string s;

			KeyValuePair<string, int>[] OidsNotRecognized = ASN1.GetOidsNotRecognized(true);

			Output.WriteLine();
			Output.WriteLine("OIDs not recognized:");
			Output.WriteLine();
			Output.WriteLine("| OID                                    | Nr Times |");
			Output.WriteLine("|:---------------------------------------|---------:|");

			foreach (KeyValuePair<string, int> P in OidsNotRecognized)
			{
				Output.Write("| ");
				Output.Write(P.Key);

				int i = 39 - P.Key.Length;
				if (i > 0)
					Output.Write(new string(' ', i));

				Output.Write('|');

				s = P.Value.ToString(CultureInfo.InvariantCulture);
				Output.Write(new string(' ', 9 - s.Length));
				Output.Write(s);
				Output.WriteLine(" |");
			}

			KeyValuePair<string, int>[] OidsNotConfigured = ASN1.GetOidsNotConfigured(true);

			Output.WriteLine();
			Output.WriteLine("OIDs not configured:");
			Output.WriteLine();
			Output.WriteLine("| OID                                    | Nr Times |");
			Output.WriteLine("|:---------------------------------------|---------:|");

			foreach (KeyValuePair<string, int> P in OidsNotConfigured)
			{
				Output.Write("| ");
				Output.Write(P.Key);

				int i = 39 - P.Key.Length;
				if (i > 0)
					Output.Write(new string(' ', i));

				Output.Write('|');

				s = P.Value.ToString(CultureInfo.InvariantCulture);
				Output.Write(new string(' ', 9 - s.Length));
				Output.Write(s);
				Output.WriteLine(" |");
			}

			if (AlgorithmsUsed.Length > 0)
			{
				Output.WriteLine();
				Output.WriteLine("Algorithms used:");
				Output.WriteLine();
				Output.WriteLine("| Algorithm                              | Nr Times |");
				Output.WriteLine("|:---------------------------------------|---------:|");

				foreach (KeyValuePair<string, int> P in AlgorithmsUsed)
				{
					Output.Write("| ");
					Output.Write(P.Key);

					int i = 39 - P.Key.Length;
					if (i > 0)
						Output.Write(new string(' ', i));

					Output.Write('|');

					s = P.Value.ToString(CultureInfo.InvariantCulture);
					Output.Write(new string(' ', 9 - s.Length));
					Output.Write(s);
					Output.WriteLine(" |");
				}
			}
			else
			{
				Output.WriteLine();
				Output.WriteLine("No Elliptic Curves used.");
				Output.WriteLine();
			}

			KeyValuePair<string, int>[] UnrecognizedCurvesUsed = ASN1.GetUnrecognizedEllipticCurvesUsed(true);

			if (UnrecognizedCurvesUsed.Length > 0)
			{
				Output.WriteLine();
				Output.WriteLine("Unrecognized Elliptic Curves used:");
				Output.WriteLine();

				foreach (KeyValuePair<string, int> P in UnrecognizedCurvesUsed)
				{
					Output.WriteLine();
					Output.WriteLine("Times: " + P.Value.ToString(CultureInfo.InvariantCulture));
					Output.WriteLine(P.Key);
				}
			}
			else
			{
				Output.WriteLine();
				Output.WriteLine("No unrecognized Elliptic Curves used.");
				Output.WriteLine();
			}
		}

		[TestMethod]
		//[DataRow("..\\..\\..\\..\\..\\IcaoPkiCertificates\\Root\\IcaoPki\\AE\\01C1CA4806FA8A1DCD50AFC75E216E90479AF7C4.cer")]
		//[DataRow("..\\..\\..\\..\\..\\IcaoPkiCertificates\\Root\\IcaoPki\\AD\\031B14A8421B68EFA0BFD081C88C2B64270542A9.cer")]
		//[DataRow("..\\..\\..\\..\\..\\IcaoPkiCertificates\\Root\\IcaoPki\\AE\\05A76DC6681AEF89920BAE83F027ED9000A57742.cer")]
		//[DataRow("..\\..\\..\\..\\..\\IcaoPkiCertificates\\Root\\IcaoPki\\AE\\0A556519D1EFF9AD209F8A72C3E9035E87299169.cer")]
		//[DataRow("..\\..\\..\\..\\..\\IcaoPkiCertificates\\Root\\IcaoPki\\BA\\8F326711052AEEF40F26E408E1773406C7BE4851.cer")]
		//[DataRow("..\\..\\..\\..\\..\\IcaoPkiCertificates\\Root\\IcaoPki\\BG\\1EFF7CD794A9D0986230A3492311CDCE0C649C12.cer")]
		//[DataRow("..\\..\\..\\..\\..\\IcaoPkiCertificates\\Root\\IcaoPki\\NO\\002E5615116D88AD2CF18F27B45FF64FEACB91A8.cer")]
		//[DataRow("..\\..\\..\\..\\..\\IcaoPkiCertificates\\Root\\IcaoPki\\SE\\B0E5DF7C87A9235D3DB4CF35306ED61C76BEB7C6.cer")]
		[DataRow("..\\..\\..\\..\\..\\IcaoPkiCertificates\\Root\\IcaoPki\\DZ\\5FED4CD3BCA95F5C8F3673C61AEC837BFE2B2B51.cer")]
		public async Task Test_05_VerifySpecificIcaoCertificates(string FileName)
		{
			TestContextWriter SnifferWriter = new(this.TestContext);
			TextWriterSniffer Sniffer = new(SnifferWriter, BinaryPresentationMethod.Hexadecimal, "Unit Test Sniffer");
			CommunicationLayer Client = new(true, Sniffer);
			byte[] Raw = File.ReadAllBytes(FileName);

			Assert.IsTrue(Certificate.TryParse(Raw, out Certificate? Cert));
			Console.Out.WriteLine(JSON.Encode(Cert.Asn1Vector, true));

			Certificate[] Chain = await CertificateChain.GetChain(Cert, idDomain);
			Assert.IsTrue(CertificateChain.VerifySignatures(Client, Chain));
		}

	}
}
