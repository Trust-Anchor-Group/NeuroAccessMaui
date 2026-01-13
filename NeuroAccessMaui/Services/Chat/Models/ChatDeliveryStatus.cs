namespace NeuroAccessMaui.Services.Chat.Models
{
	/// <summary>
	/// Represents delivery state transitions for chat messages.
	/// </summary>
	public enum ChatDeliveryStatus
	{
		/// <summary>
		/// Message has been created locally but not yet handed to the transport layer.
		/// </summary>
		Pending,

		/// <summary>
		/// Message is currently being transmitted.
		/// </summary>
		Sending,

		/// <summary>
		/// Message has been acknowledged by the server or transport layer.
		/// </summary>
		Sent,

		/// <summary>
		/// Message failed to send and awaits manual or automatic retry.
		/// </summary>
		Failed,

		/// <summary>
		/// Counterparty transport has delivered the payload.
		/// </summary>
		Received,

		/// <summary>
		/// Counterparty UI has surfaced the payload to the user.
		/// </summary>
		Displayed
	}
}
