using Android.Nfc;
using Android.Nfc.Tech;
using NeuroAccess.Nfc;

namespace NeuroAccessMaui.AndroidPlatform.Nfc
{
	/// <summary>
	/// Class handling NDEF Formatable Interfaces.
	/// </summary>
	/// <param name="Tag">Underlying Android Tag object.</param>
	/// <param name="Technology">NFC interface.</param>
	public class NdefFormatableInterface(Tag Tag, NdefFormatable Technology)
		: NfcInterface(Tag, Technology), INdefFormatableInterface
	{
		private readonly NdefFormatable ndefFormatable = Technology;
	}
}
