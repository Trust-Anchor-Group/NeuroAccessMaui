using System.Threading;
using System.Threading.Tasks;
using Waher.Events;
using Waher.Networking.XMPP.Push;

namespace NeuroAccessMaui.Services.Push
{
	/// <summary>
	/// Abstraction for platform push transports (Firebase, APNS, WNS).
	/// </summary>
	public interface IPushTransport
	{
		/// <summary>
		/// Raised when the transport receives a refreshed token.
		/// </summary>
		event EventHandlerAsync<TokenInformation>? TokenChanged;

		/// <summary>
		/// Initializes the transport and wires platform callbacks.
		/// </summary>
		/// <param name="CancellationToken">Cancellation token.</param>
		/// <returns>Task representing the asynchronous operation.</returns>
		Task InitializeAsync(CancellationToken CancellationToken);
	}
}
