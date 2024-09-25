using Android.Nfc;
using Android.Nfc.Tech;
using NeuroAccess.Nfc;

namespace NeuroAccessMaui.AndroidPlatform.Nfc
{
	/// <summary>
	/// Class handling NFC F Interfaces.
	/// </summary>
	/// <param name="Tag">Underlying Android Tag object.</param>
	/// <param name="Technology">NFC interface.</param>
	public class NfcFInterface(Tag Tag, NfcF Technology)
		: NfcInterface(Tag, Technology), INfcFInterface
	{
		private readonly NfcF nfcF = Technology;

		/// <summary>
		/// Return the Manufacturer bytes from tag discovery.
		/// </summary>
		public async Task<byte[]> GetManufacturer()
		{
			await this.OpenIfClosed();
			return this.nfcF.GetManufacturer() ?? throw UnableToReadDataFromDevice();
		}

		/// <summary>
		/// Return the System Code bytes from tag discovery.
		/// </summary>
		public async Task<byte[]> GetSystemCode()
		{
			await this.OpenIfClosed();
			return this.nfcF.GetSystemCode() ?? throw UnableToReadDataFromDevice();
		}
	}
}
