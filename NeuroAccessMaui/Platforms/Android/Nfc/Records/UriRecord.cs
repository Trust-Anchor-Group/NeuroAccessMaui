using Android.Nfc;
using NeuroAccess.Nfc.Records;

namespace NeuroAccessMaui.AndroidPlatform.Nfc.Records
{
	/// <summary>
	/// Absolute URI NDEF Record.
	/// </summary>
	/// <param name="Record">Android NDEF Record</param>
	public class UriRecord(NdefRecord Record)
		: Record(Record), INdefUriRecord
	{
		private readonly string uri = Record.ToUri()?.ToString() ?? throw NfcInterface.UnableToReadDataFromDevice();

		/// <summary>
		/// Type of NDEF record
		/// </summary>
		public override NDefRecordType Type => NDefRecordType.Uri;

		/// <summary>
		/// URI
		/// </summary>
		public string Uri => this.uri;
	}
}
