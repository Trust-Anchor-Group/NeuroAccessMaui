using System.Threading.Tasks;
using Waher.Networking;

namespace NeuroAccess.Nfc
{
	/// <summary>
	/// ISO DEP interface, for communication with an NFC Tag.
	/// </summary>
	public interface IIsoDepInterface : INfcInterface
	{
		/// <summary>
		/// Return the higher layer response bytes for NfcB tags.
		/// </summary>
		Task<byte[]> GetHighLayerResponse();

		/// <summary>
		/// Return the ISO-DEP historical bytes for NfcA tags.
		/// </summary>
		Task<byte[]> GetHistoricalBytes();

		/// <summary>
		/// Sets communication timeout.
		/// </summary>
		/// <param name="Timeout">Timeout, in milliseconds.</param>
		void SetTimeout(int Timeout);

		/// <summary>
		/// Executes an ISO 14443-4 command on the tag.
		/// </summary>
		/// <param name="Command">Command</param>
		/// <param name="CommunicationLayer">Communication Layer</param>
		/// <returns>Response</returns>
		Task<byte[]> ExecuteCommand(byte[] Command, ICommunicationLayer CommunicationLayer);
	}
}
