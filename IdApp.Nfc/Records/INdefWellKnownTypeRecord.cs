namespace IdApp.Nfc.Records
{
	/// <summary>
	/// Interface for NDEF Well-Known Type records
	/// </summary>
	public interface INdefWellKnownTypeRecord : INdefRecord
	{
		/// <summary>
		/// NFC Well-Known Type
		/// 
		/// Reference:
		/// https://nfc-forum.org/our-work/specification-releases/specifications/nfc-forum-assigned-numbers-register/
		/// </summary>
		string WellKnownType { get; }

		/// <summary>
		/// Data Payload
		/// </summary>
		byte[] Data { get; }
	}
}
