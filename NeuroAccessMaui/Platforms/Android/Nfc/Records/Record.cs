using Android.Nfc;
using NeuroAccess.Nfc.Records;

namespace NeuroAccessMaui.AndroidPlatform.Nfc.Records
{
	/// <summary>
	/// Abstract base class for NDEF Records.
	/// </summary>
	/// <param name="Record">Android NDEF Record</param>
	public abstract class Record(NdefRecord Record) : INdefRecord
	{
		private readonly NdefRecord record = Record;

		/// <summary>
		/// Type of NDEF record
		/// </summary>
		public abstract NDefRecordType Type { get; }
	}
}
