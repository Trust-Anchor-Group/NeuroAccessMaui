using NeuroAccess.Nfc.TravelDocuments;
using NeuroAccess.Nfc.TravelDocuments.DataObjects;
using static NeuroAccess.Nfc.Test.EfSodTests;

namespace NeuroAccess.Nfc.Test
{
	[TestClass]
	public class EfCardAccessTests
	{
		[TestMethod]
		[DataRow("MSgwEgYKBAB/AAcCAgQCBAIBAgIBEDASBgoEAH8ABwICBAYEAgECAgEQ")]
		[DataRow("MRQwEgYKBAB/AAcCAgQCBAIBAgIBDg==")]
		[DataRow("MRQwEgYKBAB/AAcCAgQCAgIBAgIBDQ==")]
		[DataRow("MUIwEgYKBAB/AAcCAgQCBAIBAgIBDTAVBgkEAH8ABwICDAEwAwIBATADAwEAMBUGCQQAfwAHAgIMAjADAgECMAMDAQA=")]
		[DataRow("MRQwEgYKBAB/AAcCAgQBAQIBAgIBAg==")]
		[DataRow("MSgwEgYKBAB/AAcCAgQEBAIBAgIBDTASBgoEAH8ABwICBAIEAgECAgEQ")]
		[DataRow("MSgwEgYKBAB/AAcCAgQCBAIBAgIBEDASBgoEAH8ABwICBAQEAgECAgEN")]
		[DataRow("MRQwEgYKBAB/AAcCAgQCBAIBAgIBDA==")]
		public async Task Test_01_Parse(string Base64)
		{
			byte[] Bin = Convert.FromBase64String(Base64);
			TravelDocumentsClient Client = new(new MockIsoDepInterface(), new DocumentInformation(), null)
			{
				AppInfo = new ApplicationLevelInformation(Array.Empty<byte>(), 1, 8, 0, 0, null),
				PermitPlatformDependentValidation = false
			};

			Assert.IsTrue(ASN1.TryDecodeDer(Client, Bin, out object? CardAccess));
			Assert.IsNotNull(CardAccess);
			Assert.IsTrue(await Client.TryFindPaceProtocol(CardAccess));
		}
	}
}
