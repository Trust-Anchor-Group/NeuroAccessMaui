using NeuroAccess.Nfc.TravelDocuments;
using NeuroAccess.Nfc.TravelDocuments.ISO19794;
using Waher.Events;
using Waher.Networking.Sniffers;

namespace NeuroAccess.Nfc.Test
{
	[TestClass]
	public class ReadoutTests
	{
		/// <summary>
		/// Test context
		/// </summary>
		public TestContext TestContext { get; set; } = null!;

		[TestMethod]
		[DataRow("C:\\Temp\\NFC.xml")]
		public async Task Test_01_TravelDocumentReadout(string FileName)
		{
			if (!File.Exists(FileName))
			{
				Assert.Inconclusive("NFC replay file not found.");
				return;
			}

			IsoDepReplay Replay = new(FileName);

			TestContextWriter SnifferWriter = new(this.TestContext);
			TextWriterSniffer Sniffer = new(SnifferWriter, BinaryPresentationMethod.Hexadecimal, "Unit Test Sniffer");

			using TravelDocumentsClient Client = new(Replay, Replay.DocumentInfo, null, Sniffer);

			Client.Information("Starting readout.");

			// TODO: Seed PACE authentication with ID of PREVIEW application, so that
			// Neuron can cryptographically validate the readout is not a replay of a
			// previous readout.

			Assert.AreEqual(AuthenticateResult.Success, await Client.Authenticate());

			bool AppInfoRead = false;
			bool SecurityInfoRead = false;
			bool MrzRead = false;
			bool FaceRead = false;

			Client.AppInfoUpdated += (_, e) =>
			{
				AppInfoRead = true;
				return Task.CompletedTask;
			};

			Client.SecurityInfoUpdated += (_, e) =>
			{
				SecurityInfoRead = true;
				return Task.CompletedTask;
			};

			Client.MrzUpdated += (_, e) =>
			{
				MrzRead = true;
				return Task.CompletedTask;
			};

			Client.BiometricEncodingFaceUpdated += (_, e) =>
			{
				FaceRead = true;

				if (Client.BiometricEncodingFace is not null)
				{
					Representation? Face = Client.BiometricEncodingFace[0].BiometricDataBlock?.Record?.Representations[0];

					if (Face is not null)
					{
						Client.Warning("Face Image (type: " + Face.ImageDataType.ToString() + "):\r\n\r\n" +
							Convert.ToBase64String(Face.ImageData, Base64FormattingOptions.InsertLineBreaks));
					}
				}

				return Task.CompletedTask;
			};

			Assert.AreEqual(ReadTravelDocumentResult.Success,
				await Client.ReadTravelDocument(CertificateTests.IdDomain));

			Client.Information("Readout completed.");

			await Sniffer.FlushAsync();

			Assert.IsTrue(AppInfoRead);
			Assert.IsTrue(SecurityInfoRead);
			Assert.IsTrue(MrzRead);
			Assert.IsTrue(FaceRead);
		}
	}
}
