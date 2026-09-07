using NeuroAccess.Nfc.TravelDocuments;
using Waher.Networking;

namespace NeuroAccess.Nfc.Test
{
	[TestClass]
	public class ApduResponseTests
	{
		[TestMethod]
		public async Task Test_01_ReadBinary_Follows_GetResponse_When_Data_Still_Available()
		{
			// READ BINARY is specified for LDS files in ICAO 9303-10, section 3.6.3:
			// https://www.icao.int/sites/default/files/publications/DocSeries/9303_p10_cons_en.pdf

			ScriptedIsoDepInterface TagInterface = new(
				[0xde, 0x61, 0x02],
				[0xad, 0x90, 0x00]);

			using TravelDocumentsClient Client = new(TagInterface, CreateDocumentInformation(), null);

			KeyValuePair<byte[]?, bool> Result = await Client.ReadBinary(0, 2);

			Assert.IsNotNull(Result.Key);
			CollectionAssert.AreEqual(new byte[] { 0xde, 0xad }, Result.Key);
			Assert.IsFalse(Result.Value);

			Assert.HasCount(2, TagInterface.Commands);
			CollectionAssert.AreEqual(new byte[] { 0x00, 0xb0, 0x00, 0x00, 0x02 }, TagInterface.Commands[0]);
			CollectionAssert.AreEqual(new byte[] { 0x00, 0xc0, 0x00, 0x00, 0x02 }, TagInterface.Commands[1]);
		}

		[TestMethod]
		public async Task Test_02_ReadBinary_Retries_With_Correct_Length_When_Le_Is_Wrong()
		{
			// READ BINARY is specified for LDS files in ICAO 9303-10, section 3.6.3:
			// https://www.icao.int/sites/default/files/publications/DocSeries/9303_p10_cons_en.pdf

			ScriptedIsoDepInterface TagInterface = new(
				[0x6c, 0x02],
				[0xbe, 0xef, 0x90, 0x00]);

			using TravelDocumentsClient Client = new(TagInterface, CreateDocumentInformation(), null);

			KeyValuePair<byte[]?, bool> Result = await Client.ReadBinary(0, 0);

			Assert.IsNotNull(Result.Key);
			CollectionAssert.AreEqual(new byte[] { 0xbe, 0xef }, Result.Key);
			Assert.IsFalse(Result.Value);

			Assert.HasCount(2, TagInterface.Commands);
			CollectionAssert.AreEqual(new byte[] { 0x00, 0xb0, 0x00, 0x00, 0x00 }, TagInterface.Commands[0]);
			CollectionAssert.AreEqual(new byte[] { 0x00, 0xb0, 0x00, 0x00, 0x02 }, TagInterface.Commands[1]);
		}

		private static DocumentInformation CreateDocumentInformation()
		{
			return new DocumentInformation()
			{
				MRZ_Information = "L898902C<369080619406236"
			};
		}

		private sealed class ScriptedIsoDepInterface : IIsoDepInterface
		{
			private readonly Queue<byte[]> responses;

			public ScriptedIsoDepInterface(params byte[][] Responses)
			{
				this.responses = new Queue<byte[]>(Responses);
			}

			public List<byte[]> Commands { get; } = [];

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
				this.Commands.Add((byte[])Command.Clone());

				if (this.responses.Count == 0)
					throw new InvalidOperationException("No scripted response available.");

				return Task.FromResult(this.responses.Dequeue());
			}

			public void Dispose()
			{
			}
		}
	}
}
