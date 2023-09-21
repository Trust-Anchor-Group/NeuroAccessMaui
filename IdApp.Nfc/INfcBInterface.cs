using System.Threading.Tasks;

namespace IdApp.Nfc
{
	/// <summary>
	/// NFC B interface, for communication with an NFC Tag.
	/// </summary>
	public interface INfcBInterface : INfcInterface
	{
		/// <summary>
		/// Gets Application Data from the interface.
		/// </summary>
		Task<byte[]> GetApplicationData();

		/// <summary>
		/// Gets Protocol Information from the interface.
		/// </summary>
		Task<byte[]> GetProtocolInfo();
	}
}
