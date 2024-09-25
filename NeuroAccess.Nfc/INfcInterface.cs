using System;

namespace NeuroAccess.Nfc
{
	/// <summary>
	/// Specific Interface (technology) for communication with an NFC Tag.
	/// </summary>
	public interface INfcInterface : IDisposable
	{
		/// <summary>
		/// NFC Tag
		/// </summary>
		INfcTag? Tag
		{
			get;
		}
	}
}
