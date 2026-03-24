using System.Globalization;
using System.Security.Cryptography.Pkcs;
using System.Security.Cryptography.X509Certificates;
using System.Text;
using NeuroAccess.Nfc.TravelDocuments;
using NeuroAccess.Nfc.TravelDocuments.RevocationLists;
using NeuroAccess.Nfc.TravelDocuments.Security;
using Waher.Content;
using Waher.Runtime.Collections;
using Waher.Runtime.Inventory;
using Waher.Security;

namespace NeuroAccess.Nfc.Test
{
	[TestClass]
	public class SodTests
	{
		// Testing parsing of Document Security Objects. (§4.6.2, ICAO 9303-10)

		[TestMethod]
		[DataRow("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")]
		public void Test_01_Parse_LDS_v1_8(string Base64)
		{
			byte[] Bin = Convert.FromBase64String(Base64);

			SignedCms SignedData = new();
			SignedData.Decode(Bin);

			TravelDocumentsClient.TryDecodeDER(SignedData.ContentInfo.Content, out object? Content);
			Vector? ContentVector = Content as Vector;
			Assert.IsNotNull(ContentVector);

			ISecurityObject SecurityObject = Types.FindBest<ISecurityObject, string>(SignedData.ContentInfo.ContentType.Value!);
			Assert.IsTrue(SecurityObject.Configure(ContentVector.Elements));

			Console.Out.WriteLine(JSON.Encode(SecurityObject, true));
			Console.Out.WriteLine();
			Console.Out.WriteLine();
			Console.Out.WriteLine();
			Console.Out.WriteLine(JSON.Encode(SignedData, true));

			SignedData.CheckSignature(true);

			foreach (X509Certificate2 Certificate in SignedData.Certificates)
			{
				KeyValuePair<string?, byte[]?> P = TravelDocumentsClient.GetAuthorityKeyIdentifier(Certificate);
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
			byte[] Bin = Convert.FromBase64String(Base64);

			SignedCms SignedData = new();
			SignedData.Decode(Bin);
			SignedData.CheckSignature(true);

			foreach (X509Certificate2 Certificate in SignedData.Certificates)
			{
				Dictionary<string, bool> Processed = [];
				ChunkedList<X509Certificate2> Certificates = [];
				KeyValuePair<string?, byte[]?> P = TravelDocumentsClient.GetAuthorityKeyIdentifier(Certificate);
				Dictionary<string, bool> CrlUrls = [];
				string? CountryCode = P.Key;
				byte[]? IssuerKeyReference = P.Value;

				// Authority Key Identifier (AKI) extension required by ICAO.
				Assert.IsNotNull(CountryCode);
				if (string.IsNullOrEmpty(CountryCode) || IssuerKeyReference is null)
					Assert.Fail("No Authority Key Identifier found in certificate.");

				while (!string.IsNullOrEmpty(CountryCode) && IssuerKeyReference is not null)
				{
					string Uri = "https://id.tagroot.io/IcaoPki/" + CountryCode +
						"/" + Hashes.BinaryToString(IssuerKeyReference).ToUpper(CultureInfo.InvariantCulture) +
						".cer";

					if (Processed.ContainsKey(Uri))
						break;

					// AKI needs to point to a certificate published by the ICAO

					ContentResponse Response = await InternetContent.GetAsync(new Uri(Uri));
					Response.AssertOk();

					Processed[Uri] = true;

					X509Certificate2 IssuerCertificate = X509CertificateLoader.LoadCertificate(Response.Encoded);
					Certificates.Insert(0, IssuerCertificate);

					// Make sure to use Certificate Revocation Lists (CRLs) from ICAO approved certificates.

					foreach (string CrlUrl in TravelDocumentsClient.GetRevocationListUrls(IssuerCertificate))
						CrlUrls[CrlUrl] = true;

					P = TravelDocumentsClient.GetAuthorityKeyIdentifier(IssuerCertificate);
					CountryCode = P.Key;
					IssuerKeyReference = P.Value;
				}

				foreach (string CrlUrl in TravelDocumentsClient.GetRevocationListUrls(Certificate))
					CrlUrls[CrlUrl] = true;

				Assert.IsGreaterThan(0, CrlUrls.Count);

				foreach (string CrlUrl in CrlUrls.Keys)
				{
					ContentResponse Response = await InternetContent.GetAsync(new Uri(CrlUrl));
					Response.AssertOk();

					CertificateList? RevokedCertificates = Response.Decoded as CertificateList;
					Assert.IsNotNull(RevokedCertificates);

					Console.Out.WriteLine(CrlUrl);
					Console.Out.WriteLine(new string('=', 80));
					Console.Out.WriteLine(JSON.Encode(Response.Decoded, true));

					if (RevokedCertificates.HasBeenRevoked(Certificate, out RevokedReason Reason))
						Assert.Fail("Certificate " + Certificate.SerialNumber + " has been revoked: " + Reason.ToString());

					foreach (X509Certificate2 Certificate2 in Certificates)
					{
						if (RevokedCertificates.HasBeenRevoked(Certificate2, out Reason))
							Assert.Fail("Certificate " + Certificate2.SerialNumber + " has been revoked: " + Reason.ToString());
					}
				}

				X509Chain Chain = X509Chain.Create();
				bool First = true;

				Chain.ChainPolicy.RevocationMode = X509RevocationMode.NoCheck;       // Custom Revocation List check performed earlier
				Chain.ChainPolicy.RevocationFlag = X509RevocationFlag.EntireChain;
				Chain.ChainPolicy.VerificationFlags = X509VerificationFlags.NoFlag;
				Chain.ChainPolicy.TrustMode = X509ChainTrustMode.CustomRootTrust;
				Chain.ChainPolicy.UrlRetrievalTimeout = TimeSpan.FromSeconds(30);

				foreach (X509Certificate2 Certificate2 in Certificates)
				{
					if (First)
					{
						Chain.ChainPolicy.CustomTrustStore.Add(Certificate2);
						First = false;
					}
					else
						Chain.ChainPolicy.ExtraStore.Add(Certificate2);
				}

				if (!Chain.Build(Certificate))
				{
					StringBuilder sb = new();

					sb.AppendLine("Validation failed: ");

					foreach (X509ChainStatus Status in Chain.ChainStatus)
						sb.AppendLine(Status.StatusInformation);

					Assert.Fail(sb.ToString());
				}
			}
		}
	}

}
