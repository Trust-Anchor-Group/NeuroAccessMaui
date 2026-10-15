using Android.Nfc;
using Android.Nfc.Tech;
using NeuroAccess.Nfc;
using NeuroAccess.Nfc.Exceptions;
using Waher.Networking;

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
		public Task<byte[]> GetHighLayerResponse()
		{
			return Task.FromResult(this.isoDep.GetHiLayerResponse() ?? throw UnableToReadDataFromDevice());
		}

		/// <summary>
		/// Return the ISO-DEP historical bytes for NfcA tags.
		/// </summary>
		public Task<byte[]> GetHistoricalBytes()
		{
			return Task.FromResult(this.isoDep.GetHistoricalBytes() ?? throw UnableToReadDataFromDevice());
		}

		/// <summary>
		/// Sets communication timeout.
		/// </summary>
		/// <param name="Timeout">Timeout, in milliseconds.</param>
		public void SetTimeout(int Timeout)
		{
			this.isoDep.SetTimeout(Timeout);
		}

		/// <summary>
		/// Executes an ISO 14443-4 command on the tag.
		/// </summary>
		/// <param name="Command">Command</param>
		/// <param name="CommunicationLayer">Communication Layer</param>
		/// <returns>Response</returns>
		public async Task<byte[]> ExecuteCommand(byte[] Command, ICommunicationLayer CommunicationLayer)
		{
			CommunicationLayer.TransmitBinary(false, Command);

			byte[]? Response;

			try
			{
				Response = await this.TransceiveAsync(Command);
			}
			catch (NfcConnectionLostException Ex)
			{
				CommunicationLayer.Error(Ex.Message);
				throw;
			}

			if (Response is null)
			{
				CommunicationLayer.Error("No response returned.");
				throw UnableToReadDataFromDevice();
			}

			CommunicationLayer.ReceiveBinary(false, Response);

			return Response;
		}

		/// <summary>
		/// Sends a command to the tag, reporting a tag that left the field as a lost connection.
		/// </summary>
		/// <param name="Command">Command</param>
		/// <returns>Response, or null if the tag returned nothing.</returns>
		/// <exception cref="NfcConnectionLostException">The tag left the field or the exchange failed.</exception>
		private async Task<byte[]?> TransceiveAsync(byte[] Command)
		{
			try
			{
				return await this.isoDep.TransceiveAsync(Command);
			}
			catch (Java.IO.IOException Ex)
			{
				// Covers TagLostException, which Android throws when the document is moved away mid-read.
				throw new NfcConnectionLostException("NFC tag connection lost: " + Ex.Message, Ex);
			}
		}
	}
}
