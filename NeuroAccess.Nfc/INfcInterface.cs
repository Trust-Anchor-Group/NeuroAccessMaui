using System;
using System.Threading.Tasks;

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

		/// <summary>
		/// Connects the interface, if not connected.
		/// </summary>
		/// <returns></returns>
		Task OpenIfClosed();

		/// <summary>
		/// Closes the interface, if connected.
		/// </summary>
		void CloseIfOpen();
	}
}
