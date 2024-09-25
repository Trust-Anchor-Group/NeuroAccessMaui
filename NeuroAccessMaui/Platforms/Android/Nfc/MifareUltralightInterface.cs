using Android.Nfc;
using Android.Nfc.Tech;
using NeuroAccess.Nfc;

namespace NeuroAccessMaui.AndroidPlatform.Nfc
{
	/// <summary>
	/// Class handling MifareUltralight NFC Interfaces.
	/// </summary>
	/// <param name="Tag">Underlying Android Tag object.</param>
	/// <param name="Technology">NFC interface.</param>
	public class MifareUltralightInterface(Tag Tag, MifareUltralight Technology)
		: NfcInterface(Tag, Technology), IMifareUltralightInterface
	{
		private readonly MifareUltralight mifareUltralight = Technology;

		/// <summary>
		/// Reads all data from the interface
		/// </summary>
		public async Task<byte[]> ReadAllData()
		{
			await this.OpenIfClosed();

			MifareUltralightType Type = this.mifareUltralight.Type;
			int TotalBytes = Type switch
			{
				MifareUltralightType.UltralightC => 192,
				_ => 64,
			};
			int PageSize = MifareUltralight.PageSize;
			byte[] Data = new byte[TotalBytes];
			int Offset = 0;

			while (Offset < TotalBytes)
			{
				byte[]? Pages = await this.mifareUltralight.ReadPagesAsync(Offset / PageSize) ?? throw UnableToReadDataFromDevice();
				int i = Math.Min(Pages.Length, TotalBytes - Offset);
				if (i <= 0)
					throw UnableToReadDataFromDevice();

				Array.Copy(Pages, 0, Data, Offset, i);
				Offset += i;
			}

			return Data;
		}
	}
}
