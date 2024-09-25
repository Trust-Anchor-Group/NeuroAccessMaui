using System.Threading.Tasks;

namespace NeuroAccess.Nfc
{
	/// <summary>
	/// NFC F interface, for communication with an NFC Tag.
	/// </summary>
	public interface INfcFInterface : INfcInterface
	{
		/// <summary>
		/// Return the Manufacturer bytes from tag discovery.
		/// </summary>
		Task<byte[]> GetManufacturer();

		/// <summary>
		/// Return the System Code bytes from tag discovery.
		/// </summary>
		Task<byte[]> GetSystemCode();
	}
}
