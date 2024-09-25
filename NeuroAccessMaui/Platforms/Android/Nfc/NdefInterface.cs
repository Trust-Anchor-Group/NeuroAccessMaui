using Android.Nfc;
using Android.Nfc.Tech;
using NeuroAccess.Nfc;
using NeuroAccess.Nfc.Records;
using NeuroAccessMaui.AndroidPlatform.Nfc.Records;
using System.Text;
using Waher.Content;
using Waher.Runtime.Inventory;

namespace NeuroAccessMaui.AndroidPlatform.Nfc
{
	/// <summary>
	/// Class handling NDEF Interfaces.
	/// </summary>
	/// <param name="Tag">Underlying Android Tag object.</param>
	/// <param name="Technology">NFC interface.</param>
	public class NdefInterface(Tag Tag, Ndef Technology)
		: NfcInterface(Tag, Technology), INdefInterface
	{
		private readonly Ndef ndef = Technology;

		/// <summary>
		/// If the TAG can be made read-only
		/// </summary>
		public async Task<bool> CanMakeReadOnly()
		{
			await this.OpenIfClosed();
			return this.ndef.CanMakeReadOnly();
		}

		/// <summary>
		/// If the TAG is writable
		/// </summary>
		public async Task<bool> IsWritable()
		{
			await this.OpenIfClosed();
			return this.ndef.IsWritable;
		}

		/// <summary>
		/// Gets the message (with records) of the NDEF tag.
		/// </summary>
		public async Task<INdefRecord[]> GetMessage()
		{
			await this.OpenIfClosed();

			NdefMessage? Message = this.ndef.NdefMessage ?? throw UnableToReadDataFromDevice();
			NdefRecord[]? Records = Message.GetRecords() ?? throw UnableToReadDataFromDevice();
			List<INdefRecord> Result = [];

			foreach (NdefRecord Record in Records)
			{
				switch (Record.Tnf)
				{
					case NdefRecord.TnfAbsoluteUri:
						Result.Add(new UriRecord(Record));
						break;

					case NdefRecord.TnfMimeMedia:
						Result.Add(new MimeTypeRecord(Record));
						break;

					case NdefRecord.TnfWellKnown:
						byte[] Bin = Record.GetTypeInfo() ?? throw UnableToReadDataFromDevice();
						string TypeInfo = Encoding.UTF8.GetString(Bin);

						if (TypeInfo == "U")
							Result.Add(new UriRecord(Record));
						else
							Result.Add(new WellKnownTypeRecord(Record));
						break;

					case NdefRecord.TnfExternalType:
						Result.Add(new ExternalTypeRecord(Record));
						break;

					case NdefRecord.TnfEmpty:
					case NdefRecord.TnfUnchanged:
					case NdefRecord.TnfUnknown:
					default:
						break;
				}
			}

			return [.. Result];
		}

		/// <summary>
		/// Sets the message (with recorsd) on the NDEF tag.
		/// </summary>
		/// <param name="Items">Items to encode</param>
		/// <returns>If the items could be encoded and written to the tag.</returns>
		public async Task<bool> SetMessage(params object[] Items)
		{
			try
			{
				await this.OpenIfClosed();

				List<NdefRecord> Records = [];

				foreach (object Item in Items)
				{
					if (Item is Uri Uri)
					{
						if (Uri.IsAbsoluteUri)
							Records.Add(NdefRecord.CreateUri(Uri.AbsoluteUri) ?? throw new IOException("Unable to create URI record."));
						else
							return false;
					}
					else if (Item is string s)
						Records.Add(NdefRecord.CreateTextRecord(null, s) ?? throw new IOException("Unable to create text record."));
					else
					{
						if (Item is not KeyValuePair<byte[], string> Mime)
						{
							if (!InternetContent.Encodes(Item, out Grade _, out IContentEncoder Encoder))
								return false;

							Mime = await Encoder.EncodeAsync(Item, Encoding.UTF8);
						}

						Records.Add(NdefRecord.CreateMime(Mime.Value, Mime.Key) ?? throw new IOException("Unable to create MIME record."));
					}
				}

				NdefMessage Message = new(Records.ToArray());

				await this.ndef.WriteNdefMessageAsync(Message);

				return true;
			}
			catch (Exception)
			{
				return false;
			}
		}
	}
}
