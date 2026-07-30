namespace NeuroAccessMaui.Services.Nfc
{
	/// <summary>
	/// Represents a low-level NFC transport failure caused by losing the tag connection.
	/// </summary>
	internal sealed class NfcConnectionLostException : InvalidOperationException
	{
		/// <summary>
		/// Initializes a new instance of the <see cref="NfcConnectionLostException"/> class.
		/// </summary>
		/// <param name="Message">The exception message.</param>
		public NfcConnectionLostException(string Message)
			: base(Message)
		{
		}
	}
}
