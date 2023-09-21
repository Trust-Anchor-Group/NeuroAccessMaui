namespace IdApp.Nfc.Records
{
	/// <summary>
	/// Interface for NDEF External Type records
	/// </summary>
	public interface INdefExternalTypeRecord : INdefRecord
	{
		/// <summary>
		/// NFC External Type
		/// </summary>
		string ExternalType { get; }

		/// <summary>
		/// Data Payload
		/// </summary>
		byte[] Data { get; }
	}
}
