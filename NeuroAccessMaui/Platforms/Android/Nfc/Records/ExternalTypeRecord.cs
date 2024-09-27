using Android.Nfc;
using NeuroAccess.Nfc.Records;
using System.Text;

namespace NeuroAccessMaui.AndroidPlatform.Nfc.Records
{
	/// <summary>
	/// External Type encoded NDEF Record.
	/// </summary>
	/// <param name="Record">Android NDEF Record</param>
	public class ExternalTypeRecord(NdefRecord Record)
		: Record(Record), INdefExternalTypeRecord
	{
		private readonly string type = Encoding.UTF8.GetString(Record.GetTypeInfo() ?? throw NfcInterface.UnableToReadDataFromDevice());
		private readonly byte[] data = Record.GetPayload() ?? throw NfcInterface.UnableToReadDataFromDevice();

		/// <summary>
		/// Type of NDEF record
		/// </summary>
		public override NDefRecordType Type => NDefRecordType.ExternalType;

		/// <summary>
		/// External Type
		/// </summary>
		public string ExternalType => this.type;

		/// <summary>
		/// Data Payload
		/// </summary>
		public byte[] Data => this.data;
	}
}
