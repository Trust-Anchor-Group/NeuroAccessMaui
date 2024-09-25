using Android.Nfc;
using Android.Nfc.Tech;
using NeuroAccess.Nfc;
using NeuroAccessMaui.Services;

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

		/// <summary>
		/// Formats a message (of records) in the NFC tag.
		/// </summary>
		/// <param name="ReadOnly">If the formatted message should be read-only (true), or writable (false).</param>
		/// <param name="Items">Items to encode.</param>
		/// <returns>If the tag could be formatted with the message.</returns>
		public async Task<bool> Format(bool ReadOnly, params object[] Items)
		{
			try
			{
				NdefMessage Message = await NdefInterface.CreateMessage(Items);

				if (ReadOnly)
					await this.ndefFormatable.FormatReadOnlyAsync(Message);
				else
					await this.ndefFormatable.FormatAsync(Message);

				return true;
			}
			catch (Exception ex)
			{
				ServiceRef.LogService.LogException(ex);
				return false;
			}
		}

	}
}
