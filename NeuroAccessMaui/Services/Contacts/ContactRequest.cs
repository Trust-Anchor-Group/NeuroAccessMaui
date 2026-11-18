using Waher.Networking.XMPP.Contracts;
using Waher.Networking.XMPP.Events;

namespace NeuroAccessMaui.Services.Contacts
{
	/// <summary>
	/// Someone who wants to add the user as a contact, and waits for an answer.
	/// </summary>
	/// <param name="ChatAddress">Their chat address.</param>
	/// <param name="Name">Their name: from their ID if they shared one, or else the name they gave.</param>
	/// <param name="Identity">Their ID, if they shared one. The app has checked it.</param>
	/// <param name="PhotoFile">A file with their ID photo, if any.</param>
	/// <param name="Received">When the request arrived.</param>
	/// <param name="Request">The request from the network, which is answered when the user decides.</param>
	public sealed record ContactRequest(string ChatAddress, string Name, LegalIdentity? Identity, string? PhotoFile, DateTime Received,
		PresenceEventArgs Request)
	{
		/// <summary>
		/// If they shared an ID that the app has checked.
		/// </summary>
		public bool IsVerified => this.Identity is not null;
	}
}
