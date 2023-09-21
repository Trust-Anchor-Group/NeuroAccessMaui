using System.Threading.Tasks;

namespace IdApp.Nfc
{
	/// <summary>
	/// Mifare Ultralight interface, for communication with an NFC Tag.
	/// </summary>
	public interface INfcReadableBinaryInterface : INfcInterface
	{
		/// <summary>
		/// Reads all data from the interface
		/// </summary>
		Task<byte[]> ReadAllData();
	}
}
