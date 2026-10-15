using Waher.Runtime.Inventory;

namespace NeuroAccessMaui.Links
{
	/// <summary>
	/// Interface for classes that can prepare links. Links are opened through <see cref="ILinkService"/>, which also waits for
	/// connectivity, applies a timeout, and reports failures safely.
	/// </summary>
	public interface ILinkOpener : IProcessingSupport<Uri>
	{
		/// <summary>
		/// Gets whether preparing or opening links requires an XMPP connection.
		/// </summary>
		bool RequiresXmppConnection => false;

		/// <summary>
		/// Prepares a link without navigating, prompting, or executing its action.
		/// </summary>
		/// <remarks>
		/// Implementations throw <see cref="FormatException"/> or return <c>null</c> for payloads that are malformed or no longer valid,
		/// and let other exceptions propagate for temporary failures. Exceptions must not carry the link's payload.
		/// </remarks>
		/// <param name="Link">The link to prepare.</param>
		/// <param name="CancellationToken">Cancellation for this preparation attempt.</param>
		/// <returns>The prepared link, or <c>null</c> when the payload is invalid.</returns>
		Task<PreparedLink?> PrepareAsync(Uri Link, CancellationToken CancellationToken);
	}
}
