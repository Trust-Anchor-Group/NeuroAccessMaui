namespace NeuroAccess.Nfc.Records
{
	/// <summary>
	/// Interface for NDEF records
	/// </summary>
	public interface INdefRecord
	{
		/// <summary>
		/// Type of NDEF record
		/// </summary>
		NDefRecordType Type { get; }
	}
}
