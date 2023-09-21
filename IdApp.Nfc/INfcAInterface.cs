using System.Threading.Tasks;

namespace IdApp.Nfc
{
	/// <summary>
	/// NFC A interface, for communication with an NFC Tag.
	/// </summary>
	public interface INfcAInterface : INfcInterface
	{
		/// <summary>
		/// Return the ATQA/SENS_RES bytes from tag discovery.
		/// </summary>
		Task<byte[]> GetAtqa();

		/// <summary>
		/// Return the SAK/SEL_RES bytes from tag discovery.
		/// </summary>
		Task<short> GetSqk();
	}
}
