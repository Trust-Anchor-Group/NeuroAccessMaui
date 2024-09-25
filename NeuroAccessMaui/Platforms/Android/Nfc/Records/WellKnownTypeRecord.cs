using Android.Nfc;
using NeuroAccess.Nfc.Records;
using System.Text;

namespace NeuroAccessMaui.AndroidPlatform.Nfc.Records
{
	/// <summary>
	/// Well-Known Type encoded NDEF Record.
	/// </summary>
	/// <param name="Record">Android NDEF Record</param>
	/// <param name="Type">Record type.</param>
	public class WellKnownTypeRecord(NdefRecord Record, string Type)
		: Record(Record), INdefWellKnownTypeRecord
	{
		private readonly string type = Type;
		private readonly byte[] data = Record.GetPayload() ?? throw NfcInterface.UnableToReadDataFromDevice();

		/// <summary>
		/// Well-Known Type encoded NDEF Record.
		/// </summary>
		/// <param name="Record">Android NDEF Record</param>
		public WellKnownTypeRecord(NdefRecord Record)
			: this(Record, Encoding.UTF8.GetString(Record.GetTypeInfo() ?? throw NfcInterface.UnableToReadDataFromDevice()))
		{
		}

		/// <summary>
		/// Type of NDEF record
		/// </summary>
		public override NDefRecordType Type => NDefRecordType.WellKnownType;

		/// <summary>
		/// NFC Well-Known Type
		/// 
		/// Reference:
		/// https://nfc-forum.org/our-work/specification-releases/specifications/nfc-forum-assigned-numbers-register/
		/// </summary>
		public string WellKnownType => this.type;

		/// <summary>
		/// Data Payload
		/// </summary>
		public byte[] Data => this.data;
	}
}
