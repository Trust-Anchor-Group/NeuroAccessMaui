using Android.Nfc;
using Android.Nfc.Tech;
using NeuroAccess.Nfc;

namespace NeuroAccessMaui.AndroidPlatform.Nfc
{
	/// <summary>
	/// Class handling NFC B Interfaces.
	/// </summary>
	/// <param name="Tag">Underlying Android Tag object.</param>
	/// <param name="Technology">NFC interface.</param>
	public class NfcBInterface(Tag Tag, NfcB Technology)
		: NfcInterface(Tag, Technology), INfcBInterface
	{
		private readonly NfcB nfcB = Technology;

		/// <summary>
		/// Gets Application Data from the interface.
		/// </summary>
		public Task<byte[]> GetApplicationData()
		{
			return Task.FromResult(this.nfcB.GetApplicationData() ?? throw UnableToReadDataFromDevice());
		}

		/// <summary>
		/// Gets Protocol Information from the interface.
		/// </summary>
		public Task<byte[]> GetProtocolInfo()
		{
			return Task.FromResult(this.nfcB.GetProtocolInfo() ?? throw UnableToReadDataFromDevice());
		}
	}
}
