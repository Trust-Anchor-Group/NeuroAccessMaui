using System;

namespace IdApp.Nfc
{
	/// <summary>
	/// Interface for an NFC Tag.
	/// </summary>
	public interface INfcTag : IDisposable
	{
		/// <summary>
		/// NFC Tag ID
		/// </summary>
		byte[] ID
		{
			get;
		}

		/// <summary>
		/// Communication interfaces available on the NFC Tag.
		/// </summary>
		INfcInterface[] Interfaces
		{
			get;
		}
	}
}
