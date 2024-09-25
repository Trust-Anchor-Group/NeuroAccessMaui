using Android.Nfc;
using Android.Nfc.Tech;
using NeuroAccess.Nfc;

namespace NeuroAccessMaui.AndroidPlatform.Nfc
{
	/// <summary>
	/// Class handling NFC V Interfaces.
	/// </summary>
	/// <remarks>
	/// Class handling NFC V Interfaces.
	/// </remarks>
	/// <param name="Tag">Underlying Android Tag object.</param>
	/// <param name="Technology">Underlying Android Technology object.</param>
	public class NfcVInterface(Tag Tag, NfcV Technology)
		: NfcInterface(Tag, Technology), INfcVInterface
	{
		private readonly NfcV nfcV = Technology;

		/// <summary>
		/// Return the DSF ID bytes from tag discovery.
		/// </summary>
		public Task<sbyte> GetDsfId()
		{
			return Task.FromResult(this.nfcV.DsfId);
		}

		/// <summary>
		/// Return the Response Flag bytes from tag discovery.
		/// </summary>
		public Task<short> GetResponseFlags()
		{
			return Task.FromResult((short)this.nfcV.ResponseFlags);
		}
	}
}
