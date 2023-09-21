namespace IdApp.Nfc.Records
{
	/// <summary>
	/// Interface for NDEF URI records
	/// </summary>
	public interface INdefUriRecord : INdefRecord
	{
		/// <summary>
		/// URI
		/// </summary>
		string Uri { get; }
	}
}
