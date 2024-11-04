using Android.Nfc;
using Android.Nfc.Tech;
using NeuroAccess.Nfc;

namespace NeuroAccessMaui.AndroidPlatform.Nfc
{
	/// <summary>
	/// Class handling MifareClassic NFC Interfaces.
	/// </summary>
	/// <param name="Tag">Underlying Android Tag object.</param>
	/// <param name="Technology">NFC interface.</param>
	public class MifareClassicInterface(Tag Tag, MifareClassic Technology)
		: NfcInterface(Tag, Technology), IMifareClassicInterface
	{
		private readonly MifareClassic mifareClassic = Technology;

		/// <summary>
		/// Reads all data from the interface
		/// </summary>
		public async Task<byte[]> ReadAllData()
		{
			//MifareClassicType Type = this.mifareClassic.Type;
			int BlockCount = this.mifareClassic.BlockCount;
			//int SectorCount = this.mifareClassic.SectorCount;
			int TotalBytes = BlockCount << 4;
			byte[] Data = new byte[TotalBytes];
			int BlockIndex = 0;

			while (BlockIndex < BlockCount)
			{
				byte[]? Block = await this.mifareClassic.ReadBlockAsync(BlockIndex++);
				if (Block is null || Block.Length != 16)
					throw UnableToReadDataFromDevice();

				Array.Copy(Block, 0, Data, BlockIndex << 4, 16);
			}

			return Data;
		}
	}
}
