using System.Threading;
using System.Threading.Tasks;
using Waher.Networking.XMPP.Push;

namespace NeuroAccessMaui.Services.Push
{
	/// <summary>
	/// Handles persistence and reporting of push notification tokens.
	/// </summary>
	public interface IPushTokenRegistrar
	{
		/// <summary>
		/// Reports a push token to the broker if needed, applying deduplication and force rules.
		/// </summary>
		/// <param name="TokenInformation">Token information.</param>
		/// <param name="ForceReport">Set to <c>true</c> to always report, regardless of cached state.</param>
		/// <param name="CancellationToken">Cancellation token.</param>
		/// <returns>True if the token was reported.</returns>
		Task<bool> ReportTokenAsync(TokenInformation TokenInformation, bool ForceReport, CancellationToken CancellationToken);
	}
}
