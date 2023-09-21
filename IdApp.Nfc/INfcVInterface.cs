using System.Threading.Tasks;

namespace IdApp.Nfc
{
	/// <summary>
	/// NFC V interface, for communication with an NFC Tag.
	/// </summary>
	public interface INfcVInterface : INfcInterface
	{
		/// <summary>
		/// Return the DSF ID bytes from tag discovery.
		/// </summary>
		Task<sbyte> GetDsfId();

		/// <summary>
		/// Return the Response Flag bytes from tag discovery.
		/// </summary>
		Task<short> GetResponseFlags();
	}
}
