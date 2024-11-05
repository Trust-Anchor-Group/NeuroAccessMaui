namespace NeuroAccessMaui.UI.Pages.Contacts.Chat
{
	/// <summary>
	/// URI Scheme
	/// </summary>
	public enum UriScheme
	{
		/// <summary>
		/// XMPP URI Scheme (xmpp)
		/// </summary>
		Xmpp,

		/// <summary>
		/// IoT ID URI Scheme (iotid)
		/// </summary>
		IotId,

		/// <summary>
		/// IoT Smart Contract URI Scheme (iotsc)
		/// </summary>
		IotSc,

		/// <summary>
		/// IoT Discovery URI Scheme (iotdisco)
		/// </summary>
		IotDisco,

		/// <summary>
		/// eDaler URI Scheme (edaler)
		/// </summary>
		EDaler,

		/// <summary>
		/// Neuro-Feature URI Scheme (nfeat)
		/// </summary>
		NeuroFeature
	}

	/// <summary>
	/// Interfaces for views displaying markdown
	/// </summary>
	public interface IChatView
	{
		/// <summary>
		/// Called when a special Multi-media URI link has been clicked.
		/// </summary>
		/// <param name="Uri">URI</param>
		/// <param name="Scheme">URI Scheme</param>
		Task ExecuteUriClicked(string Uri, UriScheme Scheme);
	}
}
