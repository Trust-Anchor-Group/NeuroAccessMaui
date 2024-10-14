using Android.Nfc;
using NeuroAccess.Nfc.Records;

namespace NeuroAccessMaui.AndroidPlatform.Nfc.Records
{
	/// <summary>
	/// MIME Type encoded NDEF Record.
	/// </summary>
	/// <param name="Record">Android NDEF Record</param>
	public class MimeTypeRecord(NdefRecord Record)
		: Record(Record), INdefMimeTypeRecord
	{
		private readonly string contentType = Record.ToMimeType() ?? throw NfcInterface.UnableToReadDataFromDevice();
		private readonly byte[] data = Record.GetPayload() ?? throw NfcInterface.UnableToReadDataFromDevice();

		/// <summary>
		/// Type of NDEF record
		/// </summary>
		public override NDefRecordType Type => NDefRecordType.MimeType;

		/// <summary>
		/// Content-Type
		/// </summary>
		public string ContentType => this.contentType;

		/// <summary>
		/// Data Payload
		/// </summary>
		public byte[] Data => this.data;
	}
}
