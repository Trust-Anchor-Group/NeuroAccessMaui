using Android.Nfc;
using Android.Nfc.Tech;
using NeuroAccess.Nfc;

namespace NeuroAccessMaui.AndroidPlatform.Nfc
{
	/// <summary>
	/// Class handling ISO DEP Interfaces.
	/// </summary>
	/// <param name="Tag">Underlying Android Tag object.</param>
	/// <param name="Technology">NFC interface.</param>
	public class IsoDepInterface(Tag Tag, IsoDep Technology)
		: NfcInterface(Tag, Technology), IIsoDepInterface
	{
		private readonly IsoDep isoDep = Technology;

		/// <summary>
		/// Return the higher layer response bytes for NfcB tags.
		/// </summary>
		public async Task<byte[]> GetHighLayerResponse()
		{
			await this.OpenIfClosed();
			return this.isoDep.GetHiLayerResponse() ?? throw UnableToReadDataFromDevice();
		}

		/// <summary>
		/// Return the ISO-DEP historical bytes for NfcA tags.
		/// </summary>
		public async Task<byte[]> GetHistoricalBytes()
		{
			await this.OpenIfClosed();
			return this.isoDep.GetHistoricalBytes() ?? throw UnableToReadDataFromDevice();
		}

		/// <summary>
		/// Executes an ISO 14443-4 command on the tag.
		/// </summary>
		/// <param name="Command">Command</param>
		/// <returns>Response</returns>
		public async Task<byte[]> ExecuteCommand(byte[] Command)
		{
			await this.OpenIfClosed();
			return await this.isoDep.TransceiveAsync(Command) ?? throw UnableToReadDataFromDevice();
		}
	}
}
