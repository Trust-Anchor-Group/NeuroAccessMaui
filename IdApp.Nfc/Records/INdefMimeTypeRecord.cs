namespace IdApp.Nfc.Records
{
	/// <summary>
	/// Interface for NDEF MIME Type records
	/// </summary>
	public interface INdefMimeTypeRecord : INdefRecord
	{
		/// <summary>
		/// Content-Type
		/// </summary>
		string ContentType { get; }

		/// <summary>
		/// Data Payload
		/// </summary>
		byte[] Data { get; }
	}
}
