using IdApp.Nfc;
using Waher.Runtime.Inventory;

namespace NeuroAccessMaui.Services.Nfc
{
	/// <summary>
	/// Interface for the Near-Field Communication (NFC) Service.
	/// </summary>
	[DefaultImplementation(typeof(NfcService))]
	public interface INfcService
	{
		/// <summary>
		/// Method called when a new NFC Tag has been detected.
		/// </summary>
		/// <param name="Tag">NFC Tag</param>
		Task TagDetected(INfcTag Tag);
	}
}
