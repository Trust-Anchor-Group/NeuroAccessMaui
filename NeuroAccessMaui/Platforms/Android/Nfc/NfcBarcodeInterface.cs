using Android.Nfc;
using Android.Nfc.Tech;
using NeuroAccess.Nfc;

namespace NeuroAccessMaui.AndroidPlatform.Nfc
{
	/// <summary>
	/// Class handling NFC Bardcode Interfaces.
	/// </summary>
	/// <remarks>
	/// Class handling NFC Bardcode Interfaces.
	/// </remarks>
	/// <param name="Tag">Underlying Android Tag object.</param>
	/// <param name="Technology">NFC interface.</param>
	public class NfcBarcodeInterface(Tag Tag, NfcBarcode Technology)
		: NfcInterface(Tag, Technology), INfcBarcodeInterface
	{
		private readonly NfcBarcode nfcBarcode = Technology;

		/// <summary>
		/// Reads all data from the interface
		/// </summary>
		public async Task<byte[]> ReadAllData()
		{
			await this.OpenIfClosed();

			return this.nfcBarcode.GetBarcode() ?? throw UnableToReadDataFromDevice();
		}
	}
}
