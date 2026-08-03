using NeuroAccess.Nfc.TravelDocuments;
using NeuroAccess.Nfc.TravelDocuments.DataObjects;
using static NeuroAccess.Nfc.Test.EfSodTests;

namespace NeuroAccess.Nfc.Test
{
	[TestClass]
	public class EfComTests
	{
		[TestMethod]
		[DataRow("YBdfAQQwMTA3XzYGMDQwMDAwXAVhdWNubw==")]
		[DataRow("YBdfAQQwMTA3XzYGMDQwMDAwXAVhdWNnbg==")]
		[DataRow("YBhfAQQwMTA3XzYGMDQwMDAwXAZhdWNrbG4=")]
		[DataRow("YBZfAQQwMTA3XzYGMDQwMDAwXARhdWNu")]
		[DataRow("YBhfAQQwMTA3XzYGMDQwMDAwXAZhdWNnbm8=")]
		[DataRow("YBlfAQQwMTA3XzYGMDQwMDAwXAdhdWNrbG1u")]
		public async Task Test_01_Parse(string Base64)
		{
			byte[] Bin = Convert.FromBase64String(Base64);
			TravelDocumentsClient Client = new(new MockIsoDepInterface(), new DocumentInformation(), null)
			{
				PermitPlatformDependentValidation = false
			};

			Assert.IsTrue(TravelDocumentsClient.TryParseDataObject(Bin, Client,
				out ApplicationLevelInformation? AppInfo));
			Assert.IsNotNull(AppInfo);
		}
	}
}
