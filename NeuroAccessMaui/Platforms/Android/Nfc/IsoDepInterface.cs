using System.Collections;
using Android.Nfc;
using Android.Nfc.Tech;
using NeuroAccess.Nfc;
using Waher.Networking;
using Waher.Networking.Sniffers;

namespace NeuroAccessMaui.AndroidPlatform.Nfc
{
	/// <summary>
	/// Class handling ISO DEP Interfaces.
	/// </summary>
	/// <param name="Tag">Underlying Android Tag object.</param>
	/// <param name="Technology">NFC interface.</param>
	/// <param name="Sniffers">Optional sniffers, for debugging purposes.</param>
	public class IsoDepInterface(Tag Tag, IsoDep Technology, params ISniffer[] Sniffers)
		: NfcInterface(Tag, Technology), IIsoDepInterface, ICommunicationLayer
	{
		private readonly IsoDep isoDep = Technology;
		private readonly CommunicationLayer sniffable = new(true, Sniffers);

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
		/// <returns>Response</returns>
		public async Task<byte[]> ExecuteCommand(byte[] Command)
		{
			this.sniffable.TransmitBinary(false, Command);

			byte[]? Response = await this.isoDep.TransceiveAsync(Command);

			if (Response is null)
			{
				this.sniffable.Error("No response returned.");
				throw UnableToReadDataFromDevice();
			}

			this.sniffable.ReceiveBinary(false, Response);

			return Response;
		}

		#region IObservableLayer

		public bool DecoupledEvents => this.sniffable.DecoupledEvents;
		public void Information(string Comment) => this.sniffable.Information(Comment);
		public void Information(DateTime Timestamp, string Comment) => this.sniffable.Information(Timestamp, Comment);
		public void Warning(string Warning) => this.sniffable.Warning(Warning);
		public void Warning(DateTime Timestamp, string Warning) => this.sniffable.Warning(Timestamp, Warning);
		public void Error(string Error) => this.sniffable.Error(Error);
		public void Error(DateTime Timestamp, string Error) => this.sniffable.Error(Timestamp, Error);
		public void Exception(string Exception) => this.sniffable.Exception(Exception);
		public void Exception(DateTime Timestamp, string Exception) => this.sniffable.Exception(Timestamp, Exception);
		public void Exception(Exception Exception) => this.sniffable.Exception(Exception);
		public void Exception(DateTime Timestamp, Exception Exception) => this.sniffable.Exception(Timestamp, Exception);
		public IEnumerator<ISniffer> GetEnumerator() => this.sniffable.GetEnumerator();
		IEnumerator IEnumerable.GetEnumerator() => this.sniffable.GetEnumerator();

		#endregion

		#region ICommunicationLayer

		public ISniffer[] Sniffers => this.sniffable.Sniffers;
		public bool HasSniffers => this.sniffable.HasSniffers;
		public void Add(ISniffer Sniffer) => this.sniffable.Add(Sniffer);
		public void AddRange(IEnumerable<ISniffer> Sniffers) => this.sniffable.AddRange(Sniffers);
		public bool Remove(ISniffer Sniffer) => this.sniffable.Remove(Sniffer);
		public void ReceiveBinary(int Count) => this.sniffable.ReceiveBinary(Count);
		public void ReceiveBinary(DateTime Timestamp, int Count) => this.sniffable.ReceiveBinary(Timestamp, Count);
		public void ReceiveBinary(bool ConstantBuffer, byte[] Data) => this.sniffable.ReceiveBinary(ConstantBuffer, Data);
		public void ReceiveBinary(DateTime Timestamp, bool ConstantBuffer, byte[] Data) => this.sniffable.ReceiveBinary(Timestamp, ConstantBuffer, Data);
		public void ReceiveBinary(bool ConstantBuffer, byte[] Data, int Offset, int Count) => this.sniffable.ReceiveBinary(ConstantBuffer, Data, Offset, Count);
		public void ReceiveBinary(DateTime Timestamp, bool ConstantBuffer, byte[] Data, int Offset, int Count) => this.sniffable.ReceiveBinary(Timestamp, ConstantBuffer, Data, Offset, Count);
		public void TransmitBinary(int Count) => this.sniffable.TransmitBinary(Count);
		public void TransmitBinary(DateTime Timestamp, int Count) => this.sniffable.TransmitBinary(Timestamp, Count);
		public void TransmitBinary(bool ConstantBuffer, byte[] Data) => this.sniffable.TransmitBinary(ConstantBuffer, Data);
		public void TransmitBinary(DateTime Timestamp, bool ConstantBuffer, byte[] Data) => this.sniffable.TransmitBinary(Timestamp, ConstantBuffer, Data);
		public void TransmitBinary(bool ConstantBuffer, byte[] Data, int Offset, int Count) => this.sniffable.TransmitBinary(ConstantBuffer, Data, Offset, Count);
		public void TransmitBinary(DateTime Timestamp, bool ConstantBuffer, byte[] Data, int Offset, int Count) => this.sniffable.TransmitBinary(Timestamp, ConstantBuffer, Data, Offset, Count);
		public void ReceiveText(string Text) => this.sniffable.ReceiveText(Text);
		public void ReceiveText(DateTime Timestamp, string Text) => this.sniffable.ReceiveText(Timestamp, Text);
		public void TransmitText(string Text) => this.sniffable.TransmitText(Text);
		public void TransmitText(DateTime Timestamp, string Text) => this.sniffable.TransmitText(Timestamp, Text);

		#endregion
	}
}
