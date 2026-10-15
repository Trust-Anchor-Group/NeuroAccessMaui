using System;

namespace NeuroAccess.Nfc.Exceptions
{
	/// <summary>
	/// Represents a low-level NFC transport failure caused by losing the tag connection.
	/// </summary>
	/// <remarks>
	/// Platform interfaces throw this when the tag leaves the field, so that callers can tell an interrupted
	/// read apart from a document that could not be authenticated or parsed.
	/// </remarks>
	public sealed class NfcConnectionLostException : Exception
	{
		/// <summary>
		/// Initializes a new instance of the <see cref="NfcConnectionLostException"/> class.
		/// </summary>
		/// <param name="Message">The exception message.</param>
		public NfcConnectionLostException(string Message)
			: base(Message)
		{
		}

		/// <summary>
		/// Initializes a new instance of the <see cref="NfcConnectionLostException"/> class.
		/// </summary>
		/// <param name="Message">The exception message.</param>
		/// <param name="InnerException">The platform exception that reported the lost connection.</param>
		public NfcConnectionLostException(string Message, Exception InnerException)
			: base(Message, InnerException)
		{
		}
	}
}
