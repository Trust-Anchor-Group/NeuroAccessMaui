using Waher.Persistence;

namespace NeuroAccessMaui.Services.Contracts
{
	/// <summary>
	/// An invitation to sign a contract, as received from another user.
	/// </summary>
	/// <param name="ContractId">The ID of the contract.</param>
	/// <param name="Role">The proposed role.</param>
	/// <param name="FromBareJid">Bare JID of the sender.</param>
	/// <param name="Message">The message sent with the invitation. Plain text.</param>
	/// <param name="ReceivedAt">When the invitation arrived (UTC).</param>
	public class ContractInvitation(CaseInsensitiveString ContractId, string? Role, string? FromBareJid, string? Message, DateTime ReceivedAt)
	{
		/// <summary>
		/// The ID of the contract.
		/// </summary>
		public CaseInsensitiveString ContractId { get; } = ContractId;

		/// <summary>
		/// The proposed role.
		/// </summary>
		public string? Role { get; } = Role;

		/// <summary>
		/// Bare JID of the sender.
		/// </summary>
		public string? FromBareJid { get; } = FromBareJid;

		/// <summary>
		/// The message sent with the invitation. Plain text.
		/// </summary>
		public string? Message { get; } = Message;

		/// <summary>
		/// When the invitation arrived (UTC).
		/// </summary>
		public DateTime ReceivedAt { get; } = ReceivedAt;
	}
}
