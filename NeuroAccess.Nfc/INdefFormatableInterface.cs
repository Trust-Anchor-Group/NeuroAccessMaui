using System.Threading.Tasks;

namespace NeuroAccess.Nfc
{
	/// <summary>
	/// NDEF Formatable interface, for communication with an NFC Tag.
	/// </summary>
	public interface INdefFormatableInterface : INfcInterface
	{
		/// <summary>
		/// Formats a message (of records) in the NFC tag.
		/// </summary>
		/// <param name="ReadOnly">If the formatted message should be read-only (true), or writable (false).</param>
		/// <param name="Items">Items to encode.</param>
		/// <returns>If the tag could be formatted with the message.</returns>
		Task<bool> Format(bool ReadOnly, params object[] Items);
	}
}
