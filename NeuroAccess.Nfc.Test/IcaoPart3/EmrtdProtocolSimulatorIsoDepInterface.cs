using NeuroAccess.Nfc.TravelDocuments;
using Waher.Networking;

namespace NeuroAccess.Nfc.Test.IcaoPart3
{
	/// <summary>
	/// ISO-DEP adapter backed by an <see cref="EmrtdProtocolSimulator"/>.
	/// </summary>
	public sealed class EmrtdProtocolSimulatorIsoDepInterface : IIsoDepInterface
	{
		private readonly EmrtdProtocolSimulator simulator;

		/// <summary>
		/// Initializes a new instance of the <see cref="EmrtdProtocolSimulatorIsoDepInterface"/> class.
		/// </summary>
		/// <param name="Simulator">APDU simulator.</param>
		public EmrtdProtocolSimulatorIsoDepInterface(EmrtdProtocolSimulator Simulator)
		{
			this.simulator = Simulator;
		}

		/// <summary>
		/// Gets the associated NFC tag.
		/// </summary>
		public INfcTag? Tag => null;

		/// <summary>
		/// Opens the simulated ISO-DEP connection.
		/// </summary>
		/// <returns>A completed task.</returns>
		public Task OpenIfClosed()
		{
			return Task.CompletedTask;
		}

		/// <summary>
		/// Closes the simulated ISO-DEP connection.
		/// </summary>
		public void CloseIfOpen()
		{
		}

		/// <summary>
		/// Gets high-layer response bytes.
		/// </summary>
		/// <returns>Empty bytes.</returns>
		public Task<byte[]> GetHighLayerResponse()
		{
			return Task.FromResult<byte[]>([]);
		}

		/// <summary>
		/// Gets historical bytes.
		/// </summary>
		/// <returns>Empty bytes.</returns>
		public Task<byte[]> GetHistoricalBytes()
		{
			return Task.FromResult<byte[]>([]);
		}

		/// <summary>
		/// Sets the simulated communication timeout.
		/// </summary>
		/// <param name="Timeout">Timeout in milliseconds.</param>
		public void SetTimeout(int Timeout)
		{
		}

		/// <summary>
		/// Executes an APDU command against the simulator.
		/// </summary>
		/// <param name="Command">Command APDU.</param>
		/// <param name="CommunicationLayer">Communication layer.</param>
		/// <returns>Response APDU.</returns>
		public Task<byte[]> ExecuteCommand(byte[] Command, ICommunicationLayer CommunicationLayer)
		{
			return Task.FromResult(this.simulator.Transmit(Command));
		}

		/// <summary>
		/// Disposes the adapter.
		/// </summary>
		public void Dispose()
		{
		}
	}
}
