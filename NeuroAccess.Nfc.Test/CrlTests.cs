using NeuroAccess.Nfc.TravelDocuments.RevocationLists;

namespace NeuroAccess.Nfc.Test
{
	[TestClass]
	public class CrlTests
	{
		[TestMethod]
		public void Test_01_ParseCrlWithoutRevokedCertificates()
		{
			// Finnish CSCA CRL without a revokedCertificates sequence.
			// Source: http://proxy.fineid.fi/crl/cscafinc.crl
			byte[] Bin = Convert.FromBase64String(
				"MIIBQzCBpwIBATAMBggqhkjOPQQDBAUAMEQxCzAJBgNVBAYTAkZJMRAwDgYDVQQKDAdGaW5sYW5kMQwwCgYDVQQLDANWUksxFTATBgNVBAMMDENTQ0EgRmlubGFuZBcNMjYwNzIzMDcxOTU4WhcNMjYwOTAxMDcxOTU4WqAwMC4wHwYDVR0jBBgwFoAUqYvqjNOZ0P8SvNhU0jdH5hofwAQwCwYDVR0UBAQCAgFBMAwGCCqGSM49BAMEBQADgYgAMIGEAkBbrEa0EZBYa+fl6bZuy0JhoOpf+g/uEAndjdPsLYVVSDrGjG/DoW5/VlfGAmFe4or0F3tHljbf1xgape88h0c6AkBoR+pTfZ1WahlaAYe8pfDD+aBB3cq1HiI/DlVU75yxAseYK07Y3TxGBTz5sBn509uhKAsHaeX7F11fUztDRtWu");

			Assert.IsTrue(CertificateList.TryParse(Bin, out CertificateList? Crl));
			Assert.IsNotNull(Crl);
			Assert.AreEqual(0, Crl.RevokedCertificates.Length);
		}
	}
}
